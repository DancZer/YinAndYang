using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TerrainManager : NetworkBehaviour
{
    public ViewDistancePreset[] ViewDistancePreset;
    public GameObject TilePrefab;
    
    public float TileSize = 240;

    Vector2 _lastDisplayPosClient;
    int _chunkTileIdx;

    TerrainGenerator _terrainGenerator;
    CancellationTokenSource _cancellationTokenSource;

    [SyncVar] int GeneratorQueueCount;
    readonly ConcurrentQueue<Vector2> _tileGeneratorRequestQueue = new ();
    readonly ConcurrentQueue<TerrainTile> _tileReMeshGeneratorResultQueue = new();
    readonly ConcurrentQueue<TerrainTile> _tileGeneratorResultQueue = new();
    Thread _tileGenetatorThread;
    Thread _tileMeshGenetatorThread;

    readonly Dictionary<Vector2, TerrainTile> _generatedTiles = new();
    readonly Dictionary<string, TerrainTileDisplay> _generatedTileDisplays = new();
    HashSet<TerrainTileDisplay> _activeTileDisplays = new();

    int lastDebugQueueCount=-1;

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
        _terrainGenerator.InitMaterial();

        _cancellationTokenSource = new CancellationTokenSource();
        _tileGenetatorThread = new Thread(GenerateTileOnThread);
        _tileGenetatorThread.Start();

        _tileMeshGenetatorThread = new Thread(GenerateTileMeshOnThread);
        _tileMeshGenetatorThread.Start();
        lastDebugQueueCount = -1;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _terrainGenerator = StaticObjectAccessor.GetTerrainGenerator();
        _terrainGenerator.InitMaterial();

        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        _chunkTileIdx = Mathf.FloorToInt(maxViewDistance / TileSize);
        _lastDisplayPosClient = new Vector2(float.MaxValue, float.MaxValue);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _cancellationTokenSource.Cancel();
        _tileGenetatorThread.Join();
        _tileMeshGenetatorThread.Join();

        _cancellationTokenSource = null;
        _tileGenetatorThread = null;
        _tileMeshGenetatorThread = null;

    }

    void GenerateTileOnThread()
    {
        var token = _cancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            if (_tileGeneratorRequestQueue.TryDequeue(out Vector2 pos))
            {
                var tile = new TerrainTile(new Rect(pos, new Vector2(TileSize, TileSize)));

                if (!token.IsCancellationRequested)
                {
                    _terrainGenerator.GenerateTerrainData(tile);
                }

                if (!token.IsCancellationRequested)
                {
                    _tileReMeshGeneratorResultQueue.Enqueue(tile);
                }
            }

            Thread.Sleep(1);
        }
    }


    void GenerateTileMeshOnThread()
    {
        var token = _cancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            if (_tileReMeshGeneratorResultQueue.TryDequeue(out var tile))
            {
                foreach (var preset in ViewDistancePreset)
                {
                    Debug.Log($"GenerateTerrainTileOnThread {tile.Name} LOD {preset.DisplayLOD} {preset.CollisionLOD}");

                    if (!token.IsCancellationRequested)
                    {
                        _terrainGenerator.GenerateMeshData(tile, preset.DisplayLOD);
                    }

                    if (!token.IsCancellationRequested)
                    {
                        _terrainGenerator.GenerateMeshData(tile, preset.CollisionLOD);
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    _tileGeneratorResultQueue.Enqueue(tile);
                }
            }

            Thread.Sleep(1);
        }
    }

    void Update()
    {
        if (IsServer)
        {
            if (_tileGeneratorResultQueue.TryDequeue(out TerrainTile tile))
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

            var viewPos = Camera.main.transform.position.To2D();

            if (GeneratorQueueCount == 0)
            {
                if (IsChunkLoadedInViewDistance(viewPos))
                {
                    if ((_lastDisplayPosClient - viewPos).magnitude > TileSize/2)
                    {
                        ActivateTilesInViewDistance(viewPos);
                        _lastDisplayPosClient = viewPos;
                    }
                }
                else
                {
                    GenerateTerrainAround(viewPos);
                }
            }
        }

        if(lastDebugQueueCount != GeneratorQueueCount)
        {
            lastDebugQueueCount = GeneratorQueueCount;

            Debug.Log($"QueueCount {lastDebugQueueCount}");
        }
    }

    [Server]
    private void UpdateQueueCount()
    {
        GeneratorQueueCount = _tileGeneratorRequestQueue.Count + _tileGeneratorResultQueue.Count + _tileReMeshGeneratorResultQueue.Count;
    }

    [Server]
    void CreateDisplayObjectOnServer(TerrainTile tile)
    {
        Debug.Log($"CreateDisplayObjectOnServer {tile.Name}");

        var obj = Instantiate(TilePrefab, tile.Area.center.To3D(), Quaternion.identity);
        obj.name = tile.Name;

        var tileDisplay = obj.GetComponent<TerrainTileDisplay>();
        tileDisplay.SetTile(tile);

        _generatedTiles.Add(tile.Area.position, tile);
        _generatedTileDisplays.Add(tile.Name, tileDisplay);

        ServerManager.Spawn(obj);

        SetObjectOnClient(obj, tile);
    }

    [ObserversRpc]
    void SetObjectOnClient(GameObject obj, TerrainTile tile)
    {
        Debug.Log($"SetObjectOnClient {tile.Name}");

        var tileDisplay = obj.GetComponent<TerrainTileDisplay>();
        tileDisplay.SetTile(tile);

        if (!IsServer)
        {
            _generatedTiles.Add(tile.Area.position, tile);
            _generatedTileDisplays.Add(tile.Name, tileDisplay);
        }
    }

    [ObserversRpc]
    void UpdateTileOnClient(TerrainTile tile)
    {
        Debug.Log($"UpdateTileOnClient {tile.Name}");

        _generatedTiles[tile.Area.position] = tile;
        var display = _generatedTileDisplays[tile.Name];
        display.SetTile(tile);
    }

    [Client]
    bool IsChunkLoadedInViewDistance(Vector2 viewPos)
    {
        Debug.Log($"IsChunkLoadedInViewDistance {viewPos}");

        for (int x = -_chunkTileIdx; x <= _chunkTileIdx; x++)
        {
            for (int z = -_chunkTileIdx; z <= _chunkTileIdx; z++)
            {
                var pos = viewPos + new Vector2(x * TileSize, z * TileSize);
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
                var pos = viewPos + new Vector2(x * TileSize, z * TileSize);
                var tile = GetTileAt(pos);
                var viewPreset = GetTileLOD(tile, viewPos);
                var tileDisplay = _generatedTileDisplays[tile.Name];

                Debug.Log($"ActivateTilesInViewDistance {tile.Name} {viewPreset}");

                if (viewPreset != null)
                {
                    tileDisplay.Display(viewPreset);
                    tileDisplay.gameObject.SetActive(true);
                    newActiveTileDisplays.Add(tileDisplay);
                }
                else
                {
                    tileDisplay.gameObject.SetActive(false);
                }
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

    [ServerRpc(RequireOwnership = false)]
    private void GenerateTerrainAround(Vector2 viewPos)
    {
        Debug.Log($"GenerateTerrainAround {viewPos}");

        for (int x = -_chunkTileIdx; x <= _chunkTileIdx; x++)
        {
            for (int z = -_chunkTileIdx; z <= _chunkTileIdx; z++)
            {
                var pos = viewPos + new Vector2(x * TileSize, z * TileSize);
                RequestTileGeneration(pos);
            }
        }
    }

    [Server]
    void RequestTileGeneration(Vector2 pos)
    {
        var tilePos = CalculateTerrainTilePos(pos);

        if (_generatedTiles.ContainsKey(tilePos)) return;

        foreach (var posInQueue in _tileGeneratorRequestQueue)
        {
            if (posInQueue == tilePos) return;
        }

        Debug.Log($"RequestTileGeneration {tilePos}");

        _tileGeneratorRequestQueue.Enqueue(tilePos);
        UpdateQueueCount();
    }

    [Client]
    ViewDistancePreset GetTileLOD(TerrainTile tile, Vector2 view)
    {
        var closestPoint = tile.Area.ClosestPoint(view);
        var distance = Vector2.Distance(closestPoint, view);

        Debug.Log($"GetLODWithinTheViewDistance {tile.Area} {distance}");

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
        var tilePos = CalculateTerrainTilePos(pos);

        if(_generatedTiles.TryGetValue(tilePos, out var tile))
        {
            return tile;
        }

        return null;
    }

    Vector2 CalculateTerrainTilePos(Vector2 pos)
    {
        var halfSize = TileSize / 2;
        return new Vector2(Mathf.RoundToInt(pos.x / TileSize) * TileSize - halfSize, Mathf.RoundToInt(pos.y / TileSize) * TileSize - halfSize);
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.To2D();
        var tile = GetTileAt(posXZ);

        var height = tile.GetHeightAt(posXZ-tile.Area.position);

        return new Vector3(pos.x, height, pos.z);
    }

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
            if (tile.FlatHeightMap(flatArea, toHeight))
            {
                _tileReMeshGeneratorResultQueue.Enqueue(tile);
                UpdateQueueCount();
            }
        }
    }
}
