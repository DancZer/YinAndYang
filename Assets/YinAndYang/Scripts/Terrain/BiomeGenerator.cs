using UnityEngine;

public class BiomeGenerator
{
	public readonly BiomeData BiomeData;

	readonly FastNoiseLite _biomeMapNoise;
	readonly BiomePreset[] _biomePresets;
	readonly BiomeTerrainHeightGenerator[] _biomeGenerators;


	public BiomeGenerator(
		BiomePreset[] biomes,
		int seed,
		float biomeSize,
		FastNoiseLite.CellularDistanceFunction distanceFunction,
		float biomeJitter,
		FastNoiseLite.DomainWarpType warpType,
		float wrapAmp,
		FastNoiseLite.FractalType fractalType,
		int fractalOctaves,
		float fractalLacunarity,
		float fractalGain)
	{
		_biomeMapNoise = new FastNoiseLite(seed);
		_biomeMapNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
		_biomeMapNoise.SetFrequency(1f / biomeSize);
		_biomeMapNoise.SetCellularDistanceFunction(distanceFunction);
		_biomeMapNoise.SetCellularJitter(biomeJitter);
		_biomeMapNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);

		_biomeMapNoise.SetDomainWarpType(warpType);
		_biomeMapNoise.SetDomainWarpAmp(wrapAmp);

		_biomeMapNoise.SetFractalType(fractalType);
		_biomeMapNoise.SetFractalOctaves(fractalOctaves);
		_biomeMapNoise.SetFractalLacunarity(fractalLacunarity);
		_biomeMapNoise.SetFractalGain(fractalGain);

		BiomeData = new BiomeData
		{
			Count = biomes.Length
		};
		BiomeData.MinHeights = new float[BiomeData.Count];
		BiomeData.MaxHeights = new float[BiomeData.Count];
		BiomeData.LayerCounts = new float[BiomeData.Count];

		_biomePresets = new BiomePreset[BiomeData.Count];
		_biomeGenerators = new BiomeTerrainHeightGenerator[BiomeData.Count];
		for (int biomeId = 0; biomeId < BiomeData.Count; biomeId++)
		{
			var biome = biomes[biomeId];
			var generator = new BiomeTerrainHeightGenerator(biome, seed);

			_biomeGenerators[biomeId] = generator;
			_biomePresets[biomeId] = biome;

			BiomeData.MinHeights[biomeId] = generator.GetHeightForNoiseVal(-1);
			BiomeData.MaxHeights[biomeId] = generator.GetHeightForNoiseVal(1);
			BiomeData.LayerCounts[biomeId] = biome.LayerCount;
		}
	}

	public int GetBiomeId(float pX, float pY)
	{
		_biomeMapNoise.DomainWarp(ref pX, ref pY);

		return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _biomeMapNoise.GetNoise(pX, pY)) * (BiomeData.Count-1));
	}

	public BiomePreset GetBiome(int biomeId)
	{
		return _biomePresets[biomeId];
	}

	public BiomeTerrainHeightGenerator GetGenerator(int biomeId)
	{
		return _biomeGenerators[biomeId];
	}
}
