using UnityEngine;
using System.Linq;

public class BiomeGenerator
{
	readonly FastNoiseLite _biomeMapNoise;
	readonly BiomeTerrainHeightGenerator[] _biomeGenerators;

	int _lastBiomeIdx;

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

		_lastBiomeIdx = biomes.Length - 1;

		_biomeGenerators = biomes.Select(b => new BiomeTerrainHeightGenerator(b, seed)).ToArray();
	}

	public int GetBiomeIdx(float pX, float pY)
	{
		_biomeMapNoise.DomainWarp(ref pX, ref pY);

		return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _biomeMapNoise.GetNoise(pX, pY)) * _lastBiomeIdx);
	}

	public BiomeTerrainHeightGenerator GetGenerator(int biomeIdx)
	{
		return _biomeGenerators[biomeIdx];
	}
}
