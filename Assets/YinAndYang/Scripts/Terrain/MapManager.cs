using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private const int PreLoadTileCount = 2;
    public ViewDistancePreset[] ViewDistancePreset;
    [Range(100, 240)] public float TileSize = 240;
    private float TileSizeHalf;

    Dictionary<Vector2, MapTile> _tiles = new();
    HashSet<MapTile> _visibleTiles = new();
    Vector2 LastDisplayPos;
    int viewDistanceTileCount;

    ConcurrentQueue<MapTile> _loadFinishedChunkQueue = new();
    public bool IsTerrainLoading { get; private set; }

    private void Start()
    {
        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        TileSizeHalf = TileSize / 2f;
        viewDistanceTileCount = (int)Mathf.Ceil((float)maxViewDistance / TileSize);
        LastDisplayPos = Vector2.zero;
        LoadChunksViewDistance(Vector2.zero, PreLoadTileCount, true);
    }

    MapTile GetChunkAt(Vector2 pos, bool loadTerrainInstant)
    {
        var area = GetChunkArea(pos);

        if (!_tiles.TryGetValue(area.position, out var map))
        {
            var gameObject = new GameObject();
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshCollider>();

            map = gameObject.AddComponent<MapTile>();
            map.Initialize(area);

            _tiles.Add(area.position, map);

            if (loadTerrainInstant)
            {
                map.LoadTerrainData();
            }
        }

        return map;
    }

    Rect GetChunkArea(Vector2 pos)
    {
        return new Rect(Mathf.RoundToInt(pos.x / TileSize) * TileSize - TileSizeHalf, Mathf.RoundToInt(pos.y / TileSize) * TileSize - TileSizeHalf, TileSize, TileSize);
    }

    void Update()
    {
        IsTerrainLoading = ProcessAsyncQueue();

        if (Camera.main == null) return;

        var viewPos = Camera.main.transform.position;

        IsTerrainLoading |= LoadChunksViewDistance(viewPos, viewDistanceTileCount, false);
    }

    bool LoadChunksViewDistance(Vector2 viewPos, int chunkCount, bool instant)
    {
        if (_visibleTiles.Count > PreLoadTileCount * PreLoadTileCount)
        {
            var distanceToChunkEdge = Vector2.Distance(LastDisplayPos, viewPos);

            if (distanceToChunkEdge < TileSize / 2f) return false;
            LastDisplayPos = viewPos;

            if (LastDisplayPos.y > TileSize)
            {
                //TODO increase map count
            }
        }

        var newVisibleChunks = new HashSet<MapTile>();

        var isAnyScheduled = false;
        for (int x = -chunkCount; x <= chunkCount; x++)
        {
            for (int z = -chunkCount; z <= chunkCount; z++)
            {
                var map = GetChunkAt(LastDisplayPos + new Vector2(x * TileSize, z * TileSize), instant);

                isAnyScheduled |= DisplayChunk(map);

                newVisibleChunks.Add(map);
            }
        }

        foreach (var map in _visibleTiles)
        {
            if (!newVisibleChunks.Contains(map))
            {
                map.HideTile();
            }
        }
        _visibleTiles = newVisibleChunks;

        return isAnyScheduled;
    }

    int GetLOD(MapTile map, Vector2 view)
    {
        var closestPoint = map.Area.ClosestPoint(view);
        var distance = Vector2.Distance(closestPoint, view);

        foreach (var preset in ViewDistancePreset)
        {
            if (distance < preset.ViewDistance)
            {
                return preset.LOD;
            }
        }

        return ViewDistancePreset[ViewDistancePreset.Length - 1].LOD;
    }

    bool ProcessAsyncQueue()
    {
        if (_loadFinishedChunkQueue.Count > 0)
        {
            if (_loadFinishedChunkQueue.TryDequeue(out var map))
            {
                if (_visibleTiles.Contains(map))
                {
                    DisplayChunk(map);
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool DisplayChunk(MapTile map)
    {
        Debug.Log($"MyTerrainManager.DisplayChunk {map.Area}");

        var chunkLOD = GetLOD(map, LastDisplayPos);
        return map.DisplayChunkAync(chunkLOD, false, OnChunkLoadAsyncFinished);
    }

    void OnChunkLoadAsyncFinished(MapTile map)
    {
        Debug.Log($"MyTerrainManager.OnChunkLoadAsyncFinished {map.Area}");
        _loadFinishedChunkQueue.Enqueue(map);
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.To2D();
        var map = GetChunkAt(posXZ, true);

        var height = map.GetTerrainHeight(posXZ);

        return new Vector3(pos.x, height, pos.z);
    }

    /// <summary>
    /// Flats the terrain for the heigh of the center and for the area
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    public void FlatTerrain(Rect flatArea, float toHeight)
    {
        var chunks = new HashSet<MapTile>();
        chunks.Add(GetChunkAt(flatArea.center, true));

        var newChunk = GetChunkAt(flatArea.position, true);
        if (!chunks.Contains(newChunk))
        {
            chunks.Add(newChunk);
        }
        
        newChunk = GetChunkAt(flatArea.position + new Vector2(flatArea.size.x, 0), true);
        if (!chunks.Contains(newChunk))
        {
            chunks.Add(newChunk);
        }

        newChunk = GetChunkAt(flatArea.position + flatArea.size, true);
        if (!chunks.Contains(newChunk))
        {
            chunks.Add(newChunk);
        }

        newChunk = GetChunkAt(flatArea.position + new Vector2(0, flatArea.size.y), true);
        if (!chunks.Contains(newChunk))
        {
            chunks.Add(newChunk);
        }

        foreach (var map in chunks)
        {
            if (map.FlatTerrain(flatArea, toHeight))
            {
                map.ResetMeshData();
                DisplayChunk(map);
            }
        }
    }
}
