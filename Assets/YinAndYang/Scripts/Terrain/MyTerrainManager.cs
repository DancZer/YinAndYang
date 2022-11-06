using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MyTerrainManager : MonoBehaviour
{
    private const int StartChunkCount = 2;
    public ViewDistancePreset[] ViewDistancePreset; 
    [Range(100, 240)] public float ChunkSize = 240;
    private float ChunkSizeHalf; 

    MyTerrainGenerator _terrainGenerator;
    Dictionary<VectorXZ, MyTerrainChunk> _chunks = new();
    List<MyTerrainChunk> _visibleChunks = new();
    VectorXZ LastChunkLoadPos;
    int viewDistanceChunkCount;

    ConcurrentQueue<MyTerrainChunk> _loadFinishedChunk = new();
    public bool IsTerrainLoading { get; private set; }

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        ChunkSizeHalf = ChunkSize / 2f;
        viewDistanceChunkCount = (int)Mathf.Ceil((float)maxViewDistance / ChunkSize);
        LastChunkLoadPos = VectorXZ.zero;
        LoadChunksViewDistance(VectorXZ.zero, StartChunkCount, true);
    }

    MyTerrainChunk GetChunkAt(VectorXZ pos)
    {
        var area = GetChunkArea(pos);

        if (!_chunks.TryGetValue(area.position, out var chunk))
        {
            chunk = new MyTerrainChunk(area, _terrainGenerator, transform);
            _chunks.Add(area.position, chunk);
        }

        return chunk;
    }
    
    RectXZ GetChunkArea(VectorXZ pos)
    {
        return new RectXZ(Mathf.RoundToInt(pos.x / ChunkSize) * ChunkSize - ChunkSizeHalf, Mathf.RoundToInt(pos.z / ChunkSize) * ChunkSize- ChunkSizeHalf, ChunkSize, ChunkSize);
    }

    void Update()
    {
        if (_loadFinishedChunk.Count > 0)
        {
            if (_loadFinishedChunk.TryDequeue(out var chunk))
            {
                Debug.Log($"Update LoadFinishedChunk remaining {_loadFinishedChunk.Count} {chunk.Area}");
                chunk.Update();
            }
            IsTerrainLoading |= true;
        }
        else
        {
            IsTerrainLoading |= false;
        }

        if (Camera.main == null) return;

        var viewPos = Camera.main.transform.position;

        IsTerrainLoading = LoadChunksViewDistance(viewPos, viewDistanceChunkCount, false);
    }

    bool LoadChunksViewDistance(VectorXZ viewPos, int chunkCount, bool instant)
    {
        if(_visibleChunks.Count > StartChunkCount * StartChunkCount) { 
            var distanceToChunkEdge = VectorXZ.Distance(LastChunkLoadPos, viewPos);

            if (distanceToChunkEdge < ChunkSize/2f) return false;
            LastChunkLoadPos = viewPos;

            if (LastChunkLoadPos.z > ChunkSize)
            {
                //TODO increase chunk count
            }
        }

        foreach (var chunk in _visibleChunks)
        {
            chunk.HideChunk();
        }
        _visibleChunks.Clear();

        var isAnyScheduled = false;
        for (int x = -chunkCount; x <= chunkCount; x++)
        {
            for (int z = -chunkCount; z <= chunkCount; z++)
            {
                var chunk = GetChunkAt(LastChunkLoadPos + new VectorXZ(x*ChunkSize, z*ChunkSize));
                var chunkLOD = GetLOD(chunk, LastChunkLoadPos);

                isAnyScheduled |= chunk.DisplayChunkAync(chunkLOD, instant, OnChunkLoadFinished);

                _visibleChunks.Add(chunk);
            }
        }

        return isAnyScheduled;
    }

    private int GetLOD(MyTerrainChunk chunk, VectorXZ view)
    {
        var closestPoint = chunk.Area.ClosestPoint(view);
        var distance = VectorXZ.Distance(closestPoint, view);

        foreach (var preset in ViewDistancePreset)
        {
            if (distance < preset.ViewDistance)
            {
                return preset.LOD;
            }
        }

        return ViewDistancePreset[ViewDistancePreset.Length - 1].LOD;
    }

    public void OnChunkLoadFinished(MyTerrainChunk chunk)
    {
        Debug.Log($"TerrainManager.OnChunkLoadFinished {chunk.Area}");
        if (!_visibleChunks.Contains(chunk)) return;

        _loadFinishedChunk.Enqueue(chunk);
        Debug.Log($"TerrainManager.OnChunkLoadFinished Enqueued {chunk.Area}, remaining {_loadFinishedChunk.Count}");
    }

    public Vector3 GetPosOnTerrain(VectorXZ pos)
    {
        var chunk = GetChunkAt(pos);

        return chunk.GetOnTerrainPos(pos);
    }

    /// <summary>
    /// Flats the terrain for the heigh of the center and for the area
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    public void FlatTerrain(RectXZ flatArea, float flatHeight)
    {
        Debug.Log($"TerrainManager.FlatTerrain {flatArea} {flatHeight} ");

        var chunk0 = GetChunkAt(flatArea.center);
        FlatChunk(chunk0, flatArea, flatHeight);

        var chunk1 = GetChunkAt(flatArea.position);
        FlatChunk(chunk1, flatArea, flatHeight);

        var chunk2 = GetChunkAt(flatArea.position+new VectorXZ(flatArea.size.x,0));
        FlatChunk(chunk2, flatArea, flatHeight);

        var chunk3 = GetChunkAt(flatArea.position + flatArea.size);
        FlatChunk(chunk3, flatArea, flatHeight);

        var chunk4 = GetChunkAt(flatArea.position + new VectorXZ(0, flatArea.size.z));
        FlatChunk(chunk4, flatArea, flatHeight);
    }

    private void FlatChunk(MyTerrainChunk chunk, RectXZ flatArea, float toHeight)
    {
        Debug.Log($"TerrainManager.FlatChunk {chunk.Area} {flatArea} {toHeight}");

        if (chunk.FlatTerrain(flatArea, toHeight))
        {
            Debug.Log($"TerrainManager.FlatChunk Success {chunk.Area} {flatArea} {toHeight}");

            var lod = GetLOD(chunk, LastChunkLoadPos);

            IsTerrainLoading |= chunk.DisplayChunkAync(lod, false, OnChunkLoadFinished);
        }
    }
}

public class MyTerrainChunk
{
    private const int NoLOD = -1;
    public RectXZ Area => _dataLoader.Area;

    public bool IsLoading
    {
        get
        {
            return _dataLoader.TerrainData != null && _dataLoader.IsScheduled || _dataLoader.TerrainData == null;
        }
    }

    readonly Transform _parent;
    readonly TerrainMeshDataLoader _dataLoader;

    GameObject _gameObject;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    MeshCollider _meshCollider;

    bool _objActive = true;
    int _requestedLOD = NoLOD;
    int _displayedLOD = NoLOD;

    public MyTerrainChunk(RectXZ area, MyTerrainGenerator terrainGenerator, Transform parent)
    {
        _parent = parent;
        _dataLoader = new TerrainMeshDataLoader(this, area, terrainGenerator);
    }

    public void HideChunk()
    {
        _objActive = false;
        Update();
    }

    public bool DisplayChunkAync(int newLOD, bool instant, Action<MyTerrainChunk> callback)
    {
        _objActive = true;
        _requestedLOD = newLOD;

        if (_gameObject == null)
        {
            CreateGameObject();
        }

        var scheduled = false;
        
        if (!_dataLoader.HasMeshData(_requestedLOD))
        {
            if (instant)
            {
                _dataLoader.LoadInstantMesh(_requestedLOD);
            }
            else
            {
                _dataLoader.ScheduleLoadMesh(_requestedLOD, callback);
                scheduled = true;
            }
        }

        Update();

        return scheduled;
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
        Debug.Log($"TerrainChunk.Update {Area} {_objActive} {_displayedLOD} {_requestedLOD} {_dataLoader.HasMeshData(_requestedLOD)} {_dataLoader.HasMeshData(_requestedLOD + 1)}");
        if (_objActive && _displayedLOD != _requestedLOD && _dataLoader.HasMeshData(_requestedLOD) && _dataLoader.HasMeshData(_requestedLOD + 1))
        {
            var meshData = _dataLoader.GetMeshData(_requestedLOD);
            meshData.CreateMesh();

            //set material, pos, text only once
            if (_displayedLOD == NoLOD) {
                meshData.CreateTexture();
                _gameObject.transform.position = Area.center;
                _meshRenderer.material = meshData.Material;
                _meshRenderer.material.SetTexture("_MainTex", meshData.Texture);
            }
            _meshFilter.mesh = meshData.Mesh;

            var collisionMeshData = _dataLoader.GetMeshData(_requestedLOD + 1);
            collisionMeshData.CreateMesh();
            _meshCollider.sharedMesh = collisionMeshData.Mesh;

            _gameObject.name = "Chunk" + meshData.Name;

            _displayedLOD = _requestedLOD;
        }

        _gameObject.SetActive(_objActive);
    }

    public Vector3 GetOnTerrainPos(VectorXZ globalPos)
    {
        _dataLoader.LoadTerrainData();

        var localPos = globalPos - _dataLoader.Area.position;

        var height = _dataLoader.TerrainData.GetHeightAt(localPos);

        return new Vector3(globalPos.x, height, globalPos.z);
    }

    public bool FlatTerrain(RectXZ flatArea, float toHeight)
    {
        _dataLoader.LoadTerrainData();

        Debug.Log($"TerrainChunk.FlatTerrain {Area} {flatArea} {toHeight}");

        if (_dataLoader.TerrainData.FlatHeightMap(flatArea, toHeight)) {
            Debug.Log($"TerrainChunk.FlatTerrain success {Area}");

            _dataLoader.IsTerrainDataModified = true;
            _dataLoader.ClearMeshData();

            _displayedLOD = NoLOD;

            return true;
        }
        else
        {
            return false;
        }
    }
}
public class TerrainMeshDataLoader
{
    public readonly RectXZ Area;
    public readonly MyTerrainChunk Chunk;
    public MyTerrainData TerrainData { get; private set; }
    public bool IsTerrainDataModified;
    public bool IsScheduled { get; private set; }
    
    readonly MyTerrainGenerator _terrainGenerator;
    readonly Dictionary<int, MyTerrainMeshData> _terrainMeshDatas = new ();

    public TerrainMeshDataLoader(MyTerrainChunk chunk, RectXZ area, MyTerrainGenerator terrainGenerator)
    {
        Chunk = chunk;
        Area = area;
        _terrainGenerator = terrainGenerator;
    }

    public void LoadInstantMesh(int lod)
    {
        if (IsScheduled)
        {
            Debug.LogError($"Can not load instant terrain chunk, because it is alread scheduled {Area} {lod}");
        }

        Debug.Log($"Terrain chunk load instant {Area} {lod}");

        LoadTerrainData();
        LoadMeshData(lod);
    }
    public void ScheduleLoadMesh(int lod, Action<MyTerrainChunk> callback)
    {
        if (IsScheduled) return;

        Debug.Log($"Terrain chunk load scheduled {Area} {lod}");

        IsScheduled = ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
        {
            try
            {
                LoadTerrainData();
                LoadMeshData(lod);
                IsScheduled = false;
                Debug.Log($"Terrain chunk schedule load {Area} {lod} finished");
                callback(Chunk);
            }
            catch(Exception e)
            {
                Debug.LogError($"Terrain chunk schedule load failed {Area} {lod} {e.Message} {e.StackTrace}");
            }
        }));

        if (!IsScheduled)
        {
            Debug.LogError($"Failed to scheduled terrain chunk load {Area} {lod}");
        }

    }
    public void LoadTerrainData()
    {
        if (TerrainData == null)
        {
            TerrainData = _terrainGenerator.GenerateTerrainData(Area);
        }
    }

    private void LoadMeshData(int lod)
    {
        if (HasMeshData(lod)) return;
        IsTerrainDataModified = false;

        var meshData = _terrainGenerator.GenerateMeshData(TerrainData, lod);
        var meshDataForCollision = _terrainGenerator.GenerateMeshData(TerrainData, lod+1);

        if (!IsTerrainDataModified) { 
            SetMeshData(meshData);
            SetMeshData(meshDataForCollision);
        }
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

    internal void ClearMeshData()
    {
        lock (_terrainMeshDatas)
        {
            _terrainMeshDatas.Clear();
        }
    }
}


