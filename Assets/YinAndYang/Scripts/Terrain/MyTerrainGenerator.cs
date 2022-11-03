using UnityEditor;
using UnityEngine;

public class MyTerrainGenerator : MonoBehaviour
{
	public FastNoise.NoiseType NoiseType;
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

    public MyTerrainData GenerateTerrainData(Bounds area)
    {
		Debug.Log($"GenerateTerrainData {area}");
		var noise = new FastNoise(Seed);

		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);

		noise.SetNoiseType(NoiseType);

		float minVal = float.MaxValue;
		float maxVal = float.MinValue;

		int NoiseMapSize = TileResolution +1;

		float[,] noiseMap = new float[NoiseMapSize, NoiseMapSize];

		var noiseMapStep = area.size.x / TileResolution;
		var offset = area.OffsetXZ();

		for (int z = 0; z < NoiseMapSize; z++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				var currentHeight = noise.GetNoise(offset.x + x * noiseMapStep, offset.z + z * noiseMapStep);
                
				if (currentHeight < minVal)
				{
					minVal = currentHeight;
				}

				if (currentHeight > maxVal)
				{
					maxVal = currentHeight;
				}

				noiseMap[x, z] = currentHeight;
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
		for (int z = 0; z < NoiseMapSize; z++)
		{
			var lineStart = new Vector3(0, noiseMap[0, z], z);
			var lineEnd = new Vector3(NoiseMapSize-1, noiseMap[NoiseMapSize - 1, z], z);

			for (int x = 0; x < NoiseMapSize; x++)
			{
				var magnitude = HandleUtility.DistancePointLine(new Vector3(x, noiseMap[x, z], z), lineStart, lineEnd);

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
