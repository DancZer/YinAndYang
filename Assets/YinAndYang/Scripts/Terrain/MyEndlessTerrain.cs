using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MyEndlessTerrain : MonoBehaviour
{
    private const int StartChunkCount = 2;
    public ViewDistancePreset[] ViewDistancePreset; 
    [Range(1, 240)] public float ChunkSize = 240;
    private float ChunkSizeHalf; 

    MyTerrainGenerator _terrainGenerator;
    Dictionary<Vector2, MyTerrainChunk> _chunks = new();
    List<MyTerrainChunk> _visibleChunks = new();
    Vector2 LastChunkLoadPos;
    int viewDistanceChunkCount;

    ConcurrentQueue<MyTerrainChunk> _loadFinishedChunk = new();

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        ChunkSizeHalf = ChunkSize / 2f;
        viewDistanceChunkCount = (int)Mathf.Ceil((float)maxViewDistance / ChunkSize);
        LastChunkLoadPos = Vector2.zero;
        LoadChunksViewDistance(Vector2.zero, StartChunkCount, true);
    }

    MyTerrainChunk GetChunkAt(Vector2 pos)
    {
        var area = GetChunkArea(pos);

        if (!_chunks.TryGetValue(area.position, out var chunk))
        {
            chunk = new MyTerrainChunk(area, _terrainGenerator, transform);
            _chunks.Add(area.position, chunk);
        }

        return chunk;
    }
    
    Rect GetChunkArea(Vector2 pos)
    {
        return new Rect(Mathf.Floor(pos.x / ChunkSize) * ChunkSize - ChunkSizeHalf, Mathf.Floor(pos.y / ChunkSize) * ChunkSize- ChunkSizeHalf, ChunkSize, ChunkSize);
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main == null) return;

        var viewPos = Camera.main.transform.position.To2DMapPos();

        LoadChunksViewDistance(viewPos, viewDistanceChunkCount, false);

        if(_loadFinishedChunk.Count > 0)
        {
            if (_loadFinishedChunk.TryDequeue(out var chunk))
            {
                Debug.Log($"Update LoadFinishedChunk remaining {_loadFinishedChunk.Count} {chunk.Bound}");
                chunk.Update();
            }
        }
    }

    void LoadChunksViewDistance(Vector2 viewPos, int chunkCount, bool instant)
    {
        if(_visibleChunks.Count > StartChunkCount * StartChunkCount) { 
            var distanceToChunkEdge = Mathf.Abs(Vector3.Distance(LastChunkLoadPos, viewPos));

            if (distanceToChunkEdge < ChunkSize/2f) return;
            LastChunkLoadPos = viewPos;

            if (LastChunkLoadPos.y > ChunkSize)
            {
                //TODO increase chunk count
            }
        }

        foreach (var chunk in _visibleChunks)
        {
            chunk.HideChunk();
        }
        _visibleChunks.Clear();

        for (int x = -chunkCount; x <= chunkCount; x++)
        {
            for (int z = -chunkCount; z <= chunkCount; z++)
            {
                var chunk = GetChunkAt(LastChunkLoadPos + new Vector2(x*ChunkSize, z*ChunkSize));
                var chunkLOD = GetLOD(chunk, LastChunkLoadPos);

                chunk.DisplayChunkAync(chunkLOD, instant, OnLoadFinished);

                _visibleChunks.Add(chunk);
            }
        }
    }

    private int GetLOD(MyTerrainChunk chunk, Vector3 view)
    {
        var closestPoint = chunk.Bound.ClosestPoint(view);
        var distance = Mathf.Abs(Vector3.Distance(closestPoint, view));

        foreach (var preset in ViewDistancePreset)
        {
            if (distance < preset.ViewDistance)
            {
                return preset.LOD;
            }
        }

        return ViewDistancePreset[ViewDistancePreset.Length - 1].LOD;
    }

    public void OnLoadFinished(MyTerrainChunk chunk)
    {
        Debug.Log($"LoadFinishedChunk remaining {chunk.Bound}");
        if (!_visibleChunks.Contains(chunk)) return;
        _loadFinishedChunk.Enqueue(chunk);
        Debug.Log($"Enqueue LoadFinishedChunk remaining {_loadFinishedChunk.Count} {chunk.Bound}");
    }
}

public class MyTerrainChunk
{
    private const int NoLOD = -1;
    public readonly Bounds Bound;

    readonly Transform _parent;

    readonly TerrainMeshDataLoader _dataLoader;

    GameObject _gameObject;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    MeshCollider _meshCollider;

    bool _objActive = true;
    int _requestedLOD = NoLOD;
    int _displayedLOD = NoLOD;

    public MyTerrainChunk(Rect area, MyTerrainGenerator terrainGenerator, Transform parent)
    {
        _parent = parent;
        _dataLoader = new TerrainMeshDataLoader(this, area, terrainGenerator);

        Bound = area.ToMapBounds();
    }

    public void HideChunk()
    {
        _objActive = false;
        Update();
    }

    public void DisplayChunkAync(int newLOD, bool instant, Action<MyTerrainChunk> callback)
    {
        _objActive = true;
        _requestedLOD = newLOD;

        if (_gameObject == null)
        {
            CreateGameObject();
        }

        if (!_dataLoader.HasMeshData(_requestedLOD))
        {
            if (instant)
            {
                _dataLoader.LoadInstant(_requestedLOD);
                
            }
            else
            {
                _dataLoader.ScheduleLoad(_requestedLOD, callback);
            }
        }

        Update();
    }

    void CreateGameObject()
    {
        _gameObject = new GameObject();
        _gameObject.transform.parent = _parent;
        _gameObject.SetActive(false);

        _meshFilter = _gameObject.AddComponent<MeshFilter>();
        _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
        _meshCollider = _gameObject.AddComponent<MeshCollider>();
    }


    public void Update()
    {
        if (_objActive && _displayedLOD != _requestedLOD && _dataLoader.HasMeshData(_requestedLOD))
        {
            var meshData = _dataLoader.GetMeshData(_requestedLOD);

            //set material, pos, text only once
            if (_displayedLOD == NoLOD) {
                meshData.CreateTexture();
                _gameObject.transform.position = Bound.center;
                _meshRenderer.material = meshData.Material;
                _meshRenderer.material.SetTexture("_MainTex", meshData.Texture);
            }
            meshData.CreateMesh();

            _gameObject.name = "Chunk" + meshData.Name;
            _meshFilter.mesh = _meshCollider.sharedMesh = meshData.Mesh;

            _displayedLOD = _requestedLOD;
        }

        _gameObject.SetActive(_objActive);
    }
}
public class TerrainMeshDataLoader
{
    public bool IsScheduled { get; private set; }

    readonly MyTerrainChunk _chunk;
    readonly Rect _area;
    readonly MyTerrainGenerator _terrainGenerator;
    readonly Dictionary<int, MyTerrainMeshData> _terrainMeshDatas = new ();

    MyTerrainData _terrainData;

    public TerrainMeshDataLoader(MyTerrainChunk chunk, Rect area, MyTerrainGenerator terrainGenerator)
    {
        _chunk = chunk;
        _area = area;
        _terrainGenerator = terrainGenerator;
    }

    public void LoadInstant(int lod)
    {
        if (IsScheduled)
        {
            Debug.LogError($"Can not load instant terrain chunk, because it is alread scheduled {_area} {lod}");
        }

        Debug.Log($"Terrain chunk load instant {_area} {lod}");

        LoadData(lod);
    }
    public void ScheduleLoad(int lod, Action<MyTerrainChunk> callback)
    {
        if (IsScheduled) return;

        Debug.Log($"Terrain chunk load scheduled {_area} {lod}");

        IsScheduled = ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
        {
            try
            {
                LoadData(lod);
                IsScheduled = false;
                Debug.Log($"Terrain chunk schedule load {_area} {lod} finished");
                callback(_chunk);
            }
            catch(Exception e)
            {
                Debug.LogError($"Terrain chunk schedule load failed {_area} {lod} {e.Message} {e.StackTrace}");
            }
        }));

        if (!IsScheduled)
        {
            Debug.LogError($"Failed to scheduled terrain chunk load {_area} {lod}");
        }

    }

    private void LoadData(int lod)
    {
        if (HasMeshData(lod)) return;

        Debug.Log($"ThreadID: {Thread.CurrentThread.ManagedThreadId} for {_area} {lod}");
        
        if (_terrainData == null)
        {
            _terrainData = _terrainGenerator.GenerateTerrainData(_area);
        }

        var meshData = _terrainGenerator.GenerateMeshData(_terrainData, lod);

        SetMeshData(meshData);
    }

    public MyTerrainMeshData GetMeshData(int LOD)
    {
        lock (_terrainMeshDatas)
        {
            return _terrainMeshDatas[LOD];
        }
    }

    public bool HasMeshData(int LOD)
    {
        lock (_terrainMeshDatas)
        {
            return _terrainMeshDatas.ContainsKey(LOD);
        }
    }

    private void SetMeshData(MyTerrainMeshData meshData)
    {
        lock (_terrainMeshDatas)
        {
            _terrainMeshDatas.Add(meshData.LOD, meshData);
        }
    }
}


