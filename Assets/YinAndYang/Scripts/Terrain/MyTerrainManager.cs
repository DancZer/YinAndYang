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
    Dictionary<Vector2, MyTerrainChunk> _chunks = new();
    List<MyTerrainChunk> _visibleChunks = new();
    Vector2 LastChunkLoadPos;
    int viewDistanceChunkCount;

    ConcurrentQueue<MyTerrainChunk> _loadFinishedChunk = new();
    public bool IsTerrainLoading { get; private set; }

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
        return new Rect(Mathf.RoundToInt(pos.x / ChunkSize) * ChunkSize - ChunkSizeHalf, Mathf.RoundToInt(pos.y / ChunkSize) * ChunkSize- ChunkSizeHalf, ChunkSize, ChunkSize);
    }

    void Update()
    {
        IsTerrainLoading = false;
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

        IsTerrainLoading |= LoadChunksViewDistance(viewPos, viewDistanceChunkCount, false);
    }

    bool LoadChunksViewDistance(Vector2 viewPos, int chunkCount, bool instant)
    {
        if(_visibleChunks.Count > StartChunkCount * StartChunkCount) { 
            var distanceToChunkEdge = Vector2.Distance(LastChunkLoadPos, viewPos);

            if (distanceToChunkEdge < ChunkSize/2f) return false;
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

        var isAnyScheduled = false;
        for (int x = -chunkCount; x <= chunkCount; x++)
        {
            for (int z = -chunkCount; z <= chunkCount; z++)
            {
                var chunk = GetChunkAt(LastChunkLoadPos + new Vector2(x*ChunkSize, z*ChunkSize));
                var chunkLOD = GetLOD(chunk, LastChunkLoadPos);

                isAnyScheduled |= chunk.DisplayChunkAync(chunkLOD, instant, OnChunkLoadFinished);

                _visibleChunks.Add(chunk);
            }
        }

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

    void OnChunkLoadFinished(MyTerrainChunk chunk)
    {
        Debug.Log($"TerrainManager.OnChunkLoadFinished {chunk.Area}");
        if (!_visibleChunks.Contains(chunk)) return;

        _loadFinishedChunk.Enqueue(chunk);
        Debug.Log($"TerrainManager.OnChunkLoadFinished Enqueued {chunk.Area}, remaining {_loadFinishedChunk.Count}");
    }

    public Vector3 GetPosOnTerrain(Vector3 pos)
    {
        var posXZ = pos.ToXZ();
        var chunk = GetChunkAt(posXZ);

        var height = chunk.GetTerrainHeight(posXZ);

        return new Vector3(pos.x, height, pos.z);
    }

    /// <summary>
    /// Flats the terrain for the heigh of the center and for the area
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    public void FlatTerrain(Rect flatArea, float flatHeight)
    {
        Debug.Log($"TerrainManager.FlatTerrain {flatArea} {flatHeight} ");

        var chunk0 = GetChunkAt(flatArea.center);
        FlatChunk(chunk0, flatArea, flatHeight);

        var chunk1 = GetChunkAt(flatArea.position);
        FlatChunk(chunk1, flatArea, flatHeight);

        var chunk2 = GetChunkAt(flatArea.position+new Vector2(flatArea.size.x,0));
        FlatChunk(chunk2, flatArea, flatHeight);

        var chunk3 = GetChunkAt(flatArea.position + flatArea.size);
        FlatChunk(chunk3, flatArea, flatHeight);

        var chunk4 = GetChunkAt(flatArea.position + new Vector2(0, flatArea.size.y));
        FlatChunk(chunk4, flatArea, flatHeight);
    }

    void FlatChunk(MyTerrainChunk chunk, Rect flatArea, float toHeight)
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
    public Rect Area => _dataLoader.Area;

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

    public MyTerrainChunk(Rect area, MyTerrainGenerator terrainGenerator, Transform parent)
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
                _dataLoader.LoadMeshInstant(_requestedLOD);
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
                _gameObject.transform.position = Area.center.To3D();
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

    public float GetTerrainHeight(Vector2 globalPos)
    {
        _dataLoader.LoadTerrainDataInstant();

        var localPos = globalPos - _dataLoader.Area.position;

        return _dataLoader.TerrainData.GetHeightAt(localPos);
    }

    public bool FlatTerrain(Rect flatArea, float toHeight)
    {
        _dataLoader.LoadTerrainDataInstant();

        Debug.Log($"TerrainChunk.FlatTerrain {Area} {flatArea} {toHeight}");

        if (_dataLoader.TerrainData.FlatHeightMap(flatArea, toHeight)) {
            Debug.Log($"TerrainChunk.FlatTerrain success {Area}");

            _dataLoader.CancelScheduledJobs();
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

    public readonly Rect Area;
    public readonly MyTerrainChunk Chunk;
    public MyTerrainData TerrainData { get; private set; }
    public bool IsScheduled { get; private set; }

    readonly object _terrainDataLockObj = new();
    readonly MyTerrainGenerator _terrainGenerator;
    readonly Dictionary<int, MyTerrainMeshData> _terrainMeshDatas = new ();

    readonly List<CancellationTokenSource> _cancellationTokens = new ();

    public TerrainMeshDataLoader(MyTerrainChunk chunk, Rect area, MyTerrainGenerator terrainGenerator)
    {
        Chunk = chunk;
        Area = area;
        _terrainGenerator = terrainGenerator;
    }

    public void LoadMeshInstant(int lod)
    {
        if (IsScheduled)
        {
            CancelScheduledJobs();
            Debug.LogWarning($"TerrainMeshDataLoader.LoadMeshInstant {Area} {lod} load is already scheduled. Scheduled result is blocked!");
        }

        Debug.Log($"TerrainMeshDataLoader.LoadMeshInstant {Area} {lod}");

        LoadTerrainData(CancellationToken.None);
        LoadMeshData(lod, CancellationToken.None);
        Debug.Log($"TerrainMeshDataLoader.LoadMeshInstant {Area} {lod} finished");
    }

    public void ScheduleLoadMesh(int lod, Action<MyTerrainChunk> callback)
    {
        var tokenSource = new CancellationTokenSource();
        lock (_cancellationTokens)
        {
            _cancellationTokens.Add(tokenSource);
        }

        Debug.Log($"TerrainMeshDataLoader.ScheduleLoadMesh {Area} {lod}");

        IsScheduled = ThreadPool.QueueUserWorkItem(new WaitCallback( param =>
        {
            var cts = (CancellationTokenSource)tokenSource;
            var token = cts.Token;
            try
            {
                if (token.IsCancellationRequested) return;

                LoadTerrainData(token);
                LoadMeshData(lod, token);

                if (token.IsCancellationRequested) 
                {
                    Debug.LogWarning($"TerrainMeshDataLoader.ScheduleLoadMesh {Area} scheduled result blocked! Callback is not triggered.");
                }
                else
                {
                    Debug.Log($"TerrainMeshDataLoader.ScheduleLoadMesh {Area} {lod} finished");
                    callback(Chunk);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"TerrainMeshDataLoader.ScheduleLoadMesh {Area} {lod} {e.Message} {e.StackTrace}");
            }
            finally
            {
                lock (_cancellationTokens)
                {
                    _cancellationTokens.Remove(cts);
                }
            }
        }), tokenSource);

        if (!IsScheduled)
        {
            Debug.LogError($"TerrainMeshDataLoader.ScheduleLoadMesh failed to start {Area} {lod}");
        }

    }

    public void LoadTerrainDataInstant()
    {
        if (IsScheduled)
        {
            CancelScheduledJobs();
            Debug.LogWarning($"TerrainMeshDataLoader.LoadMeshInstant {Area} load is already scheduled. Scheduled result is blocked!");
        }

        LoadTerrainData(CancellationToken.None);
    }

    void LoadTerrainData(CancellationToken token)
    {
        var terrainData = _terrainGenerator.GenerateTerrainData(Area);

        SetTerrainData(terrainData, token);
    }

    void SetTerrainData(MyTerrainData terrainData, CancellationToken token)
    {
        if (TerrainData == null)
        {
            if (token.IsCancellationRequested) return;

            lock (_terrainDataLockObj)
            {
                if (TerrainData == null)
                {
                    if (token.IsCancellationRequested) return;
                    TerrainData = terrainData;
                }
            }
        }
    }

    void LoadMeshData(int lod, CancellationToken token)
    {
        if (_terrainMeshDatas.ContainsKey(lod)) return;

        var meshData = _terrainGenerator.GenerateMeshData(TerrainData, lod);
        var meshDataForCollision = _terrainGenerator.GenerateMeshData(TerrainData, lod + 1);

        if (token.IsCancellationRequested)
        {
            Debug.LogWarning($"TerrainMeshDataLoader.LoadMeshData {Area} scheduled result blocked!");
            return;
        }
         
        SetMeshData(meshData, meshDataForCollision, token);
    }

    void SetMeshData(MyTerrainMeshData terrainMeshData, MyTerrainMeshData colliderMeshData, CancellationToken token)
    {
        lock (_terrainMeshDatas)
        {
            if (token.IsCancellationRequested) return;

            if (!_terrainMeshDatas.ContainsKey(terrainMeshData.LOD))
            {
                _terrainMeshDatas.Add(terrainMeshData.LOD, terrainMeshData);
            }

            if (!_terrainMeshDatas.ContainsKey(colliderMeshData.LOD))
            {
                _terrainMeshDatas.Add(colliderMeshData.LOD, colliderMeshData);
            }
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

    public void ClearMeshData()
    {
        lock (_terrainMeshDatas)
        {
            _terrainMeshDatas.Clear();
        }
    }

    public void CancelScheduledJobs()
    {
        lock (_cancellationTokens)
        {
            foreach (var token in _cancellationTokens)
            {
                token.Cancel();
            }
        
            _cancellationTokens.Clear();
        }
    }
}


