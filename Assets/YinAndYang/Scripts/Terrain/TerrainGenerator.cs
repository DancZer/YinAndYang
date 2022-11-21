using UnityEngine;
using System.Runtime.CompilerServices;

public class TerrainGenerator : MonoBehaviour
{
	public const float TilePhysicalSize = 240;
	public const float TilePhysicalSizeHalf = TilePhysicalSize/2;
	public static readonly Vector2 TilePhysicalSizeVect = new Vector2(TilePhysicalSize, TilePhysicalSize);
	public static readonly Vector2 TilePhysicalSizeHalfVect = new Vector2(TilePhysicalSizeHalf, TilePhysicalSizeHalf);

	public const int TileMeshResolution = 120;
	public const int TileMeshResolution2 = TileMeshResolution * TileMeshResolution;
	public const int TileMeshResolutionHalf = TileMeshResolution/2;

	public const int TileDataResolution = 120;
	public const int TileDataResolutionHalf = TileDataResolution/2;
	public static readonly Vector2Int TileDataResolutionVect = new Vector2Int(TileDataResolution, TileDataResolution);
	public static readonly Vector2Int TileDataResolutionHalfVect = new Vector2Int(TileDataResolutionHalf, TileDataResolutionHalf);

	public const int TileHeightMapDataResolution = TileDataResolution+1;
	public const int TileHeightMapDataResolutionHalf = TileDataResolutionHalf + 1;
	public static readonly Vector2Int TileHeightMapDataResolutionVect = new Vector2Int(TileHeightMapDataResolution, TileHeightMapDataResolution);
	public static readonly Vector2Int TileHeightMapDataResolutionHalfVect = new Vector2Int(TileHeightMapDataResolutionHalf, TileHeightMapDataResolutionHalf);

	public static int[] MeshStepSizeByLOD = { 4, 8, 12, 20, 24, 30, 48 }; //last idx:6

	public Material BaseMaterial;

	public int Seed = 1234;
	
	public BiomePreset[] Biomes;

	[Range(1, 10000)]
	public int BiomeSize = 5;
	
	public FastNoiseLite.CellularDistanceFunction BiomeDistanceFunction = FastNoiseLite.CellularDistanceFunction.Manhattan;
	[Range(0f, 1f)]
	public float BiomeJitter = 1;

	public FastNoiseLite.DomainWarpType DomainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
	[Range(0f, 1000f)]
	public float DomainWarpAmp = 100;

	public FastNoiseLite.FractalType FractalType = FastNoiseLite.FractalType.DomainWarpIndependent;
	[Range(1, 30)]
	public int FractalOctaves = 5;
	[Range(0f, 10f)]
	public int FractalLacunarity = 3;
	[Range(0f, 1f)]
	public int FractalGain = 1;

	[Range(1, 5)]
	public int BiomeBlendSize = 1;

	BiomeGenerator _biomeGenerator;
	float _biomeBlendPercentage;

    void Start()
    {
		SetupGenerator();
    }
	public void SetupGenerator()
	{
		_biomeGenerator = new BiomeGenerator(Biomes, Seed, BiomeSize, BiomeDistanceFunction, BiomeJitter, DomainWarpType, DomainWarpAmp, FractalType, FractalOctaves, FractalLacunarity, FractalGain);
		_biomeBlendPercentage = 1f / ((2 * BiomeBlendSize + 1) * (2 * BiomeBlendSize + 1));
	}

	public BiomeData GetBiomeData()
    {
		return _biomeGenerator.BiomeData;
	}

	public TerrainTile CreateEmptyTile(Vector2 pos)
    {
		return new TerrainTile(pos, TileHeightMapDataResolution, BiomeBlendSize);
	}

	public void GenerateBiomeMap(TerrainTile tile)
	{
		var biomeMap = new int[tile.DataSize * tile.DataSize];

		for (int dY = 0; dY < tile.DataSize; dY++)
		{
			for (int dX = 0; dX < tile.DataSize; dX++)
			{
				var pX = DataResolutionToPhysicalSize(dX - tile.BlendSize);
				var pY = DataResolutionToPhysicalSize(dY - tile.BlendSize);

				biomeMap[dY * tile.DataSize + dX] = _biomeGenerator.GetBiomeId(tile.PhysicalPos.x + pX, tile.PhysicalPos.y + pY);
			}
		}

		tile.BiomeDataMap = biomeMap;
		tile.SetState(TerrainTileState.BiomeMap);
	}

	public void GenerateHeightMap(TerrainTile tile)
    {
		var heightMap = new float[tile.DataSize * tile.DataSize];

		for (int dY = 0; dY < tile.DataSize; dY++)
		{
			for (int dX = 0; dX < tile.DataSize; dX++)
			{
				var biomeId = tile.BiomeDataMap[dY * tile.DataSize + dX];
				var generator = _biomeGenerator.GetGenerator(biomeId);

				var pX = tile.PhysicalPos.x + DataResolutionToPhysicalSize(dX - tile.BlendSize);
				var pY = tile.PhysicalPos.y + DataResolutionToPhysicalSize(dY - tile.BlendSize);

				heightMap[dY * tile.DataSize + dX] = generator.GetTerrainHeight(pX, pY);
			}
		}

		tile.HeightDataMap = heightMap;
		tile.SetState(TerrainTileState.HeightMap);
	}

	public void BlendHeightMap(TerrainTile tile)
	{
		var newHeightMap = new float[tile.DataSize * tile.DataSize];
		var biomeAlphaMap = new Color[TileMeshResolution2 * _biomeGenerator.BiomeData.Count];

		var blendBiomeCache = new float[_biomeGenerator.BiomeData.Count];

		for (int dY = 0; dY < tile.DataSize; dY++)
		{
			for (int dX = 0; dX < tile.DataSize; dX++)
			{
				var dIdx = dY * tile.DataSize + dX;
				var newHeight = 0f;

				if(dX < tile.BlendSize || dY < tile.BlendSize || dX >= tile.DataSize - tile.BlendSize || dY >= tile.DataSize - tile.BlendSize)
                {
					newHeight = tile.HeightDataMap[dIdx];
				}
                else
                {
                    for (int biomeId = 0; biomeId < _biomeGenerator.BiomeData.Count; biomeId++)
                    {
						blendBiomeCache[biomeId] = 0;
					}

					for (int bY = -tile.BlendSize; bY <= tile.BlendSize; bY++)
					{
						for (int bX = -tile.BlendSize; bX <= tile.BlendSize; bX++)
						{
							var blendIdx = (dY + bY) * tile.DataSize + (dX + bX);
							var biomeId = tile.BiomeDataMap[blendIdx];

							blendBiomeCache[biomeId] += _biomeBlendPercentage;

							newHeight += _biomeBlendPercentage * tile.HeightDataMap[blendIdx];
						}
					}

					var mY = DataResolutionToMeshResolution(dY - tile.BlendSize, TileMeshResolution);
					var mX = DataResolutionToMeshResolution(dX - tile.BlendSize, TileMeshResolution);

					if(mX < TileMeshResolution && mY < TileMeshResolution) { 

						for (int biomeId = 0; biomeId < _biomeGenerator.BiomeData.Count; biomeId++)
						{
							var color = Color.Lerp(Color.black, Color.white, blendBiomeCache[biomeId]);
							biomeAlphaMap[biomeId * TileMeshResolution2 + mY * TileMeshResolution + mX] = color;
						}
					}
				}

				newHeightMap[dIdx] = newHeight;
			}
		}

		tile.HeightDataMap = newHeightMap;
		tile.BiomeWeightColorMap = biomeAlphaMap;
		tile.SetState(TerrainTileState.BlendedHeightMap);
	}

	public void GenerateAllMeshData(TerrainTile tile)
    {
        for (int lod = 0; lod < MeshStepSizeByLOD.Length; lod++)
        {
			GenerateMeshData(tile, lod);
		}
		tile.SetState(TerrainTileState.MeshData);
    }

	private void GenerateMeshData(TerrainTile tile, int lod)
	{
		lod = lod < 0 ? 0 : lod;

		var meshStepSize = MeshStepSizeByLOD[lod];
		var meshResolution = TileMeshResolution / meshStepSize;
		var uvStep = 1f / meshResolution;

		var meshData = new TerrainMeshData(lod, meshResolution);

		; for (int mY = 0; mY < meshData.VertexDataSize; mY++)
		{
			for (int mX = 0; mX < meshData.VertexDataSize; mX++)
			{ 
				var dX = MeshResolutionToDataResolution(mX, meshResolution);
				var dY = MeshResolutionToDataResolution(mY, meshResolution);

				var dIdx = (dY + tile.BlendSize) * tile.DataSize + (dX + tile.BlendSize);

				var pX = MeshResolutionToPhysicalSize(mX, meshResolution);
				var pY = MeshResolutionToPhysicalSize(mY, meshResolution);

				var pHeight = tile.HeightDataMap[dIdx];

				meshData.Verts.Add(new Vector3(
					-TilePhysicalSizeHalf + pX,
					pHeight,
					- TilePhysicalSizeHalf + pY));

				meshData.UVs.Add(new Vector2(mX * uvStep, mY * uvStep));

				//create triangles till the last line of vertices
				if (mX < meshResolution && mY < meshResolution)
				{
					var idx = mY * meshData.VertexDataSize + mX;
					meshData.AddTriangle(idx, idx + meshData.VertexDataSize, idx + meshData.VertexDataSize + 1);
					meshData.AddTriangle(idx, idx + meshData.VertexDataSize + 1, idx + 1);
				}
			}
		}

		tile.AddOrUpdateMesh(meshData);
	}

	public void AdjustTerrainTileMeshData(TerrainTile tile)
    {

    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int PhysicalSizeToDataResolution(float pPos)
	{
		return Mathf.FloorToInt((pPos / TilePhysicalSize) * TileDataResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int PhysicalSizeToDataResolution(Vector2 pPos)
	{
		return Vector2Int.FloorToInt((pPos / TilePhysicalSize) * TileDataResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DataResolutionToPhysicalSize(int dPos)
	{
		return (dPos / (float)TileDataResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 DataResolutionToPhysicalSize(Vector2Int dPos)
	{
		return (new Vector2(dPos.x, dPos.y) / TileDataResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float MeshResolutionToPhysicalSize(int mPos, int meshResolution)
	{
		return (mPos / (float)meshResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 MeshResolutionToPhysicalSize(Vector2Int mPos, int meshResolution)
	{
		return (new Vector2(mPos.x, mPos.y) / meshResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int MeshResolutionToDataResolution(int mPos, int meshResolution)
	{
		return Mathf.FloorToInt((mPos / (float)meshResolution) * TileDataResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int MeshResolutionToDataResolution(Vector2Int mPos, int meshResolution)
	{
		return Vector2Int.FloorToInt((new Vector2(mPos.x, mPos.y) / meshResolution) * TileDataResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int DataResolutionToMeshResolution(int dPos, int meshResolution)
	{
		return Mathf.FloorToInt((dPos / (float)TileDataResolution) * meshResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int DataResolutionToMeshResolution(Vector2Int dPos, int meshResolution)
	{
		return Vector2Int.FloorToInt((new Vector2(dPos.x, dPos.y) / TileDataResolution) * meshResolution);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float AnyPosToTilePos(float pPos)
	{
		return Mathf.Floor(pPos / TilePhysicalSize) * TilePhysicalSize - TilePhysicalSizeHalf;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 AnyPosToTilePos(Vector2 pPos)
	{
		return new Vector2(AnyPosToTilePos(pPos.x), AnyPosToTilePos(pPos.y));
	}
}

public class BiomeData
{
	public int Count;

	public float[] MinHeights;
	public float[] MaxHeights;
	public float[] LayerCounts;

    public override string ToString()
    {
        return $"Count:{Count}, MinHeights:{MinHeights?.Length} ({string.Join(",", MinHeights)}), MinHeights:{MaxHeights?.Length} ({string.Join(",", MaxHeights)}), MinHeights:{LayerCounts?.Length} ({string.Join(",", LayerCounts)})";
    }
}