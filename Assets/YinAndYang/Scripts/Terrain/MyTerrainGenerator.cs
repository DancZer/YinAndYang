using UnityEditor;
using UnityEngine;

public class MyTerrainGenerator : MonoBehaviour
{
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
		var noise = new FastNoise(Seed);

		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);

		noise.SetNoiseType(FastNoise.NoiseType.Perlin);

		float minVal = float.MaxValue;
		float maxVal = float.MinValue;

		int NoiseMapSize = TileResolution +1;

		float[,] noiseMap = new float[NoiseMapSize, NoiseMapSize];

		var noiseMapStep = area.width / TileResolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
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

		return new MyTerrainData(area, noiseMap, TileResolution, GetMagnitude(noiseMap));
    }

	private float GetMagnitude(float[,] noiseMap)
	{
		int NoiseMapSize = TileResolution + 1;

		var maxMagnitude = float.MinValue;
		for (int y = 0; y < NoiseMapSize; y++)
		{
			var lineStart = new Vector3(0, noiseMap[0, y], y);
			var lineEnd = new Vector3(NoiseMapSize-1, noiseMap[NoiseMapSize - 1, y], y);

			for (int x = 0; x < NoiseMapSize; x++)
			{
				var magnitude = HandleUtility.DistancePointLine(new Vector3(x, noiseMap[x, y], y), lineStart, lineEnd);

                if (magnitude > maxMagnitude)
                {
					maxMagnitude = magnitude;
                }
			}
		}

		return maxMagnitude;
	}
}

[System.Serializable]
public struct TerrainType
{
	public string Name;
	public float Height;
	public Color Colour;
}
