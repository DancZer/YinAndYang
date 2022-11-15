using UnityEngine;
using FishNet.Object;

public class TerrainTileDisplay : NetworkBehaviour
{
#if UNITY_EDITOR
    public ViewDistancePreset EditorViewDistance;
#endif
    TerrainTile _tile;
    ViewDistancePreset displayedPreset;

    MeshFilter _meshFilter;
    MeshCollider _meshCollider;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
    }

    public void SetTile(TerrainTile tile)
    {
        _tile = tile;
        displayedPreset = null;
    }

    public void Display(ViewDistancePreset preset)
    {
        if (_tile == null || displayedPreset == preset) return;

        var meshData = _tile.GetMeshData(preset.DisplayLOD);

        if (Application.isEditor)
        {
            GetComponent<MeshFilter>().sharedMesh = meshData.GetMesh();
        }
        else
        {
            var colliderMeshData = _tile.GetMeshData(preset.CollisionLOD);

            _meshFilter.mesh = meshData.GetMesh();
            _meshCollider.sharedMesh = colliderMeshData.GetMesh();
        }

        displayedPreset = preset;
    }
}