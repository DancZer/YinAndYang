using UnityEngine;
using System.Linq;

public class TerrainTileDisplay : MonoBehaviour
{
    MeshFilter _meshFilter;
    MeshCollider _meshCollider;
    MeshRenderer _meshRenderer;

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

    public void Display(TerrainTile tile, RequiredTileStatePreset preset, BiomeLayerData biomeData)
    {
        bool isReadyAndChanged = IsReadyForDisplay(tile, preset) &&
            (_tile is null || _preset is null || _tile != tile || _preset != preset ||
            _tile == tile && _preset == preset && _lastDisplayTime != tile.LastChangedTime);

        if (!isReadyAndChanged)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Skip Display tile:{tile}, _tile:{_tile}, preset:{preset}, _preset:{_preset}, _lastDisplayTime:{_lastDisplayTime}");
#endif
            return;
        }

        var meshData = tile.GetMeshData(preset.DisplayLOD);

        if (Application.isEditor && !Application.isPlaying)
        {
            GetMeshFilter().sharedMesh = meshData.CreateMesh();
            SetupMaterial(GetMeshRenderer().sharedMaterial, tile, biomeData);
        }
        else
        {
            var colliderMeshData = tile.GetMeshData(preset.CollisionLOD);

            GetMeshFilter().mesh = meshData.CreateMesh();
            GetMeshCollider().sharedMesh = colliderMeshData.CreateMesh();
            SetupMaterial(GetMeshRenderer().material, tile, biomeData);
        }

        _tile = tile;
        _preset = preset;
        _lastDisplayTime = tile.LastChangedTime;
    }

    private void SetupMaterial(Material material, TerrainTile tile, BiomeLayerData biomeData)
    {
        Debug.Log($"SetupMaterial Tile:{tile}, BiomeData:{biomeData}");

        material.SetInteger("_BiomeCount", biomeData.BiomeCount);
        material.SetFloatArray("_BiomeTexIds", biomeData.BiomeTexIds.Select(id => (float)id).ToArray());
        material.SetFloatArray("_BiomeLayerCounts", biomeData.LayerCounts.Select(c => (float)c).ToArray());
        material.SetFloatArray("_BiomeMinHeights", biomeData.MinHeights);
        material.SetFloatArray("_BiomeMaxHeights", biomeData.MaxHeights);
        
        material.SetFloatArray("_BiomesBaseBlends", biomeData.BaseBlendFlat2D);
        material.SetFloatArray("_BiomesBaseStartHeights", biomeData.BaseStartHeightFlat2D);
        material.SetColorArray("_BiomesBaseColors", biomeData.BaseColorFlat2D);
        material.SetFloatArray("_BiomesBaseColorStrengths", biomeData.BaseColorStrengthFlat2D);
        
        material.SetTexture("_BiomeMapWeightTex", tile.CreateBiomeMapWeightTexArray(biomeData));
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

    private MeshRenderer GetMeshRenderer()
    {
        if (_meshRenderer != null) return _meshRenderer;

        return _meshRenderer = GetComponent<MeshRenderer>();
    }
}