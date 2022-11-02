using UnityEngine;

public class MyTerrainGenerator : MonoBehaviour
{
	public TerrainType[] Regions;
	public int TileResolution = 33;
	public int Seed = 1234;
	public int Octaves = 3;
	public float Gain = 2;
	public float Lacunarity = 2;

	public float NoiseClampMin = -0.5f;
	public float NoiseClampMax = 0.5f;

	public float GlobalMinVal;
	public float GlobalMaxVal;

    private void Start()
	{
		GlobalMinVal = float.MaxValue;
		GlobalMaxVal = float.MinValue;
	}

    public MyTerrainData GenerateTerrainData(Rect area)
    {
		var offset = area.position;

		var noise = new FastNoise(Seed);

		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);

		noise.SetNoiseType(FastNoise.NoiseType.Perlin);

		float minVal = float.MaxValue;
		float maxVal = float.MinValue;

		int NoiseMapResolution = TileResolution +1;
		int ColorResolution = TileResolution;

		float[,] noiseMap = new float[NoiseMapResolution, NoiseMapResolution];
		Color[] colorMap = new Color[ColorResolution * ColorResolution];

		var noiseMapStep = area.width / NoiseMapResolution;

		for (int y = 0; y < NoiseMapResolution; y++)
		{
			for (int x = 0; x < NoiseMapResolution; x++)
			{
				var currentHeight = noise.GetPerlin(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep);
                
				if (currentHeight < minVal)
				{
					minVal = currentHeight;
				}

				if (currentHeight > maxVal)
				{
					maxVal = currentHeight;
				}

				noiseMap[x, y] = currentHeight;

				if (x >= ColorResolution || y >= ColorResolution) continue;

				foreach (var region in Regions)
				{
					if (currentHeight < region.Height)
					{
						colorMap[y * ColorResolution + x] = region.Colour;
						break;
					}
				}
			}
		}

		if (minVal < GlobalMinVal)
		{
			GlobalMinVal = minVal;
		}

		if (maxVal > GlobalMaxVal)
		{
			GlobalMaxVal = maxVal;
		}

		return new MyTerrainData(area, noiseMap, TileResolution, maxVal -minVal, colorMap);
    }
}

[System.Serializable]
public struct TerrainType
{
	public string Name;
	public float Height;
	public Color Colour;
}
