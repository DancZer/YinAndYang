using UnityEngine;

public class BiomeTerrainHeightGenerator
{
	public const int TileCount = 16;
	public const float TileUVStep = 1f / TileCount;

	public readonly float PhysicalMinHeight;
	public readonly float PhysicalMaxHeight;

	readonly BiomePreset _biome;
	readonly AnimationCurve _heightCurve;
	readonly FastNoiseLite _noise;

	public BiomeTerrainHeightGenerator(BiomePreset biome, int seed)
	{
		_biome = biome;

		_heightCurve = new AnimationCurve(_biome.HeightCurve.keys);
		_noise = new FastNoiseLite(seed);

		_noise.SetNoiseType(_biome.NoiseType);
		_noise.SetFractalType(_biome.FractalType);
		_noise.SetFrequency(_biome.Frequency);
		_noise.SetFractalOctaves(_biome.Octaves);
		_noise.SetFractalGain(_biome.Gain);
		_noise.SetFractalLacunarity(_biome.Lacunarity);

		PhysicalMinHeight = GetHeightForNoiseVal(-1f);
		PhysicalMaxHeight = GetHeightForNoiseVal(1f);
	}

	public float GetTerrainHeight(float pX, float pY)
	{
		return GetHeightForNoiseVal(_noise.GetNoise(pX, pY));
	}

	public float GetHeightForNoiseVal(float val)
	{
		if (_biome.UseHeightCurve)
		{
			val = _heightCurve.Evaluate(val);
		}

		return _biome.BaseHeight + val * _biome.HeightMultiplier;
	}
}