using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MyTerrainChunk : MonoBehaviour
{
    private const int NoLOD = -1;
    public Rect Area { get; private set; }

    TerrainDataLoader _terrainDataLoader;
    TerrainMeshDataLoader _terrainMeshDataLoader;
    Action<MyTerrainChunk> _meshLoadCallback;

    MyTerrainManager _terrainManager;
    MyTerrainGenerator _terrainGenerator;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    MeshCollider _meshCollider;

    int _requestedLOD = NoLOD;
    int _displayedLOD = NoLOD;

    public void Initialize(Rect area)
    {
        Area = area;
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        _terrainManager = StaticObjectAccessor.GetTerrainManager();
        _terrainGenerator = _terrainManager.GetComponent<MyTerrainGenerator>();
        _terrainDataLoader = new TerrainDataLoader(Area, _terrainGenerator);
        _terrainMeshDataLoader = new TerrainMeshDataLoader(_terrainDataLoader, _terrainGenerator);
    }

    public void HideChunk()
    {
        gameObject.SetActive(false);
    }

    public void LoadTerrainData()
    {
        _terrainDataLoader.ExecuteInstant(null);

        Debug.Log($"MyTerrainChunk.LoadTerrainData {Area}");
    }

    public bool DisplayChunkAync(int newLOD, bool instant, Action<MyTerrainChunk> callback)
    {
        gameObject.SetActive(true);

        _requestedLOD = newLOD;
        _meshLoadCallback = callback;

        var scheduled = LoadTerrainAsync(instant);

        if (!scheduled)
        {
            scheduled = LoadTerrainMeshDataAsync(instant);
        }

        if (!scheduled)
        {
            UpdateMesh();
        }

        return scheduled;
    }


    bool LoadTerrainAsync(bool instant)
    {
        if (_terrainDataLoader.HasDataLoaded(null)) return false;

        var scheduled = false;
        
        if (instant)
        {
            _terrainDataLoader.ExecuteInstant(null);
        }
        else
        {
            _terrainDataLoader.Schedule(null, OnTerrainDataLoaded);
            scheduled = true;
        }

        Debug.Log($"MyTerrainChunk.LoadTerrainAsync {Area} {scheduled}");

        return scheduled;
    }

    void OnTerrainDataLoaded()
    {
        LoadTerrainMeshDataAsync(false);
    }

    bool LoadTerrainMeshDataAsync(bool instant)
    {
        if (_terrainMeshDataLoader.HasDataLoaded(_requestedLOD)) return false;

        var scheduled = false;
        if (instant)
        {
            _terrainMeshDataLoader.ExecuteInstant(_requestedLOD);
        }
        else
        {
            _terrainMeshDataLoader.Schedule(_requestedLOD, OnTerrainMeshDataLoaded);
            scheduled = true;
        }

        Debug.Log($"MyTerrainChunk.LoadTerrainMeshDataAsync {Area} {scheduled}");

        return scheduled;
    }

    void OnTerrainMeshDataLoaded()
    {
        _meshLoadCallback.Invoke(this);
    }

    void UpdateMesh()
    {
        if (!gameObject.activeSelf || _requestedLOD == _displayedLOD || !_terrainMeshDataLoader.HasDataLoaded(_requestedLOD)) return;

        Debug.Log($"MyTerrainChunk.UpdateMesh {Area} {_requestedLOD} {_displayedLOD}");

        var meshData = _terrainMeshDataLoader.GetData(_requestedLOD);
        meshData.CreateMesh();

        //set material, pos, text only once
        if (_displayedLOD == NoLOD)
        {
            meshData.CreateTexture();
            transform.position = _terrainDataLoader.Area.center.To3D();
            _meshRenderer.material = meshData.Material;
            _meshRenderer.material.SetTexture("_MainTex", meshData.Texture);
        }
        _meshFilter.mesh = meshData.Mesh;

        var collisionMeshData = _terrainMeshDataLoader.GetData(_requestedLOD + 1);
        collisionMeshData.CreateMesh();
        _meshCollider.sharedMesh = collisionMeshData.Mesh;

        name = "Chunk" + meshData.Name;

        _displayedLOD = _requestedLOD;
    }

    public void ResetMeshData()
    {
        _terrainMeshDataLoader.CancelScheduledJobs();
        _terrainMeshDataLoader.ClearMeshData();
        _displayedLOD = NoLOD;
    }

    public float GetTerrainHeight(Vector2 globalPos)
    {
        _terrainDataLoader.ExecuteInstant(null);

        var localPos = globalPos - _terrainDataLoader.Area.position;

        var terrainData = _terrainDataLoader.GetData(null);

        return terrainData.GetHeightAt(localPos);
    }

    /// <summary>
    /// Returns if the terrain is chainged
    /// </summary>
    /// <param name="flatArea"></param>
    /// <param name="toHeight"></param>
    /// <returns></returns>
    public bool FlatTerrain(Rect flatArea, float toHeight)
    {
        _terrainDataLoader.ExecuteInstant(null);

        var terrainData = _terrainDataLoader.GetData(null);

        return terrainData.FlatHeightMap(flatArea, toHeight);
    }
}

public class TerrainDataLoader : MyJob<object, MyTerrainData>
{
    public readonly Rect Area;
    
    readonly object _dataLock = new();
    readonly MyTerrainGenerator _terrainGenerator;

    MyTerrainData _terrainData;

    public TerrainDataLoader(Rect area, MyTerrainGenerator terrainGenerator)
    {
        Area = area;
        _terrainGenerator = terrainGenerator;
    }

    protected override void Execute(object input, CancellationToken token)
    {
        if (token.IsCancellationRequested || _terrainData != null) return;

        var data = _terrainGenerator.GenerateTerrainData(Area);

        if (token.IsCancellationRequested || _terrainData != null) return;

        lock (_dataLock)
        {
            if (token.IsCancellationRequested || _terrainData != null) return;

            _terrainData = data;
        }
    }

    public override bool HasDataLoaded(object input)
    {
        lock (_dataLock)
        {
            return _terrainData != null;
        }
    }

    public override MyTerrainData GetData(object input)
    {
        lock (_dataLock)
        {
            return _terrainData;
        }
    }

    public override void ClearMeshData()
    {
        throw new NotImplementedException();
    }
}

public class TerrainMeshDataLoader : MyJob<int, MyTerrainMeshData>
{
    readonly object _dataLock = new();

    readonly TerrainDataLoader _terrainDataLoader;
    readonly MyTerrainGenerator _terrainGenerator;
    readonly Dictionary<int, MyTerrainMeshData> _terrainMeshDatas = new();

    public TerrainMeshDataLoader(TerrainDataLoader dataLoader, MyTerrainGenerator terrainGenerator)
    {
        _terrainDataLoader = dataLoader;
        _terrainGenerator = terrainGenerator;
    }

    protected override void Execute(int lod, CancellationToken token)
    {
        if (token.IsCancellationRequested || _terrainMeshDatas.ContainsKey(lod)) return;

        _terrainDataLoader.ExecuteInstant(null);

        var terrainData = _terrainDataLoader.GetData(null);

        var terrainMeshData = _terrainGenerator.GenerateMeshData(terrainData, lod);

        if (token.IsCancellationRequested || _terrainMeshDatas.ContainsKey(lod)) return;

        var colliderMeshData = _terrainGenerator.GenerateMeshData(terrainData, lod + 1);

        if (token.IsCancellationRequested || _terrainMeshDatas.ContainsKey(lod)) return;

        lock (_dataLock)
        {
            if (token.IsCancellationRequested || _terrainMeshDatas.ContainsKey(lod)) return;

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

    public override MyTerrainMeshData GetData(int LOD)
    {
        lock (_dataLock)
        {
            return _terrainMeshDatas[LOD];
        }
    }

    public override bool HasDataLoaded(int lod)
    {
        lock (_dataLock)
        {
            return _terrainMeshDatas.ContainsKey(lod);
        }
    }
    public override void ClearMeshData()
    {
        lock (_dataLock)
        {
            CancelScheduledJobs();

            _terrainMeshDatas.Clear();
        }
    }
}
public abstract class MyJob<TI, TO>
{
    public bool IsScheduled { get; private set; }

    readonly List<CancellationTokenSource> _cancellationTokens = new();
    public void ExecuteInstant(TI input)
    {
        if (IsScheduled)
        {
            CancelScheduledJobs();
        }

        Execute(input, CancellationToken.None);
    }

    protected abstract void Execute(TI input, CancellationToken token);

    public void Schedule(TI input, Action callback)
    {
        var tokenSource = new CancellationTokenSource();
        lock (_cancellationTokens)
        {
            _cancellationTokens.Add(tokenSource);
        }

        IsScheduled = ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
        {
            var cts = (CancellationTokenSource)tokenSource;
            var token = cts.Token;
            try
            {
                if (token.IsCancellationRequested) return;

                Execute(input, token);

                if (!token.IsCancellationRequested)
                {
                    callback();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MyTerrainChunk.ScheduleLoadMesh {e.Message} {e.StackTrace}");
            }
            finally
            {
                lock (_cancellationTokens)
                {
                    _cancellationTokens.Remove(cts);
                }
            }
        }), tokenSource);
    }

    public abstract bool HasDataLoaded(TI input);
    public abstract TO GetData(TI input);
    public abstract void ClearMeshData();

    public bool IsFinished()
    {
        lock (_cancellationTokens)
        {
            return _cancellationTokens.Count == 0;
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