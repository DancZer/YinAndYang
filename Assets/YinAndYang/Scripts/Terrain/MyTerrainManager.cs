using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;

public class MyTerrainManager : MonoBehaviour
{
    public float TileSize = 10;
    public int Level = 2;
    public ViewDistancePreset[] ViewDistanceForLevels;
    public LayerMask GroundMask;

    [ReadOnly] public float ChunkSize;

    private MyTerrainTile _root;
    private MyTerrainRenderer _renderer;

    private ViewDistnaceHandler _viewDistnaceHandler;
    private MyTerrainMeshProvider _meshProvider;

    private readonly Dictionary<string, Mesh> _tileMeshCache = new Dictionary<string, Mesh>();

    void Start()
    {
        ChunkSize = TileSize * Mathf.Pow(2, Level);

        _root = new MyTerrainTile(Level, new Rect(ChunkSize / -2, ChunkSize / -2, ChunkSize, ChunkSize));

        _viewDistnaceHandler = new ViewDistnaceHandler(ViewDistanceForLevels);
        _meshProvider = GetComponent<MyTerrainMeshProvider>();
        _renderer = GetComponent<MyTerrainRenderer>();
    }

    void Update()
    {
        if (!InstanceFinder.IsClient || Camera.main == null) return;

        var pos2d = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);

        _viewDistnaceHandler.ChangeCenter(pos2d);

        _root.Update(_viewDistnaceHandler);

        LoadTileMeshRecursive(_root);

        _renderer.Render(_root);
    }

    private void OnDrawGizmos()
    {
        if (_viewDistnaceHandler == null) return;

        _viewDistnaceHandler.OnDrawGizmos();
    }

    public Vector3 GetGroundPosAtCord(Vector3 pos)
    {
        var ray = new Ray(new Vector3(pos.x, 1000, pos.z), Vector3.down);

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, GroundMask))
        {
            return hit.point;
        }
        else
        {
            Debug.LogError($"Ground pos was not found at {pos}");
        }

        return pos;
    }

    private void LoadTileMeshRecursive(MyTerrainTile tile)
    {
        if (tile.IsExpanded)
        {
            LoadTileMeshRecursive(tile.Child00);
            LoadTileMeshRecursive(tile.Child01);
            LoadTileMeshRecursive(tile.Child11);
            LoadTileMeshRecursive(tile.Child10);
        }
        else
        {
            LoadTileMesh(tile);
        }
    }

    private void LoadTileMesh(MyTerrainTile tile)
    {
        if (tile.Mesh != null) return;

        if (!_tileMeshCache.TryGetValue(tile.TileName, out Mesh mesh))
        {
            mesh = _meshProvider.CreateProceduralMesh(tile);
            _tileMeshCache.Add(tile.TileName, mesh);
        }

        tile.Mesh = mesh;
    }

}