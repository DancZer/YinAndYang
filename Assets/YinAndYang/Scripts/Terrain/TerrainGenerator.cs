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

	float _minHeight;
	float _maxHeight;

#if UNITY_EDITOR
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

		tileDisplay.Display(tile, tileDisplay.EditorRequiredTileStatePreset);
	}
	private void UpdateEditorBiomeDisplay()
	{
		var tile = CreateEmptyTile(EditorBiomeDisplay.transform.position.To2D() - TilePhysicalSizeHalfVect);

		GenerateBiomeMap(tile);
		GenerateHeightMap(tile);
		BlendHeightMap(tile);

		var colors = CreateBiomeColorMap(tile);
		var tex = CreateTexture(colors);

		var meshFilter = EditorBiomeDisplay.GetComponent<MeshFilter>();
		meshFilter.sharedMesh = CreatePlaneMesh();
		var meshRenderer = EditorBiomeDisplay.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
	}

	public Texture CreateTexture(Color[] colors)
	{
		var texture = new Texture2D(TileMeshResolution, TileMeshResolution);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colors);
		texture.Apply();

		return texture;

	}

	private Color[] CreateBiomeColorMap(TerrainTile tile)
	{
		Color[] colorMap = new Color[TileMeshResolution * TileMeshResolution];

		for (int mY = 0; mY < TileMeshResolution; mY++)
		{
			for (int mX = 0; mX < TileMeshResolution; mX++)
			{
				var dY = MeshResolutionToDataResolution(mY, TileMeshResolution);
				var dX = MeshResolutionToDataResolution(mX, TileMeshResolution);

				var dIdx = (dY + tile.BlendSize) * tile.DataSize + (dX + tile.BlendSize);

				var height = tile.HeightMap[dIdx];
				var biomeId = tile.BiomeMap[dIdx];
				var biome = _biomeGenerator.GetBiome(biomeId);

				var percentage = Mathf.InverseLerp(_minHeight, _maxHeight, height);

				Color color;

                if (percentage > 0.5f)
                {
					color = Color.Lerp(biome.ColorInEditor, Color.white, Mathf.InverseLerp(0.5f, 1f, percentage));
				}
                else
                {
					color = Color.Lerp(Color.black, biome.ColorInEditor, Mathf.InverseLerp(0f, 0.5f, percentage));
				}
				

				if (biome != null)
				{
					colorMap[mY * TileMeshResolution + dX] = color;
				}
				else
				{
					colorMap[mY * TileMeshResolution + dX] = Color.black;
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
#endif
	/// <summary>
	/// Initaialize the Terrain Generator
	/// </summary>
	public void SetupGenerator()
	{
		_minHeight = float.MaxValue;
		_maxHeight = float.MinValue;

		foreach (var biome in Biomes)
        {
			var generator = new BiomeTerrainHeightGenerator(biome, Seed);

			var biomeMinHeight = generator.GetHeightForNoiseVal(-1);
			var biomeMaxHeight = generator.GetHeightForNoiseVal(1);

			if (biomeMinHeight < _minHeight)
            {
				_minHeight = biomeMinHeight;
			}

			if (biomeMaxHeight > _maxHeight)
			{
				_maxHeight = biomeMaxHeight;
			}
		}

		MyTerrainRegionPreset[] regions = Biomes[0].Regions;

		var heightColourCount = regions.Length;
		var heightColours = regions.Select(r => r.Color).ToArray();
		var heightColoursStartHeight = regions.Select(r => r.Height).ToArray();

		Debug.Log($"InitMaterial {_minHeight} {_maxHeight} {heightColourCount} {string.Join(",", heightColours)} {string.Join(",",heightColoursStartHeight)}");

		BaseMaterial.SetFloat("minHeight", _minHeight);
		BaseMaterial.SetFloat("maxHeight", _maxHeight);
		BaseMaterial.SetInt("heightColourCount", heightColourCount);
		BaseMaterial.SetColorArray("heightColours", heightColours);
		BaseMaterial.SetFloatArray("heightColoursStartHeight", heightColoursStartHeight);

		_biomeGenerator = new BiomeGenerator(Biomes, Seed, BiomeSize, BiomeDistanceFunction, BiomeJitter, DomainWarpType, DomainWarpAmp, FractalType, FractalOctaves, FractalLacunarity, FractalGain);

		_biomeBlendPercentage = 1f / ((2 * BiomeBlendSize + 1) * (2 * BiomeBlendSize + 1));
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

		tile.BiomeMap = biomeMap;
		tile.SetState(TerrainTileState.BiomeMap);
	}

	public void GenerateHeightMap(TerrainTile tile)
    {
		var heightMap = new float[tile.DataSize * tile.DataSize];

		for (int dY = 0; dY < tile.DataSize; dY++)
		{
			for (int dX = 0; dX < tile.DataSize; dX++)
			{
				var biomeId = tile.BiomeMap[dY * tile.DataSize + dX];
				var generator = _biomeGenerator.GetGenerator(biomeId);

				var pX = DataResolutionToPhysicalSize(dX - tile.BlendSize);
				var pY = DataResolutionToPhysicalSize(dY - tile.BlendSize);

				heightMap[dY * tile.DataSize + dX] = generator.GetTerrainHeight(tile.PhysicalPos.x + pX, tile.PhysicalPos.y + pY);
			}
		}

		tile.HeightMap = heightMap;
		tile.SetState(TerrainTileState.HeightMap);
	}

	public void BlendHeightMap(TerrainTile tile)
	{
		var newHeightMap = new float[tile.DataSize * tile.DataSize];

		for (int dY = 0; dY < tile.DataSize; dY++)
		{
			for (int dX = 0; dX < tile.DataSize; dX++)
			{
				var dIdx = dY * tile.DataSize + dX;
				var newHeight = 0f;

				if(dX < tile.BlendSize || dY < tile.BlendSize || dX > tile.BlendSize + TileDataResolution || dY > tile.BlendSize + TileDataResolution)
                {
					newHeight = tile.HeightMap[dIdx];
				}
                else
                {
					var currentBiome = tile.BiomeMap[dIdx];
					var hasBlendAreaMultipleBiomes = true;

					for (int bY = -tile.BlendSize; bY <= tile.BlendSize; bY++)
					{
						for (int bX = -tile.BlendSize; bX <= tile.BlendSize; bX++)
						{
							var blendIdx = (dY + bY) * tile.DataSize + (dX + bX);

							if (currentBiome != tile.BiomeMap[blendIdx])
                            {
								hasBlendAreaMultipleBiomes = true;
							}
							newHeight += _biomeBlendPercentage * tile.HeightMap[blendIdx];
						}
					}

                    if (!hasBlendAreaMultipleBiomes)
                    {
						newHeight = tile.HeightMap[dIdx];
					}
				}

				newHeightMap[dIdx] = newHeight;
			}
		}

		tile.HeightMap = newHeightMap;
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

				var	height = tile.HeightMap[(dY + tile.BlendSize) * tile.DataSize + (dX + tile.BlendSize)];

				var pX = MeshResolutionToPhysicalSize(mX, meshResolution);
				var pY = MeshResolutionToPhysicalSize(mY, meshResolution);

				meshData.Verts.Add(new Vector3(
					-TilePhysicalSizeHalf + pX,
					height,
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
		var res = new Vector2(dPos.x, dPos.y);
		return (res / TileDataResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float MeshResolutionToPhysicalSize(int mPos, int meshResolution)
	{
		return (mPos / (float)meshResolution) * TilePhysicalSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 MeshResolutionToPhysicalSize(Vector2Int mPos, int meshResolution)
	{
		var res = new Vector2(mPos.x, mPos.y);
		return (res / meshResolution) * TilePhysicalSize;
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
		return Mathf.FloorToInt((dPos / TileDataResolution) * meshResolution);
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

public enum TerrainTileState
{
	Empty = 0,
	BiomeMap = 1,
	HeightMap = 2,
	BlendedHeightMap = 3,
	MeshData = 4,
	AdjustedMeshData = 5 
}

public class TerrainTile : System.IEquatable<TerrainTile>
{
	public readonly string Id;
	public readonly Vector2 PhysicalPos;
	public readonly int DataSize;
	public readonly int BlendSize;
	public readonly Vector2Int BlendSizeVect;

	public float[] HeightMap;
	public int[] BiomeMap;

	public readonly Dictionary<int, TerrainMeshData> MeshDatas = new();

	public TerrainTileState CurrentState;
	public TerrainTileState PreviousState;
	public float LastChangedTime = 0;

	public TerrainTile()
	{
	}

	public TerrainTile(Vector2 pos, int tileDataResolution, int blendSize)
	{
		Id = $"Tile_{pos}";
		PhysicalPos = pos;
		DataSize = tileDataResolution + 2 * blendSize;
		BlendSize = blendSize;
		BlendSizeVect = new Vector2Int(blendSize, blendSize);
	}
	public void ClearMeshes()
	{
		MeshDatas.Clear();
	}

	public bool HasMesh(int lod)
	{
		return MeshDatas.ContainsKey(lod);
	}

	public TerrainMeshData GetMeshData(int lod)
    {
		return MeshDatas[lod];
    }

	public void AddOrUpdateMesh(TerrainMeshData meshData)
    {
		if (MeshDatas.ContainsKey(meshData.LOD))
		{
			MeshDatas[meshData.LOD] = meshData;
		}
		else
		{
			MeshDatas.Add(meshData.LOD, meshData);
		}
	}
	public void SetState(TerrainTileState newState)
    {
		PreviousState = CurrentState;
		CurrentState = newState;
    }

	public float GetHeightAt(Vector2 localPos)
	{
		var heightMapPos = TerrainGenerator.PhysicalSizeToDataResolution(localPos);

		Debug.Log($"GetHeightAt Tile:{this}, localPos:{localPos}, heightMapPos:{heightMapPos}");

		return HeightMap[(heightMapPos.y+BlendSize) * DataSize + (heightMapPos.x + BlendSize)];
	}

	public bool FlatHeightMap(Rect globalFlatArea, float flatValue)
	{
		var flatAreaPhysLocal = new Rect(globalFlatArea.position - PhysicalPos, globalFlatArea.size);

		var dataStartPos = TerrainGenerator.PhysicalSizeToDataResolution(flatAreaPhysLocal.position) - Vector2Int.one + BlendSizeVect;
		var dataEndPos = TerrainGenerator.PhysicalSizeToDataResolution(flatAreaPhysLocal.position + globalFlatArea.size) + Vector2Int.one * 2 + BlendSizeVect;

		if (dataStartPos.x < 0 || dataStartPos.y < 0 || dataEndPos.x >= DataSize || dataEndPos.y >= DataSize) return false;
		
		Debug.Log($"FlatHeightMap tile:{this}, globalFlatArea:{globalFlatArea}, flatValue:{flatValue}, flatAreaPhysLocal:{flatAreaPhysLocal}, dataStartPos:{dataStartPos}, dataEndPos:{dataEndPos}");

		var modified = false;
		for (int dY = dataStartPos.y; dY < dataEndPos.y; dY++)
		{
			for (int dX = dataStartPos.x; dX < dataEndPos.x; dX++)
			{
				var dIdx = dY * DataSize + dX;
				if (HeightMap[dIdx] != flatValue)
				{
					HeightMap[dIdx] = flatValue;
					modified = true;
				}
			}
		}

		return modified;
	}

	/// <summary>
	/// Returns null if no preset should be applied
	/// </summary>
	/// <param name="viewPos"></param>
	/// <param name="requiredStatePresets"></param>
	/// <returns></returns>
	public RequiredTileStatePreset SelectTilePreset(Vector2 viewPos, RequiredTileStatePreset[] requiredStatePresets)
	{
		var tileDistance = Vector2.Distance(viewPos, PhysicalPos);

		return requiredStatePresets.FirstOrDefault(p => p.Distance > tileDistance);
	}

	public static bool operator ==(TerrainTile lhv, TerrainTile rhv)
    {
		return lhv is not null && rhv is not null && lhv.Id == rhv.Id;
    }

	public static bool operator !=(TerrainTile lhv, TerrainTile rhv)
	{
		return !(lhv == rhv);
	}

	public override string ToString()
    {
        return $"Tile Id:{Id} Pos:{PhysicalPos}, CurrentState:{CurrentState}, PreviousState:{PreviousState}, DataSize:{DataSize}, BlendSize:{BlendSize} LastChangedTime:{LastChangedTime}";
    }

    public bool Equals(TerrainTile other)
    {
		return Id == other.Id;
    }
}

public class TerrainMeshData
{
	public readonly int LOD;
	public readonly int VertexDataSize;

	public readonly List<Vector3> Verts = new();
	public readonly List<Vector2> UVs = new();
	public readonly List<int> Tris = new();

	[System.NonSerialized]
	private Mesh _mesh;

    public TerrainMeshData()
    {
    }

	public TerrainMeshData(int lod, int meshResolution)
	{
		LOD = lod < 0 ? 0 : lod;
		VertexDataSize = meshResolution +1;
	}
	public void AddTriangle(int a, int b, int c)
	{
		Tris.AddRange(new[] { a, b, c });
	}

	public Mesh CreateMesh()
    {
		_mesh = new Mesh();
		_mesh.name = $"Mesh LOD {LOD}";
		_mesh.SetVertices(Verts);
		_mesh.SetTriangles(Tris, 0);
		_mesh.SetUVs(0, UVs);
		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		_mesh.Optimize();

		return _mesh;
	}

	/// <summary>
	/// Neighbour lods should be in order of Left, Forward, Right, Backward
	/// </summary>
	/// <param name="lods"></param>
	public void AdjustMeshToNeighboursLOD(int[] lods)
	{
		if (lods == null) throw new UnityException($"Argument null {nameof(lods)}");
		if (lods.Length != 4) throw new UnityException($"Argument lods has not 4 values but {lods.Length}");

		if (lods[0] == LOD && lods[1] == LOD && lods[2] == LOD && lods[3] == LOD) return;

		var selfLODStep = TerrainGenerator.MeshStepSizeByLOD[LOD];
		var adjustedMeshVerts = new List<Vector3>();
		adjustedMeshVerts.AddRange(Verts);
		var lastIdx = VertexDataSize - 1;

		//LEFT
		var neighbourLOD = lods[0];
		if (neighbourLOD > LOD)
        {
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep/selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int mI = 0; mI < VertexDataSize; mI++)
			{
                if (mI >= endIdx)
                {
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= VertexDataSize)
					{
						startIdx = VertexDataSize - 1;
					}

					if (endIdx >= VertexDataSize)
                    {
						endIdx = VertexDataSize - 1;
                    }
                }

				var percent = Mathf.InverseLerp(startIdx, endIdx, mI);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * VertexDataSize + 0];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * VertexDataSize + 0];
				var resultVert = adjustedMeshVerts[mI * VertexDataSize + 0];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[mI * VertexDataSize + 0] = resultVert;
			}
		}

		//FORWARD
		neighbourLOD = lods[1];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int mI = 0; mI < VertexDataSize; mI++)
			{
				if (mI >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= VertexDataSize)
					{
						startIdx = VertexDataSize - 1;
					}

					if (endIdx >= VertexDataSize)
					{
						endIdx = VertexDataSize - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, mI);

				var startVert = Verts[lastIdx * VertexDataSize + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[lastIdx * VertexDataSize + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[lastIdx * VertexDataSize + mI];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[lastIdx * VertexDataSize + mI] = resultVert;
			}
		}

		//RIGHT
		neighbourLOD = lods[2];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int mI = 0; mI < VertexDataSize; mI++)
			{
				if (mI >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= VertexDataSize)
					{
						startIdx = VertexDataSize - 1;
					}

					if (endIdx >= VertexDataSize)
					{
						endIdx = VertexDataSize - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, mI);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * VertexDataSize + lastIdx];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * VertexDataSize + lastIdx];
				var resultVert = adjustedMeshVerts[mI * VertexDataSize + lastIdx];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[mI * VertexDataSize + lastIdx] = resultVert;
			}
		}


		//Backward
		neighbourLOD = lods[3];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int mI = 0; mI < VertexDataSize; mI++)
			{
				if (mI >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= VertexDataSize)
					{
						startIdx = VertexDataSize - 1;
					}

					if (endIdx >= VertexDataSize)
					{
						endIdx = VertexDataSize - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, mI);

				var startVert = Verts[0 * VertexDataSize + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[0 * VertexDataSize + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[0 * VertexDataSize + mI];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[0 * VertexDataSize + mI] = resultVert;
			}
		}

		_mesh = new Mesh();
		_mesh.name = $"Mesh LOD {LOD} Adjusted to {string.Join(",", lods)}";
		_mesh.SetVertices(adjustedMeshVerts);
		_mesh.SetTriangles(Tris, 0);
		_mesh.SetUVs(0, UVs);
		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		_mesh.Optimize();
	}
}
public class BiomeTerrainHeightGenerator
{
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

public class BiomeGenerator
{
	readonly FastNoiseLite _biomeMapNoise;

	readonly int _biomeCount;
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

		int biomeId = 1;
		_biomeCount = biomes.Length;
		_biomePresets = new BiomePreset[_biomeCount + 1];
		_biomeGenerators = new BiomeTerrainHeightGenerator[_biomeCount + 1];
		foreach (var biome in biomes)
		{
			_biomePresets[biomeId] = biome;
			_biomeGenerators[biomeId] = new BiomeTerrainHeightGenerator(biome, seed);
			biomeId++;
		}
	}

	public int GetBiomeId(float pX, float pY)
    {
		_biomeMapNoise.DomainWarp(ref pX, ref pY);

		return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _biomeMapNoise.GetNoise(pX, pY)) * (_biomeCount - 1)) + 1;
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
