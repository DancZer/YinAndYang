using UnityEngine;
using System.Linq;

public class TerrainTileDisplay : MonoBehaviour
{
    public TerrainTileStatePreset LastPreset;

    MeshFilter _meshFilter;
    MeshCollider _meshCollider;
    MeshRenderer _meshRenderer;

    TerrainTile _tile;
    
    float _lastDisplayTime;

    public static bool IsReadyForDisplay(TerrainTile tile, TerrainTileStatePreset preset)
    {
        return
            tile != null && preset != null &&
            (tile.CurrentState >= TerrainTileState.MeshData || tile.PreviousState >= TerrainTileState.MeshData) &&
            preset.DisplayLOD >= 0;
    }

    public void Display(TerrainTile tile, TerrainTileStatePreset preset, BiomeLayerData biomeData)
    {
        bool isReadyAndChanged = IsReadyForDisplay(tile, preset) &&
            (_tile is null || LastPreset is null || _tile != tile || LastPreset != preset ||
            _tile == tile && LastPreset == preset && _lastDisplayTime != tile.LastChangedTime);

        if (!isReadyAndChanged) return;

        var meshData = tile.GetMeshData(preset.DisplayLOD);

        Debug.Log($"Display: {tile}, {preset}");
        

        var mesh = meshData.CreateMesh();

        if (Application.isEditor && !Application.isPlaying)
        {
            GetMeshFilter().sharedMesh = mesh;
            SetupMaterial(GetMeshRenderer().sharedMaterial, tile, biomeData);
        }
        else
        {
            var colliderMeshData = tile.GetMeshData(preset.CollisionLOD);

            GetMeshFilter().mesh = mesh;
            GetMeshCollider().sharedMesh = colliderMeshData.CreateMesh();
            SetupMaterial(GetMeshRenderer().material, tile, biomeData);
        }

        _tile = tile;
        LastPreset = preset;
        _lastDisplayTime = tile.LastChangedTime;
    }

    private void SetupMaterial(Material material, TerrainTile tile, BiomeLayerData biomeData)
    {
        //Debug.Log($"SetupMaterial Tile:{tile}, BiomeData:{biomeData}");

        material.SetInteger("_BiomeCount", biomeData.BiomeCount);
        material.SetFloatArray("_BiomeLayerTexIdx", biomeData.BiomeLayerTexIdx.Select(id => (float)id).ToArray());
        material.SetColorArray("_BiomeColors", biomeData.BiomeColor);
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
        LastPreset = null;
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