using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Terrain Biome", menuName = "Scriptables/TerrainBiome", order = 3)]
public class BiomePreset : ScriptableObject
{
	public float BaseHeight = 0;
	public float HeightMultiplier = 100;

	public bool UseHeightCurve;
	public AnimationCurve HeightCurve;
	public MyTerrainRegionPreset[] Regions;
	public int LayerCount;

	public FastNoiseLite.NoiseType NoiseType;
	public FastNoiseLite.FractalType FractalType;
	[Range(0.00001f, 100000)]
	public float Frequency = 0.5f;
	[Range(1, 10)]
	public int Octaves = 3;
	[Range(0.0001f, 1000)]
	public float Gain = 2;
	[Range(0.0001f, 1000)]
	public float Lacunarity = 2;

	public Color ColorInEditor;
}
