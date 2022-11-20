using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;
using System.Runtime.CompilerServices;

[ExecuteInEditMode]
public class TerrainGenerator : NetworkBehaviour
{
	public const float TilePhysicalSize = 240;
	public const float TilePhysicalSizeHalf = TilePhysicalSize/2;
	public static readonly Vector2 TilePhysicalSizeVect = new Vector2(TilePhysicalSize, TilePhysicalSize);
	public static readonly Vector2 TilePhysicalSizeHalfVect = new Vector2(TilePhysicalSizeHalf, TilePhysicalSizeHalf);

	public const int TileMeshResolution = 240;
	public const int TileMeshResolution2 = TileMeshResolution * TileMeshResolution;
	public const int TileMeshResolutionHalf = TileMeshResolution/2;

	public const int TileDataResolution = 240;
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



#if UNITY_EDITOR
    #region EDITOR
	public enum BiomeMapDisplays
	{
		Weighted, Single, HeightMap
    }

    public bool EditorAutoUpdateMesh;
	public bool EditorAutoUpdateBiome;
	public TerrainTileDisplay EditorCenterTile;
	public TerrainTileDisplay EditorLeftTile;
	public TerrainTileDisplay EditorRightTile;
	public TerrainTileDisplay EditorForwardTile;
	public TerrainTileDisplay EditorBackwardTile;
	public BuildingFootprint EditorBuilding;

	public GameObject EditorBiomeDisplay;
	public float FlatAreaHeight = 10;

	public BiomeMapDisplays BiomeMapDisplay = BiomeMapDisplays.Weighted;
	public int BiomeMapDisplayBiomeId = 0;

	List<Vector2> _lastPosListTerrain = new();
	List<Vector2> _lastPosListBiome = new();


	private void DrawTerrainInEditor()
	{
		SetupGenerator();

		UpdateEditorDisplay(EditorCenterTile
			, new TerrainTileDisplay[] { 
				EditorLeftTile,
				EditorForwardTile,
				EditorRightTile,
				EditorBackwardTile
			});
		UpdateEditorDisplay(EditorLeftTile);
		UpdateEditorDisplay(EditorRightTile);
		UpdateEditorDisplay(EditorForwardTile);
		UpdateEditorDisplay(EditorBackwardTile);

		UpdateEditorBiomeDisplay();
	}

	private void DrawBiomeInEditor()
    {
		SetupGenerator();

		UpdateEditorBiomeDisplay();
	}

	private void UpdateEditorDisplay(TerrainTileDisplay tileDisplay, TerrainTileDisplay[] neighbours = null)
    {
		if (tileDisplay == null) return;

		var tile = CreateEmptyTile(tileDisplay.transform.position.To2D() - TilePhysicalSizeHalfVect);

		GenerateBiomeMap(tile);
		GenerateHeightMap(tile);
		BlendHeightMap(tile);

		if (EditorBuilding != null)
		{
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			tile.FlatHeightMap(flatArea, EditorBuilding.transform.position.y);
		}

		GenerateAllMeshData(tile);

		if(neighbours != null && neighbours.Length == 4)
        {
			tile.MeshDatas[tileDisplay.EditorRequiredTileStatePreset.DisplayLOD].AdjustMeshToNeighboursLOD(neighbours.Select(n => n.EditorRequiredTileStatePreset.DisplayLOD).ToArray());

			neighbours[0].transform.position = tileDisplay.transform.position + new Vector2(-TilePhysicalSize,0).To3D();
			neighbours[1].transform.position = tileDisplay.transform.position + new Vector2(0, TilePhysicalSize).To3D();
			neighbours[2].transform.position = tileDisplay.transform.position + new Vector2(TilePhysicalSize, 0).To3D();
			neighbours[3].transform.position = tileDisplay.transform.position + new Vector2(0, -TilePhysicalSize).To3D();
		}

		tileDisplay.Display(tile, tileDisplay.EditorRequiredTileStatePreset, _biomeGenerator.BiomeData);

	}
	private void UpdateEditorBiomeDisplay()
	{
		var tile = CreateEmptyTile(EditorBiomeDisplay.transform.position.To2D() - TilePhysicalSizeHalfVect);

		GenerateBiomeMap(tile);
		GenerateHeightMap(tile);
		BlendHeightMap(tile);

		
		Texture2D tex;

		if(BiomeMapDisplay == BiomeMapDisplays.Single)
        {
			tex = tile.CreateBiomeMapText(BiomeMapDisplayBiomeId % _biomeGenerator.BiomeData.Count);
        }
        else
        {
			var colors = CreateBiomeColorMap(tile);
			tex = TerrainTile.CreateTexture(colors);
		}

		var meshFilter = EditorBiomeDisplay.GetComponent<MeshFilter>();
		meshFilter.sharedMesh = CreatePlaneMesh();
		var meshRenderer = EditorBiomeDisplay.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
	}


	private Color[] CreateBiomeColorMap(TerrainTile tile)
	{
		Color[] colorMap = new Color[TileMeshResolution * TileMeshResolution];

		for (int mY = 0; mY < TileMeshResolution; mY++)
		{
			for (int mX = 0; mX < TileMeshResolution; mX++)
			{
				if(BiomeMapDisplay == BiomeMapDisplays.Weighted)
                {
					Color biomesCombinedColor = Color.black;

					for (int biomeId = 0; biomeId < _biomeGenerator.BiomeData.Count; biomeId++)
					{
						var biome = _biomeGenerator.GetBiome(biomeId);
						var biomeWeight = tile.GetBiomeMapColor(mX, mY, biomeId).r;

						biomesCombinedColor += biome.ColorInEditor * biomeWeight;
					}

					colorMap[mY * TileMeshResolution + mX] = biomesCombinedColor;
				}
                else
				{
					var dY = MeshResolutionToDataResolution(mY, TileMeshResolution);
					var dX = MeshResolutionToDataResolution(mX, TileMeshResolution);

					var dIdx = (dY + tile.BlendSize) * tile.DataSize + (dX + tile.BlendSize);
					var height = tile.HeightDataMap[dIdx];

					float minHeight = float.MaxValue;
					float maxHeight = float.MinValue;

					for (int biomeId = 0; biomeId < _biomeGenerator.BiomeData.Count; biomeId++)
					{
						var generator = _biomeGenerator.GetGenerator(biomeId);
						
						if(minHeight > generator.PhysicalMinHeight)
                        {
							minHeight = generator.PhysicalMinHeight;
                        }

						if (maxHeight < generator.PhysicalMaxHeight)
						{
							maxHeight = generator.PhysicalMaxHeight;
						}
					}

					var heightPercentage = Mathf.InverseLerp(minHeight, maxHeight, height);

					colorMap[mY * TileMeshResolution + mX] = Color.Lerp(Color.black, Color.white, heightPercentage);
				}
			}
		}

		return colorMap;
	}

	private Mesh CreatePlaneMesh()
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var numFaces = 1;
		verts.Add(new Vector3(-TilePhysicalSizeHalf, 0, -TilePhysicalSizeHalf));
		verts.Add(new Vector3(-TilePhysicalSizeHalf, 0,  TilePhysicalSizeHalf));
		verts.Add(new Vector3( TilePhysicalSizeHalf, 0,  TilePhysicalSizeHalf));
		verts.Add(new Vector3( TilePhysicalSizeHalf, 0, -TilePhysicalSizeHalf));

		uvs.Add(new Vector2(0, 0));
		uvs.Add(new Vector2(0, 1));
		uvs.Add(new Vector2(1, 1));
		uvs.Add(new Vector2(1, 0));

		var tris = new List<int>();
		int tl = verts.Count - 4 * numFaces;
		for (int i = 0; i < numFaces; i++)
		{
			var p0 = tl + i * 4;
			var p1 = tl + i * 4 + 1;
			var p2 = tl + i * 4 + 2;
			var p3 = tl + i * 4 + 3;

			tris.AddRange(new int[] {
				p0, p1, p2,
				p0, p2, p3 }
			);
		}

		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		mesh.Optimize();

		return mesh;
	}

	public void UpdateEditor()
    {
		DrawTerrainInEditor();
		DrawBiomeInEditor();
	}

	void Update()
	{
		var list = new List<Transform>();

        if (EditorAutoUpdateMesh) { 
			if (EditorBuilding != null)
				list.Add(EditorBuilding.transform);

			if (EditorCenterTile != null)
				list.Add(EditorCenterTile.transform);

			if (EditorLeftTile != null)
				list.Add(EditorLeftTile.transform);
			if (EditorRightTile != null)
				list.Add(EditorRightTile.transform);

			if (EditorForwardTile != null)
				list.Add(EditorForwardTile.transform);
			if (EditorBackwardTile != null)
				list.Add(EditorBackwardTile.transform);

			if (ShouldDraw(_lastPosListTerrain, list))
			{
				DrawTerrainInEditor();
			}
		}

        if (EditorAutoUpdateBiome) { 
			list.Clear();
			if (EditorBiomeDisplay != null)
				list.Add(EditorBiomeDisplay.transform);

			if (ShouldDraw(_lastPosListBiome, list))
			{
				DrawBiomeInEditor();
			}
		}
	}
	private bool ShouldDraw(List<Vector2> expected, List<Transform> actual)
	{
		if (expected.Count == actual.Count)
		{
			var changed = false;

			for (int i = 0; i < actual.Count; i++)
			{
				var pos = actual[i].position.To2DInt();
				if (expected[i] != pos)
				{
					expected[i] = pos;
					changed = true;
				}
			}

			return changed;
		}
		else
		{
			expected.Clear();

			foreach (var t in actual)
			{
				expected.Add(t.position);
			}
			return true;
		}
	}
    #endregion
#endif
    public override void OnStartServer()
    {
        base.OnStartServer();

		SetupGenerator();
    }

	public override void OnStopServer()
	{
		base.OnStopServer();

		_biomeGenerator = null;
	}

	private void SetupGenerator()
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