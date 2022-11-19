using UnityEngine;
using FishNet.Object;

public class TerrainTileDisplay : MonoBehaviour
{
#if UNITY_EDITOR
    public RequiredTileStatePreset EditorRequiredTileStatePreset;
#endif
    MeshFilter _meshFilter;
    MeshCollider _meshCollider;

    TerrainTile _tile;
    RequiredTileStatePreset _preset;
    float _lastDisplayTime;

    public static bool IsReadyForDisplay(TerrainTile tile, RequiredTileStatePreset preset)
    {
        return
            tile != null && preset != null &&
            (tile.CurrentState >= TerrainTileState.MeshData || tile.PreviousState >= TerrainTileState.MeshData) &&
            preset.DisplayLOD >= 0;
    }

    public void Display(TerrainTile tile, RequiredTileStatePreset preset)
    {
        bool isReadyAndChanged = IsReadyForDisplay(tile, preset) &&
            _tile is null || _preset is null || 
            _tile.Id == tile.Id && _preset == preset && _lastDisplayTime != tile.LastChangedTime;

        if (!isReadyAndChanged)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Skip Display tile:{tile}, _tile:{_tile}, preset:{preset}, _preset:{_preset}, _lastDisplayTime:{_lastDisplayTime}");
#endif
            return;
        }

        //Debug.Log($"Display Tile:{tile}, RequiredTileStatePreset:{preset}");

        var meshData = tile.GetMeshData(preset.DisplayLOD);

        if (Application.isEditor && !Application.isPlaying)
        {
            GetMeshFilter().sharedMesh = meshData.CreateMesh();
        }
        else
        {
            var colliderMeshData = tile.GetMeshData(preset.CollisionLOD);

            GetMeshFilter().mesh = meshData.CreateMesh();
            GetMeshCollider().sharedMesh = colliderMeshData.CreateMesh();
        }

        _tile = tile;
        _preset = preset;
        _lastDisplayTime = tile.LastChangedTime;
    }

    public void ResetDisplay()
    {
        _tile = null;
        _preset = null;
        _lastDisplayTime = 0;
    }

    private MeshFilter GetMeshFilter()
    {
        if (_meshFilter != null) return _meshFilter;

        return _meshFilter = GetComponent<MeshFilter>();
    }

    private MeshCollider GetMeshCollider()
    {
        if (_meshCollider != null) return _meshCollider;

        return _meshCollider = GetComponent<MeshCollider>();
    }
}