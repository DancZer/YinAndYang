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

	BiomeLayerData _biomeLayerData;
	BiomeGenerator _biomeGenerator;
	float _biomeBlendPercentage;

    void Start()
    {
		SetupGenerator();
    }
	public void SetupGenerator()
	{
		_biomeGenerator = new BiomeGenerator(Biomes, Seed, BiomeSize, BiomeDistanceFunction, BiomeJitter, DomainWarpType, DomainWarpAmp, FractalType, FractalOctaves, FractalLacunarity, FractalGain);
		var blendWidth = 2 * BiomeBlendSize + 1;
		_biomeBlendPercentage = 1f / (blendWidth * blendWidth);


		_biomeLayerData = new BiomeLayerData(Biomes.Length);

		for (int biomeIdx = 0; biomeIdx < Biomes.Length; biomeIdx++)
		{
			var biome = Biomes[biomeIdx];
			var generator = _biomeGenerator.GetGenerator(biomeIdx);

			_biomeLayerData.BiomeTexIds[biomeIdx] = biome.TexId;
			_biomeLayerData.MinHeights[biomeIdx] = generator.GetHeightForNoiseVal(-1);
			_biomeLayerData.MaxHeights[biomeIdx] = generator.GetHeightForNoiseVal(1);
			_biomeLayerData.LayerCounts[biomeIdx] = biome.Layers.Length;

            for (int layerIdx = 0; layerIdx < biome.Layers.Length; layerIdx++)
            {
				var flat2DIdx = biomeIdx * BiomeLayerData.MaxLayerCount + layerIdx;
				var layer = biome.Layers[layerIdx];

				_biomeLayerData.BaseBlendFlat2D[flat2DIdx] = layer.BaseBlend;
				_biomeLayerData.BaseStartHeightFlat2D[flat2DIdx] = layer.BaseStartHeight;
				_biomeLayerData.BaseColorFlat2D[flat2DIdx] = layer.BaseColor;
				_biomeLayerData.BaseColorStrengthFlat2D[flat2DIdx] = layer.BaseColorStrength;

			}
		}
	}

	public BiomeLayerData GetBiomeLayerData()
    {
		return _biomeLayerData;
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

				biomeMap[dY * tile.DataSize + dX] = _biomeGenerator.GetBiomeIdx(tile.PhysicalPos.x + pX, tile.PhysicalPos.y + pY);
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
				var biomeIdx = tile.BiomeDataMap[dY * tile.DataSize + dX];
				var generator = _biomeGenerator.GetGenerator(biomeIdx);

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
		var biomeAlphaMap = new float[TileMeshResolution2 * _biomeLayerData.BiomeCount];

		var blendBiomeCache = new float[_biomeLayerData.BiomeCount];

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
                    for (int biomeIdx = 0; biomeIdx < _biomeLayerData.BiomeCount; biomeIdx++)
                    {
						blendBiomeCache[biomeIdx] = 0;
					}

					for (int bY = -tile.BlendSize; bY <= tile.BlendSize; bY++)
					{
						for (int bX = -tile.BlendSize; bX <= tile.BlendSize; bX++)
						{
							var blendIdx = (dY + bY) * tile.DataSize + (dX + bX);
							var biomeIdx = tile.BiomeDataMap[blendIdx];

							blendBiomeCache[biomeIdx] += _biomeBlendPercentage;

							newHeight += _biomeBlendPercentage * tile.HeightDataMap[blendIdx];
						}
					}

					var mY = DataResolutionToMeshResolution(dY - tile.BlendSize, TileMeshResolution);
					var mX = DataResolutionToMeshResolution(dX - tile.BlendSize, TileMeshResolution);

					if(mX < TileMeshResolution && mY < TileMeshResolution) {

						for (int biomeIdx = 0; biomeIdx < _biomeLayerData.BiomeCount; biomeIdx++)
						{
							var weight = blendBiomeCache[biomeIdx];

							biomeAlphaMap[biomeIdx * TileMeshResolution2 + mY * TileMeshResolution + mX] = weight;
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

public class BiomeLayerData
{
	public const int MaxLayerCount = 16;

	public readonly int BiomeCount;

	public readonly int[] BiomeTexIds;
	public readonly float[] MinHeights;
	public readonly float[] MaxHeights;
	public readonly int[] LayerCounts;

	public readonly float[] BaseBlendFlat2D;
	public readonly float[] BaseStartHeightFlat2D;

	public readonly Color[] BaseColorFlat2D;
	public readonly float[] BaseColorStrengthFlat2D;

	public BiomeLayerData()
    {

    }

	public BiomeLayerData(int biomeCount)
	{
		BiomeCount = biomeCount;

		BiomeTexIds = new int[MaxLayerCount];
		MinHeights = new float[MaxLayerCount];
		MaxHeights = new float[MaxLayerCount];
		LayerCounts = new int[MaxLayerCount];

		BaseBlendFlat2D = new float[MaxLayerCount * MaxLayerCount];
		BaseStartHeightFlat2D = new float[MaxLayerCount * MaxLayerCount];
		BaseColorFlat2D = new Color[MaxLayerCount * MaxLayerCount];
		BaseColorStrengthFlat2D = new float[MaxLayerCount * MaxLayerCount];
	}
}