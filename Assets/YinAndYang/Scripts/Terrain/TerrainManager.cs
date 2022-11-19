//#define THREADED
//#define BENCHMARK_STOP

using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class TerrainManager : NetworkBehaviour
{
    public ViewDistancePreset[] ViewDistancePreset;
    public GameObject TilePrefab;

    Vector2 _lastDisplayPosClient;
    int _chunkTileIdx;

    TerrainGenerator _terrainGenerator;

    [SyncVar] int GeneratorQueueCount;
    readonly ConcurrentQueue<TerrainTile> _terrainGeneratorInputQueue = new();
    readonly ConcurrentQueue<TerrainTile> _terrainGeneratorOutputQueue = new();

#if THREADED
    CancellationTokenSource _cancellationTokenSource;
    Thread _tileGenetatorThread;
#endif

    readonly Dictionary<Vector2, TerrainTile> _generatedTiles = new();
    readonly Dictionary<string, TerrainTileDisplay> _generatedTileDisplays = new();
    HashSet<TerrainTileDisplay> _activeTileDisplays = new();

    HashSet<Vector2> _generateTilesRequestViewPos = new();

    int lastDebugQueueCount=-1;
    float _maxViewDistance;

    public bool IsLoading
    {
        get
        {
            return _generatedTiles.Count == 0 || GeneratorQueueCount > 0;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        GeneratorQueueCount = 0;

        _terrainGenerator = StaticObjectAccessor.GetTerrainGenerator();
        _terrainGenerator.SetupGenerator();
#if THREADED
        _cancellationTokenSource = new CancellationTokenSource();
        _tileGenetatorThread = new Thread(GenerateTileOnThread);
        _tileGenetatorThread.Start();
#endif
        lastDebugQueueCount = -1;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _terrainGenerator = StaticObjectAccessor.GetTerrainGenerator();
        _terrainGenerator.SetupGenerator();

        _maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;

        _chunkTileIdx = Mathf.FloorToInt(_maxViewDistance / TerrainGenerator.TileDataResolution);
        _lastDisplayPosClient = new Vector2(float.MaxValue, float.MaxValue);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
#if THREADED
        _cancellationTokenSource.Cancel();
        _tileGenetatorThread.Join();

        _cancellationTokenSource = null;
        _tileGenetatorThread = null;
#endif
    }

#if THREADED
    void GenerateTileOnThread()
    {
        var token = _cancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            ProcessGenerateTile(token);

            Thread.Sleep(1);
        }
    }
#endif
    void ProcessGenerateTile(CancellationToken token)
    {
        if (_terrainGeneratorInputQueue.TryDequeue(out var tile))
        {
            UpdateTileToNextState(tile);

            if (!token.IsCancellationRequested)
            {
                _terrainGeneratorOutputQueue.Enqueue(tile);
            }
        }
    }

    public void UpdateTileToNextState(TerrainTile tile)
    {
        switch (tile.State)
        {
            case TileState.Empty:
                _terrainGenerator.GenerateBiomeMap(tile);
                break;
            case TileState.BiomeMap:
                _terrainGenerator.GenerateHeightMap(tile);
                break;
            case TileState.HeightMap:
                _terrainGenerator.BlendHeightMap(tile);
                break;
            case TileState.BlendedHeightMap:
                _terrainGenerator.GenerateAllMeshData(tile);
                break;
            case TileState.MeshData:
                _terrainGenerator.AdjustTerrainTileMeshData(tile);
                break;
            case TileState.AdjustedMeshData:
                //TOWN?
                break;
            default:
                break;
        }
    }


    void Update()
    {
        if (IsServer)
        {
#if !THREADED
            ProcessGenerateTile(CancellationToken.None);
#endif

            if (_terrainGeneratorOutputQueue.TryDequeue(out TerrainTile tile))
            {
                if (!_generatedTileDisplays.ContainsKey(tile.Name))
                {
                    CreateDisplayObjectOnServer(tile);
                }
                else
                {
                    UpdateTileOnClient(tile);
                }
                
                UpdateQueueCount();
            }
        }

        if (IsClient) 
        { 
            if (Camera.main == null) return;

            Camera.main.farClipPlane = _maxViewDistance;
            var viewPos = Camera.main.transform.position.To2D();

            if (GeneratorQueueCount == 0)
            {
                if ((_lastDisplayPosClient - viewPos).magnitude > TerrainGenerator.TileDataResolutionHalf)
                {
                    if (IsChunkLoadedInViewDistance(viewPos))
                    {
                        ActivateTilesInViewDistance(viewPos);
                    }
                    else
                    {
                        GenerateTerrainAround(viewPos);
                    }

                    _lastDisplayPosClient = viewPos;
                }
            }
        }

        if(lastDebugQueueCount != GeneratorQueueCount)
        {
            lastDebugQueueCount = GeneratorQueueCount;
            Debug.Log($"QueueCount {lastDebugQueueCount}");
        }

#if BENCHMARK_STOP
        if (_activeTileDisplays.Count > 1)
        {
            Debug.Log("Quit");
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
    }

    [Server]
    private void UpdateQueueCount()
    {
        GeneratorQueueCount = _terrainGeneratorInputQueue.Count + _terrainGeneratorOutputQueue.Count;
    }

    [Server]
    void CreateDisplayObjectOnServer(TerrainTile tile)
    {
        //Debug.Log($"CreateDisplayObjectOnServer {tile.Name}");

        var obj = Instantiate(TilePrefab, tile.PhysicalPos.To3D(), Quaternion.identity);
        obj.name = tile.Name;

        var tileDisplay = obj.GetComponent<TerrainTileDisplay>();
        tileDisplay.SetTile(tile);

        _generatedTiles.Add(tile.PhysicalPos, tile);
        _generatedTileDisplays.Add(tile.Name, tileDisplay);

        ServerManager.Spawn(obj);

        SetObjectOnClient(obj, tile);
    }

    [ObserversRpc]
    void SetObjectOnClient(GameObject obj, TerrainTile tile)
    {
        //Debug.Log($"SetObjectOnClient {tile.Name}");

        var tileDisplay = obj.GetComponent<TerrainTileDisplay>();
        tileDisplay.SetTile(tile);

        if (!IsServer)
        {
            _generatedTiles.Add(tile.PhysicalPos, tile);
            _generatedTileDisplays.Add(tile.Name, tileDisplay);
        }

        UpdateTileDisplay(tile, _lastDisplayPosClient, _activeTileDisplays);
    }

    [ObserversRpc]
    void UpdateTileOnClient(TerrainTile tile)
    {
        //Debug.Log($"UpdateTileOnClient {tile.Name}");

        _generatedTiles[tile.PhysicalPos] = tile;
        var display = _generatedTileDisplays[tile.Name];
        display.SetTile(tile);
    }

    [Client]
    bool IsChunkLoadedInViewDistance(Vector2 viewPos)
    {
        //Debug.Log($"IsChunkLoadedInViewDistance {viewPos}");

        for (int x = -_chunkTileIdx; x <= _chunkTileIdx; x++)
        {
            for (int z = -_chunkTileIdx; z <= _chunkTileIdx; z++)
            {
                var pos = viewPos + new Vector2(x * TerrainGenerator.TileDataResolution, z * TerrainGenerator.TileDataResolution);
                var tile = GetTileAt(pos);
                if (tile == null) return false;
                if (!_generatedTileDisplays.ContainsKey(tile.Name)) return false;
            }
        }

        return true;
    }

    [Client]
    void ActivateTilesInViewDistance(Vector2 viewPos)
    {
        var newActiveTileDisplays = new HashSet<TerrainTileDisplay>();

        for (int x = -_chunkTileIdx; x <= _chunkTileIdx; x++)
        {
            for (int z = -_chunkTileIdx; z <= _chunkTileIdx; z++)
            {
                var pos = viewPos + new Vector2(x * TerrainGenerator.TileDataResolution, z * TerrainGenerator.TileDataResolution);
                var tile = GetTileAt(pos);
                UpdateTileDisplay(tile, viewPos, newActiveTileDisplays);
            }
        }

        foreach (var tile in _activeTileDisplays)
        {
            if (!newActiveTileDisplays.Contains(tile))
            {
                tile.gameObject.SetActive(false);
            }
        }
        _activeTileDisplays = newActiveTileDisplays;
    }

    void UpdateTileDisplay(TerrainTile tile, Vector2 viewPos, HashSet<TerrainTileDisplay> newActiveTileDisplays)
    {
        var tileDisplay = _generatedTileDisplays[tile.Name];
        var viewPreset = GetTileLOD(tile, viewPos);

        //Debug.Log($"ActivateTilesInViewDistance {tile.Name} {viewPreset}");

        if (viewPreset != null)
        {
            tileDisplay.Display(viewPreset);
            tileDisplay.gameObject.SetActive(true);

            if (!newActiveTileDisplays.Contains(tileDisplay))
            {
                newActiveTileDisplays.Add(tileDisplay);
            }
        }
        else
        {
            tileDisplay.gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GenerateTerrainAround(Vector2 viewPos)
    {
        //Debug.Log($"GenerateTerrainAround {viewPos}");
        if (_generateTilesRequestViewPos.Contains(viewPos)) return;
        _generateTilesRequestViewPos.Add(viewPos);

        var requestPosList = new List<Vector2>();

        for (int x = -_chunkTileIdx; x <= _chunkTileIdx; x++)
        {
            for (int z = -_chunkTileIdx; z <= _chunkTileIdx; z++)
            {
                var pos = viewPos + new Vector2(x * TerrainGenerator.TileDataResolution, z * TerrainGenerator.TileDataResolution);
                requestPosList.Add(pos);
            }
        }

        //request firs which is closer to the player
        foreach (var pos in requestPosList.OrderBy(p => Vector2.Distance(p, viewPos)))
        {
            RequestTileGeneration(pos.ToInt());
        }
    }

    [Server]
    void RequestTileGeneration(Vector2 pos)
    {
        var tilePos = TerrainGenerator.AnyPosToTilePos(pos);

        if (_generatedTiles.ContainsKey(tilePos)) return;

        //Debug.Log($"RequestTileGeneration {tilePos}");

        _terrainGeneratorInputQueue.Enqueue(_terrainGenerator.CreateEmptyTile(pos));
        UpdateQueueCount();
    }

    [Client]
    ViewDistancePreset GetTileLOD(TerrainTile tile, Vector2 viewPos)
    {
        //var closestPoint = tile.Area.ClosestPoint(viewPos);
        var distance = Vector2.Distance(tile.PhysicalPos + TerrainGenerator.TileDataResolutionHalfVect, viewPos);

        //Debug.Log($"GetLODWithinTheViewDistance {tile.Area} {distance}");

        foreach (var preset in ViewDistancePreset)
        {
            if (distance <= preset.ViewDistance)
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>
    /// Return null if the tile not loaded
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public TerrainTile GetTileAt(Vector2 pos)
    {
        if(_generatedTiles.TryGetValue(TerrainGenerator.AnyPosToTilePos(pos), out var tile))
        {
            return tile;
        }

        return null;
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.To2D();
        var tile = GetTileAt(posXZ);

        var height = tile.GetHeightAt(posXZ-tile.PhysicalPos);

        return new Vector3(pos.x, height, pos.z);
    }

    //TODO make it async on separated thread, make it general, so it would support any async tile action
    [ServerRpc]
    public void FlatTerrain(Rect flatArea, float toHeight)
    {
        var tiles = new HashSet<TerrainTile>();
        tiles.Add(GetTileAt(flatArea.center));

        { 
            var tile = GetTileAt(flatArea.position);
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
            }
        
            tile = GetTileAt(flatArea.position + new Vector2(flatArea.size.x, 0));
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
            }

            tile = GetTileAt(flatArea.position + flatArea.size);
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
            }

            tile = GetTileAt(flatArea.position + new Vector2(0, flatArea.size.y));
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
            }
        }

        foreach (var tile in tiles)
        {
            if(tile.State >= TileState.HeightMap)
            {
                if (tile.FlatHeightMap(flatArea, toHeight))
                {
                    _terrainGeneratorInputQueue.Enqueue(tile);
                    UpdateQueueCount();
                }
            }
            
        }
    }
}
