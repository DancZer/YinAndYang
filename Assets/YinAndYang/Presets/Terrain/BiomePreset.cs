using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Terrain Biome", menuName = "Scriptables/TerrainBiome", order = 3)]
public class BiomePreset : ScriptableObject
{
	public int TexId;

	public float BaseHeight = 0;
	public float HeightMultiplier = 100;

	public bool UseHeightCurve;
	public AnimationCurve HeightCurve;
	public BiomeLayer[] Layers;

	public FastNoiseLite.NoiseType NoiseType;
	public FastNoiseLite.FractalType FractalType;
		
	public float Frequency = 0.5f;
	[Range(1, 10)]
	public int Octaves = 3;
	public float Gain = 2;
	public float Lacunarity = 2;
	public Color BiomeColor;
}

[System.Serializable]
public struct BiomeLayer
{
	[Range(0f, 1f)]
	public float BaseBlend;
	public float BaseStartHeight;

	public Color BaseColor;
	[Range(0f, 1f)]
	public float BaseColorStrength;
}