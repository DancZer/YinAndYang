#define THREADED
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
    public GameObject TilePrefab;
    public RequiredTileStatePreset[] TerrainTilePresets;
    public bool IsLoading
    {
        get
        {
            return _activeTileDisplays.Count == 0 || _generatedTiles.Count == 0 || GeneratorQueueCount > 0;
        }
    }
    public bool IsPlayerAreaLoaded { get; private set; }

    TerrainGenerator _terrainGenerator;

    [SyncVar] int GeneratorQueueCount;
    readonly Dictionary<Vector2, TerrainTile> _generatedTiles = new();
    readonly ConcurrentQueue<(TerrainTile tile, TerrainTileState requestedState)> _terrainGeneratorInputQueue = new();
    readonly ConcurrentQueue<TerrainTile> _terrainGeneratorOutputQueue = new();

#if THREADED
    CancellationTokenSource _cancellationTokenSource;
    Thread[] _tileGenetatorThreads;
#endif

    Vector2 _lastTerrainUpdatePos;
    float _maxViewDistance;

    int lastDebugQueueCount = -1;

    Dictionary<string, TerrainTileDisplay> _activeTileDisplays = new();
    Dictionary<string, TerrainTileDisplay> _previousActiveTileDisplays = new();
    List<TerrainTileDisplay> _notUsedTileDisplays = new();

    public override void OnStartServer()
    {
        base.OnStartServer();

        GeneratorQueueCount = 0;

        _terrainGenerator = StaticObjectAccessor.GetTerrainGenerator();
        _terrainGenerator.SetupGenerator();
#if THREADED
        _cancellationTokenSource = new CancellationTokenSource();

        _tileGenetatorThreads = new Thread[SystemInfo.processorCount];

        for (int i = 0; i < SystemInfo.processorCount; i++)
        {
            var thread = new Thread(GenerateTileOnThread);
            thread.Start();
            _tileGenetatorThreads[i] = thread;
        }
        
#endif
        lastDebugQueueCount = -1;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _lastTerrainUpdatePos = new Vector2(float.MaxValue, float.MaxValue);
        _maxViewDistance = TerrainTilePresets.Last(p => p.DisplayLOD != -1).Distance;
        IsPlayerAreaLoaded = false;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
#if THREADED
        _cancellationTokenSource.Cancel();
        foreach (var thread in _tileGenetatorThreads)
        {
            thread.Join();
        }
        _cancellationTokenSource = null;
        _tileGenetatorThreads = null;
#endif
    }

#if THREADED
    void GenerateTileOnThread()
    {
        var token = _cancellationTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            ProcessGenerateTileInputQueue(token);

            Thread.Sleep(1);
        }
    }
#endif

    void ProcessGenerateTileInputQueue(CancellationToken token)
    {
        if (_terrainGeneratorInputQueue.TryDequeue(out var tileRequest))
        {
            if (tileRequest.tile.CurrentState < tileRequest.requestedState)
            {
                while (tileRequest.tile.CurrentState != tileRequest.requestedState) {
                    GenerateTileNextState(tileRequest.tile);
                }

                if (!token.IsCancellationRequested)
                {
                    _terrainGeneratorOutputQueue.Enqueue(tileRequest.tile);
                }
            }
        }
    }

    public void GenerateTileNextState(TerrainTile tile)
    {
        switch (tile.CurrentState)
        {
            case TerrainTileState.Empty:
                _terrainGenerator.GenerateBiomeMap(tile);
                break;
            case TerrainTileState.BiomeMap:
                _terrainGenerator.GenerateHeightMap(tile);
                break;
            case TerrainTileState.HeightMap:
                _terrainGenerator.BlendHeightMap(tile);
                break;
            case TerrainTileState.BlendedHeightMap:
                _terrainGenerator.GenerateAllMeshData(tile);
                break;
            case TerrainTileState.MeshData:
                _terrainGenerator.AdjustTerrainTileMeshData(tile);
                break;
            case TerrainTileState.AdjustedMeshData:
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
            ProcessGenerateTileInputQueue(CancellationToken.None);
#endif
            ProcessGenerateTileOutputQueue();
        }

        if (IsClient)
        {
            if (Camera.main == null) return;

            Camera.main.farClipPlane = _maxViewDistance;
            var viewPos = Camera.main.transform.position.To2D();

            if (Vector2.Distance(_lastTerrainUpdatePos, viewPos) >= TerrainGenerator.TilePhysicalSizeHalf)
            {
                _lastTerrainUpdatePos = viewPos;

                RequestTilesArountPos(_lastTerrainUpdatePos, TerrainTilePresets);
                UpdateTilesArountClientLastPos(TerrainTilePresets);
            }
        }

        if (lastDebugQueueCount != GeneratorQueueCount)
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

    [ServerRpc(RequireOwnership = false)]
    private void RequestTilesArountPos(Vector2 viewPos, RequiredTileStatePreset[] simDistancePresets)
    {
        var maxViewDistance = simDistancePresets[simDistancePresets.Length - 1];
        var maxDistanceTileCount = Mathf.CeilToInt(maxViewDistance.Distance / TerrainGenerator.TilePhysicalSize);

        var newRequests = new List<(TerrainTile tile, TerrainTileState requestedState)>();
        for (int tX = -maxDistanceTileCount; tX <= maxDistanceTileCount; tX++)
        {
            for (int tY = -maxDistanceTileCount; tY <= maxDistanceTileCount; tY++)
            {
                var posInTile = viewPos + new Vector2(tX * TerrainGenerator.TilePhysicalSize, tY * TerrainGenerator.TilePhysicalSize);

                var tile = GetTileAt(posInTile);
                var tilePreset = tile.SelectTilePreset(viewPos, simDistancePresets);

                //We request a state for a tile if it is within the view distance
                if (tilePreset != null)
                {
                    if (tile.CurrentState < tilePreset.RequiredState)
                    {
                        var request = (tile, tilePreset.RequiredState);

                        newRequests.Add(request);
                    }
                }
            }
        }

        foreach (var request in newRequests.OrderBy(r => Vector2.Distance(viewPos, r.Item1.PhysicalPos)))
        {
            if (request.tile.CurrentState<request.requestedState)
            {
                if (!_terrainGeneratorInputQueue.Contains(request))
                {
                    _terrainGeneratorInputQueue.Enqueue(request);
                }
            }
        }

        UpdateQueueCount();
    }

    [Server]
    void ProcessGenerateTileOutputQueue()
    {
        if (_terrainGeneratorOutputQueue.TryDequeue(out TerrainTile tile))
        {
            if (tile.CurrentState >= TerrainTileState.HeightMap)
            {
                tile.LastChangedTime = Time.realtimeSinceStartup;
                SetChangedTileOnClient(tile);
            }
                

            UpdateQueueCount();
        }
    }

    [Server]
    private void UpdateQueueCount()
    {
        GeneratorQueueCount = _terrainGeneratorInputQueue.Count + _terrainGeneratorOutputQueue.Count;
    }


    [Client]
    private void UpdateTilesArountClientLastPos(RequiredTileStatePreset[] simDistancePresets)
    {
        var maxViewDistance = simDistancePresets.Last();
        var maxDistanceTileCount = Mathf.CeilToInt(maxViewDistance.Distance / TerrainGenerator.TilePhysicalSize);

        _previousActiveTileDisplays = _activeTileDisplays;
        _activeTileDisplays = new Dictionary<string, TerrainTileDisplay>();

        for (int tX = -maxDistanceTileCount; tX <= maxDistanceTileCount; tX++)
        {
            for (int tY = -maxDistanceTileCount; tY <= maxDistanceTileCount; tY++)
            {
                var posInTile = _lastTerrainUpdatePos + new Vector2(tX * TerrainGenerator.TilePhysicalSize, tY * TerrainGenerator.TilePhysicalSize);

                var tile = GetTileAt(posInTile);
                var tilePreset = tile.SelectTilePreset(_lastTerrainUpdatePos, simDistancePresets);

                UpdateDisplayWithPreset(tile, tilePreset);
            }
        }

        IsPlayerAreaLoaded = _activeTileDisplays.Count >= maxDistanceTileCount * maxDistanceTileCount;

        //Debug.Log($"IsPlayerAreaLoaded:{IsPlayerAreaLoaded}, _activeTileDisplays:{_activeTileDisplays.Count}, maxDistanceTileCount2:{maxDistanceTileCount * maxDistanceTileCount}");

        foreach (var pair in _previousActiveTileDisplays)
        {
            if (!_activeTileDisplays.ContainsKey(pair.Key))
            {
                var tileDisplay = pair.Value;

                _notUsedTileDisplays.Add(tileDisplay);

                tileDisplay.gameObject.SetActive(false);
                tileDisplay.ResetDisplay();
            }
        }
        _previousActiveTileDisplays.Clear();
    }

    [ObserversRpc]
    void SetChangedTileOnClient(TerrainTile tile)
    {
        //Debug.Log($"SetChangedTileOnClient {tile}");
        if (!IsServer)
        {
            if (_generatedTiles.ContainsKey(tile.PhysicalPos))
            {
                _generatedTiles[tile.PhysicalPos] = tile;
            }
            else
            {
                _generatedTiles.Add(tile.PhysicalPos, tile);
            }
        }

        var requiredPreset = tile.SelectTilePreset(_lastTerrainUpdatePos, TerrainTilePresets);

        UpdateDisplayWithPreset(tile, requiredPreset);
    }

    [Client]
    void UpdateDisplayWithPreset(TerrainTile tile, RequiredTileStatePreset preset)
    {
        if (!TerrainTileDisplay.IsReadyForDisplay(tile, preset))
        {
            //Debug.Log($"UpdateDisplayWithPreset Hide Tile:{tile}, Preset:{preset}");
            if (_activeTileDisplays.TryGetValue(tile.Id, out var tileDisplay))
            {
                _notUsedTileDisplays.Add(tileDisplay);
                _activeTileDisplays.Remove(tile.Id);

                tileDisplay.gameObject.SetActive(false);
                tileDisplay.ResetDisplay();
            }
        }
        else
        {
            //Debug.Log($"UpdateDisplayWithPreset Show Tile:{tile}, Preset:{preset}");
            if (!_activeTileDisplays.TryGetValue(tile.Id, out var tileDisplay))
            {
                tileDisplay = GetAvailableTerrainDisplay(tile);
                _activeTileDisplays.Add(tile.Id, tileDisplay);
            }
            tileDisplay.gameObject.transform.position = tile.PhysicalCenter.To3D();
            tileDisplay.gameObject.name = tile.Id;
            tileDisplay.gameObject.SetActive(true);

            tileDisplay.Display(tile, preset);
        }
    }

    private TerrainTileDisplay GetAvailableTerrainDisplay(TerrainTile forTile)
    {
        //Debug.Log($"GetAvailableTerrainDisplay {forTile.Id} {_activeTileDisplays.Count} {_previousActiveTileDisplays.Count} {_notUsedTileDisplays.Count}");

        if(_previousActiveTileDisplays.TryGetValue(forTile.Id, out var tileDisplay))
        {
            //Debug.Log($"GetAvailableTerrainDisplay Previous");
            return tileDisplay;
        }
        else if(_notUsedTileDisplays.Count>0)
        {
            //Debug.Log($"GetAvailableTerrainDisplay Not used");
            var notUsed = _notUsedTileDisplays[0];

            _notUsedTileDisplays.RemoveAt(0);

            return notUsed;
        }
        else
        {
            //Debug.Log($"GetAvailableTerrainDisplay New");
            var obj = Instantiate(TilePrefab, Vector3.zero, Quaternion.identity);
            return obj.GetComponent<TerrainTileDisplay>();
        }
    }



    /// <summary>
    /// Return null if the tile not loaded
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public TerrainTile GetTileAt(Vector2 pos)
    {
        var tilePos = TerrainGenerator.AnyPosToTilePos(pos);
        if (!_generatedTiles.TryGetValue(tilePos, out var tile))
        {
            tile = _terrainGenerator.CreateEmptyTile(tilePos);
            _generatedTiles.Add(tilePos, tile);
        }

        return tile;
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.To2D();
        var tile = GetTileAt(posXZ);

        if(tile.CurrentState < TerrainTileState.BlendedHeightMap)
        {
            throw new UnityException($"You can not modify tile in this state {tile}");
        }

        var height = tile.GetHeightAt(posXZ-tile.PhysicalPos);

        return new Vector3(pos.x, height, pos.z);
    }

    //TODO make it async on separated thread, make it general, so it would support any async tile action
    [ServerRpc(RequireOwnership = false)]
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
            if (tile.CurrentState < TerrainTileState.BlendedHeightMap)
            {
                throw new UnityException($"You can not modify tile in this state {tile}");
            }

            if (tile.FlatHeightMap(flatArea, toHeight))
            {
                tile.SetState(TerrainTileState.BlendedHeightMap);
                SetChangedTileOnClient(tile);

                _terrainGeneratorInputQueue.Enqueue((tile, tile.PreviousState));
                UpdateQueueCount();
            }
        }
    }
}
