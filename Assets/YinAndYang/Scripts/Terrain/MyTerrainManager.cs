using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Creates new Chunk and set requested visibility and lod
/// </summary>
public class MyTerrainManager : MonoBehaviour
{
    private const int StartChunkCount = 2;
    public ViewDistancePreset[] ViewDistancePreset;
    [Range(100, 240)] public float ChunkSize = 240;
    private float ChunkSizeHalf;

    MyTerrainGenerator _terrainGenerator;
    Dictionary<Vector2, MyTerrainChunk> _chunks = new();
    HashSet<MyTerrainChunk> _visibleChunks = new();
    Vector2 LastChunkLoadPos;
    int viewDistanceChunkCount;

    ConcurrentQueue<MyTerrainChunk> _loadFinishedChunkQueue = new();
    public bool IsTerrainLoading { get; private set; }

    private void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        ChunkSizeHalf = ChunkSize / 2f;
        viewDistanceChunkCount = (int)Mathf.Ceil((float)maxViewDistance / ChunkSize);
        LastChunkLoadPos = Vector2.zero;
        LoadChunksViewDistance(Vector2.zero, StartChunkCount, true);
    }

    MyTerrainChunk GetChunkAt(Vector2 pos, bool loadTerrainInstant)
    {
        var area = GetChunkArea(pos);

        if (!_chunks.TryGetValue(area.position, out var chunk))
        {
            var gameObject = new GameObject();
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshCollider>();

            chunk = gameObject.AddComponent<MyTerrainChunk>();
            chunk.Initialize(area);

            _chunks.Add(area.position, chunk);

            if (loadTerrainInstant)
            {
                chunk.LoadTerrainData();
            }
        }

        return chunk;
    }

    Rect GetChunkArea(Vector2 pos)
    {
        return new Rect(Mathf.RoundToInt(pos.x / ChunkSize) * ChunkSize - ChunkSizeHalf, Mathf.RoundToInt(pos.y / ChunkSize) * ChunkSize - ChunkSizeHalf, ChunkSize, ChunkSize);
    }

    void Update()
    {
        IsTerrainLoading = ProcessAsyncQueue();

        if (Camera.main == null) return;

        var viewPos = Camera.main.transform.position;

        IsTerrainLoading |= LoadChunksViewDistance(viewPos, viewDistanceChunkCount, false);
    }

    bool LoadChunksViewDistance(Vector2 viewPos, int chunkCount, bool instant)
    {
        if (_visibleChunks.Count > StartChunkCount * StartChunkCount)
        {
            var distanceToChunkEdge = Vector2.Distance(LastChunkLoadPos, viewPos);

            if (distanceToChunkEdge < ChunkSize / 2f) return false;
            LastChunkLoadPos = viewPos;

            if (LastChunkLoadPos.y > ChunkSize)
            {
                //TODO increase chunk count
            }
        }

        var newVisibleChunks = new HashSet<MyTerrainChunk>();

        var isAnyScheduled = false;
        for (int x = -chunkCount; x <= chunkCount; x++)
        {
            for (int z = -chunkCount; z <= chunkCount; z++)
            {
                var chunk = GetChunkAt(LastChunkLoadPos + new Vector2(x * ChunkSize, z * ChunkSize), instant);

                isAnyScheduled |= DisplayChunk(chunk);

                newVisibleChunks.Add(chunk);
            }
        }

        foreach (var chunk in _visibleChunks)
        {
            if (!newVisibleChunks.Contains(chunk))
            {
                chunk.HideChunk();
            }
        }
        _visibleChunks = newVisibleChunks;

        return isAnyScheduled;
    }

    int GetLOD(MyTerrainChunk chunk, Vector2 view)
    {
        var closestPoint = chunk.Area.ClosestPoint(view);
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
            if (_loadFinishedChunkQueue.TryDequeue(out var chunk))
            {
                if (_visibleChunks.Contains(chunk))
                {
                    DisplayChunk(chunk);
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool DisplayChunk(MyTerrainChunk chunk)
    {
        Debug.Log($"MyTerrainManager.DisplayChunk {chunk.Area}");

        var chunkLOD = GetLOD(chunk, LastChunkLoadPos);
        return chunk.DisplayChunkAync(chunkLOD, false, OnChunkLoadAsyncFinished);
    }

    void OnChunkLoadAsyncFinished(MyTerrainChunk chunk)
    {
        Debug.Log($"MyTerrainManager.OnChunkLoadAsyncFinished {chunk.Area}");
        _loadFinishedChunkQueue.Enqueue(chunk);
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.To2D();
        var chunk = GetChunkAt(posXZ, true);

        var height = chunk.GetTerrainHeight(posXZ);

        return new Vector3(pos.x, height, pos.z);
    }

    /// <summary>
    /// Flats the terrain for the heigh of the center and for the area
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    public void FlatTerrain(Rect flatArea, float toHeight)
    {
        var chunks = new HashSet<MyTerrainChunk>();
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

        foreach (var chunk in chunks)
        {
            if (chunk.FlatTerrain(flatArea, toHeight))
            {
                chunk.ResetMeshData();
                DisplayChunk(chunk);
            }
        }
    }
}
