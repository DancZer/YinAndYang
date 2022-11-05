using System;
using System.Collections.Generic;
using UnityEngine;

public class MyEndlessTerrain : MonoBehaviour
{
    public ViewDistancePreset[] ViewDistancePreset; 
    [Range(1, 240)] public int ChunkSize = 240;
    [Range(1, 200)] public int LoadChunksAtEdgeDistance = 20;

    private MyTerrainGenerator _terrainGenerator;
    private Dictionary<Vector2, MyTerrainChunk> _chunks = new();
    private List<MyTerrainChunk> _lastVisibleChunks = new();

    [ReadOnly] public int VisibleChunkCount;
    [ReadOnly] public Vector2 LastChunkLoadPos;
    [ReadOnly] public List<Vector2> VisibleChunksPos = new();

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        var maxViewDistance = ViewDistancePreset[ViewDistancePreset.Length - 1].ViewDistance;
        VisibleChunkCount = (int)Mathf.Ceil((float)maxViewDistance / ChunkSize);
        
        LoadChunksViewDistance(Vector2.zero, VisibleChunkCount, true);
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
        return new Rect(Mathf.Floor(pos.x / ChunkSize) * ChunkSize, Mathf.Floor(pos.y / ChunkSize) * ChunkSize, ChunkSize, ChunkSize);
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main == null) return;

        var viewPos = Camera.main.transform.position.To2DMapPos();

        LoadChunksViewDistance(viewPos, VisibleChunkCount, false);
    }

    void LoadChunksViewDistance(Vector2 viewPos, int chunkCount, bool instant)
    {
        var currentChunk = GetChunkAt(viewPos);

        var chunkEdgePos = currentChunk.Bound.ClosestPoint(viewPos);
        var distanceToChunkEdge = Mathf.Abs(Vector3.Distance(chunkEdgePos, viewPos));

        //do not load new chunks if it is not moved near the edge
        if (distanceToChunkEdge > LoadChunksAtEdgeDistance) return;

        if (viewPos.y > ChunkSize)
        {
            //TODO increase chunk count
        }

        foreach (var chunk in _lastVisibleChunks)
        {
            chunk.HideChunk();
        }
        _lastVisibleChunks.Clear();

        for (int x = -chunkCount; x < chunkCount; x++)
        {
            for (int z = -chunkCount; z < chunkCount; z++)
            {
                var chunk = GetChunkAt(viewPos + new Vector2(x*ChunkSize, z*ChunkSize));
                var chunkLOD = GetLOD(chunk, viewPos);

                chunk.DisplayChunkAync(chunkLOD, instant);

                _lastVisibleChunks.Add(chunk);
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
}

public class MyTerrainChunk
{
    public readonly Bounds Bound;

    Rect _area;
    Transform _parent;
    MyTerrainGenerator _terrainGenerator;

    MyTerrainData _terrainData;
    MyTerrainMeshData _meshData;
    GameObject _gameObject;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    MeshCollider _meshCollider;

    int _meshDataLOD;
    bool _requestedMeshData;
    bool _requestedTerrainData;
    bool _meshChanged;

    bool _objActive = true;

    public MyTerrainChunk(Rect area, MyTerrainGenerator terrainGenerator, Transform parent)
    {
        _area = area;       
        _terrainGenerator = terrainGenerator;
        _parent = parent;

        Bound = _area.ToMapBounds();
    }

    public void HideChunk()
    {
        _objActive = false;
        UpdateGameObject();
    }

    public void DisplayChunkAync(int withLOD, bool instant)
    {
        _objActive = true;
        
        if(_terrainData == null && !_requestedTerrainData) { 
            RequestTerrainData(instant);
        }

        if (_objActive && (_meshData == null || _meshDataLOD != withLOD) && !_requestedMeshData)
        {
            RequestMeshData(withLOD, instant);
        }

        if(_gameObject == null)
        {
            CreateGameObject();
        }
        UpdateGameObject();
    }
    void RequestTerrainData(bool instant)
    {
        _requestedTerrainData = true;

        _terrainData = _terrainGenerator.GenerateTerrainData(_area);

        _requestedTerrainData = false;
    }

    void RequestMeshData(int lod, bool instant)
    {
        _requestedMeshData = true;
        _meshDataLOD = lod;

        _meshData = _terrainGenerator.GenerateMeshData(_terrainData, _meshDataLOD);
        _meshChanged = true;

        _requestedMeshData = false;
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

    void UpdateGameObject()
    {
        if (_objActive && _meshChanged) {
            _gameObject.transform.position = Bound.center;
            _meshFilter.mesh = _meshCollider.sharedMesh = _meshData.CreateMesh();
            _meshRenderer.material = _terrainGenerator.BaseMaterial;
            _meshRenderer.material.SetTexture("_MainTex", _meshData.CreateTexture());
            _meshChanged = false;
            _gameObject.name = $"Area:{_area}_LOD:{_meshDataLOD}";
        }

        _gameObject.SetActive(_objActive);
    }
}

