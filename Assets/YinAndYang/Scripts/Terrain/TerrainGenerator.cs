using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

[ExecuteInEditMode]
public class TerrainGenerator : NetworkBehaviour
{
	public const int TileSize = 240;
	public const int TileSizeHalf = TileSize/2;
	public const int TileHeightMapSize = TileSize+1;
	public const int TileHeightMapSizeHalf = TileSizeHalf + 1;
	public static readonly Vector2Int TileSizeVect = new Vector2Int(TileSize, TileSize);
	public static readonly Vector2Int TileSizeHalfVect = new Vector2Int(TileSize/2, TileSize/2);

	public static int[] MeshStepSizeByLOD = { 4, 8, 12, 20, 24, 30, 48 };

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
	public BuildingOnTerrain EditorBuilding;

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
			}
			);
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

	private void UpdateEditorDisplay(TerrainTileDisplay tileDisplay, TerrainTileDisplay[] neigbours = null)
    {
		if (tileDisplay == null) return;

		TerrainTile tile = new TerrainTile(tileDisplay.transform.position.To2DInt() - TileSizeHalfVect, TileSize, BiomeBlendSize);

		GenerateTerrainData(tile);

		if (EditorBuilding != null)
		{
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			var pos = EditorBuilding.transform.position;
			tile.FlatHeightMap(flatArea, pos.y);
		}

		if (tileDisplay.EditorViewDistance == null) return;

		GenerateMeshData(tile, tileDisplay.EditorViewDistance.DisplayLOD);
		GenerateMeshData(tile, tileDisplay.EditorViewDistance.CollisionLOD);

		if(neigbours != null && neigbours.Length == 4)
        {
			tile.MeshDatas[tileDisplay.EditorViewDistance.DisplayLOD].AdjustMeshToNeighboursLOD(neigbours.Select(n => n.EditorViewDistance.DisplayLOD).ToArray());
		}

		tileDisplay.SetTile(tile);
		tileDisplay.Display(tileDisplay.EditorViewDistance);
	}
	private void UpdateEditorBiomeDisplay()
	{
		TerrainTile tile = new TerrainTile(EditorBiomeDisplay.transform.position.To2DInt() - TileSizeHalfVect, TileSize, BiomeBlendSize);

		GenerateTerrainData(tile);

		var colors = CreateBiomeColorMap(tile);
		var tex = CreateTexture(colors);

		var meshFilter = EditorBiomeDisplay.GetComponent<MeshFilter>();
		meshFilter.sharedMesh = CreatePlaneMesh();
		var meshRenderer = EditorBiomeDisplay.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
	}

	public Texture CreateTexture(Color[] colors)
	{
		var texture = new Texture2D(TileSize, TileSize);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colors);
		texture.Apply();

		return texture;

	}

	private Color[] CreateBiomeColorMap(TerrainTile tile)
	{
		Color[] colorMap = new Color[TileSize * TileSize];

		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				var idx = (y + tile.BlendSize) * tile.DataSize + (x + tile.BlendSize);

				var height = tile.HeightMap[idx];
				var biomeId = tile.BiomeMap[idx];
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
					colorMap[y * TileSize + x] = color;
				}
				else
				{
					colorMap[y * TileSize + x] = Color.black;
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
		verts.Add(new Vector3(-TileSizeHalf, 0, -TileSizeHalf));
		verts.Add(new Vector3(-TileSizeHalf, 0,  TileSizeHalf));
		verts.Add(new Vector3( TileSizeHalf, 0,  TileSizeHalf));
		verts.Add(new Vector3( TileSizeHalf, 0, -TileSizeHalf));

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
			var generator = new BiomeTerrainGenerator(biome, Seed);

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

	public void GenerateTerrainData(TerrainTile tile)
	{
		GenerateBiomeMap(tile);
		GenerateHeightMap(tile);
		BlendHeightMap(tile);
	}

	private void GenerateBiomeMap(TerrainTile tile)
	{
		var biomeMap = new int[tile.DataSize * tile.DataSize];

		for (int y = 0; y < tile.DataSize; y++)
		{
			for (int x = 0; x < tile.DataSize; x++)
			{
				biomeMap[y * tile.DataSize + x] = _biomeGenerator.GetBiomeId(tile.Pos.x + x - tile.BlendSize, tile.Pos.y + y - tile.BlendSize);
			}
		}

		tile.BiomeMap = biomeMap;
	}

	private void GenerateHeightMap(TerrainTile tile)
    {
		var heightMap = new float[tile.DataSize * tile.DataSize];

		for (int y = 0; y < tile.DataSize; y++)
		{
			for (int x = 0; x < tile.DataSize; x++)
			{
				var generator = _biomeGenerator.GetGenerator(tile.BiomeMap[y * tile.DataSize + x]);

				heightMap[y * tile.DataSize + x] = generator.GetTerrainHeight(tile.Pos.x + x - tile.BlendSize, tile.Pos.y + y - tile.BlendSize);
			}
		}

		tile.HeightMap = heightMap;
	}

	private void BlendHeightMap(TerrainTile tile)
	{
		var newHeightMap = new float[tile.DataSize * tile.DataSize];

		for (int y = 0; y < tile.DataSize; y++)
		{
			for (int x = 0; x < tile.DataSize; x++)
			{
				var idx = y * tile.DataSize + x;
				var newHeight = 0f;

				if(x < tile.BlendSize || y < tile.BlendSize || x > tile.BlendSize + TileSize || y > tile.BlendSize + TileSize)
                {
					newHeight = tile.HeightMap[idx];
				}
                else
                {
					var currentBiome = tile.BiomeMap[idx];
					var hasBlendAreaMultipleBiomes = true;

					for (int bY = -tile.BlendSize; bY <= tile.BlendSize; bY++)
					{
						for (int bX = -tile.BlendSize; bX <= tile.BlendSize; bX++)
						{
							var blendIdx = (y + bY) * tile.DataSize + (x + bX);

							if (currentBiome != tile.BiomeMap[blendIdx])
                            {
								hasBlendAreaMultipleBiomes = true;
							}
							newHeight += _biomeBlendPercentage * tile.HeightMap[blendIdx];
						}
					}

                    if (!hasBlendAreaMultipleBiomes)
                    {
						newHeight = tile.HeightMap[idx];
					}
				}

				newHeightMap[idx] = newHeight;
			}
		}

		tile.HeightMap = newHeightMap;
	}

	public void GenerateMeshData(TerrainTile tile, int lod)
	{
		if (tile.HasMesh(lod)) return;

		lod = lod < 0 ? 0 : lod;

		var meshStepSize = MeshStepSizeByLOD[lod];
		var meshSize = TileSize / meshStepSize;
		var uvStep = 1f / meshSize;
		var offset = -TileSizeHalfVect;

		var meshData = new TerrainMeshData(lod, meshSize + 1);

		; for (int y = 0; y < meshData.Size; y++)
		{
			for (int x = 0; x < meshData.Size; x++)
			{
				meshData.Verts.Add(new Vector3(
					offset.x + x * meshStepSize, 
					tile.HeightMap[(y*meshStepSize + tile.BlendSize) * tile.DataSize + (x * meshStepSize + tile.BlendSize)], 
					offset.y + y * meshStepSize));

				meshData.UVs.Add(new Vector2(x * uvStep, y * uvStep));

				if (x < meshSize && y < meshSize)
				{
					var idx = y * meshSize + x;
					meshData.AddTriangle(idx + y, idx + y + meshSize + 1, idx + y + meshSize + 2);
					meshData.AddTriangle(idx + y, idx + y + meshSize + 2, idx + y + 1);
				}
			}
		}

		tile.AddOrUpdateMesh(meshData);
	}
}

public class TerrainTile
{
	public readonly string Name;
	public readonly Vector2Int Pos;
	public readonly int DataSize;
	public readonly int BlendSize;
	public float[] HeightMap;
	public int[] BiomeMap;

	public Dictionary<int, TerrainMeshData> MeshDatas = new();

	public TerrainTile()
	{

	}

	public TerrainTile(Vector2Int pos, int size, int blendSize)
	{
		Name = $"Tile {pos}";
		Pos = pos;
		DataSize = size + 1 + 2 * blendSize;
		BlendSize = blendSize;
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

	public void SetHeightMap(float[] heightMap)
	{
		HeightMap = heightMap;
	}

	public float GetHeightAt(Vector2 localPos)
	{
		var heightMapPos = Vector2Int.FloorToInt(localPos / TerrainGenerator.TileHeightMapSize);

		return HeightMap[heightMapPos.y * TerrainGenerator.TileHeightMapSize + heightMapPos.x];
	}


	public bool FlatHeightMap(RectInt flatArea, float flatValue)
	{
		var flatAreaLocal = new RectInt(flatArea.position - Pos, flatArea.size);

		var startPosScaled = (flatAreaLocal.position * TerrainGenerator.TileHeightMapSize) - Vector2Int.one;
		var endPosScaled = Vector2Int.FloorToInt((flatAreaLocal.position + flatArea.size) * TerrainGenerator.TileHeightMapSize) + Vector2Int.one * 2;

		var startPos = Vector2Int.Min(Vector2Int.Max(startPosScaled, Vector2Int.zero), new Vector2Int(TerrainGenerator.TileHeightMapSize, TerrainGenerator.TileHeightMapSize));
		var endPos = Vector2Int.Min(Vector2Int.Max(endPosScaled, Vector2Int.zero), new Vector2Int(TerrainGenerator.TileHeightMapSize, TerrainGenerator.TileHeightMapSize));

		//Debug.Log($"MyTerrainData.FlatHeightMap {Area} {MapSize} {HeightMapStepSize} Flat {flatArea} {flatAreaLocal} {flatValue} SP {startPosScaled} {startPos} EP {endPosScaled} {endPos}");

		var modified = false;
		for (int y = startPos.y; y < endPos.y; y++)
		{
			for (int x = startPos.x; x < endPos.x; x++)
			{
				var idx = y * TerrainGenerator.TileHeightMapSize + x;
				if (HeightMap[idx] != flatValue)
				{
					HeightMap[idx] = flatValue;
					modified = true;
				}
			}
		}

		return modified;
	}
}

public class TerrainMeshData
{
	public readonly int LOD;
	public readonly int Size;

	public readonly List<Vector3> Verts = new();
	public readonly List<Vector2> UVs = new();
	public readonly List<int> Tris = new();

	[System.NonSerialized]
	private Mesh _mesh;

    public TerrainMeshData()
    {
    }

	public TerrainMeshData(int lod, int size)
	{
		LOD = lod < 0 ? 0 : lod;
		Size = size;
	}
	public void AddTriangle(int a, int b, int c)
	{
		Tris.AddRange(new[] { a, b, c });
	}

	public Mesh GetMesh()
    {
		if (_mesh != null) return _mesh;

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

	public void ResetMeshToDefault()
    {
        for (int i = 0; i < Size; i++)
        {
			//x axis
			var vertexPos = 0 * Size + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = Size * Size + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];

			//y axis
			vertexPos = i * Size + 0;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = i * Size + Size;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
		}

		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		//_mesh.Optimize();
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
		var lastIdx = Size - 1;

		//LEFT
		var neighbourLOD = lods[0];
		if (neighbourLOD > LOD)
        {
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep/selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < Size; i++)
			{
                if (i >= endIdx)
                {
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= Size)
					{
						startIdx = Size - 1;
					}

					if (endIdx >= Size)
                    {
						endIdx = Size - 1;
                    }
                }

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * Size + 0];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * Size + 0];
				var resultVert = adjustedMeshVerts[i * Size + 0];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * Size + 0] = resultVert;
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

			for (int i = 0; i < Size; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= Size)
					{
						startIdx = Size - 1;
					}

					if (endIdx >= Size)
					{
						endIdx = Size - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[lastIdx * Size + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[lastIdx * Size + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[lastIdx * Size + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[lastIdx * Size + i] = resultVert;
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

			for (int i = 0; i < Size; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= Size)
					{
						startIdx = Size - 1;
					}

					if (endIdx >= Size)
					{
						endIdx = Size - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * Size + lastIdx];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * Size + lastIdx];
				var resultVert = adjustedMeshVerts[i * Size + lastIdx];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * Size + lastIdx] = resultVert;
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

			for (int i = 0; i < Size; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= Size)
					{
						startIdx = Size - 1;
					}

					if (endIdx >= Size)
					{
						endIdx = Size - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[0 * Size + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[0 * Size + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[0 * Size + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[0 * Size + i] = resultVert;
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
public class BiomeTerrainGenerator
{
	public readonly BiomePreset Preset;

	AnimationCurve _heightCurve;
	FastNoiseLite _noise;

	public BiomeTerrainGenerator(BiomePreset preset, int seed)
	{
		Preset = preset;

		_heightCurve = new AnimationCurve(Preset.HeightCurve.keys);
		_noise = new FastNoiseLite(seed);

		_noise.SetNoiseType(Preset.NoiseType);
		_noise.SetFractalType(Preset.FractalType);
		_noise.SetFrequency(Preset.Frequency);
		_noise.SetFractalOctaves(Preset.Octaves);
		_noise.SetFractalGain(Preset.Gain);
		_noise.SetFractalLacunarity(Preset.Lacunarity);
	}

	public float GetTerrainHeight(int x, int y)
	{
		return GetHeightForNoiseVal(_noise.GetNoise(x, y));
	}

	public float GetHeightForNoiseVal(float val)
    {
		if (Preset.UseHeightCurve)
		{
			val = _heightCurve.Evaluate(val);
		}

		return Preset.BaseHeight + val * Preset.HeightMultiplier;
	}
}

public class BiomeGenerator
{
	readonly FastNoiseLite _biomeMapNoise;

	readonly int _biomeCount;
	readonly BiomePreset[] _biomePresets;
	readonly BiomeTerrainGenerator[] _biomeGenerators;

	Vector2Int _previousMapPos;
	int[,] _previousMap;

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
		_biomeGenerators = new BiomeTerrainGenerator[_biomeCount + 1];
		foreach (var biome in biomes)
		{
			_biomePresets[biomeId] = biome;
			_biomeGenerators[biomeId] = new BiomeTerrainGenerator(biome, seed);
			biomeId++;
		}
	}

	public int GetBiomeId(int x, int y)
    {
		float nX = x;
		float nY = y;

		_biomeMapNoise.DomainWarp(ref nX, ref nY);

		return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _biomeMapNoise.GetNoise(nX, nY)) * (_biomeCount - 1)) + 1;
	}

	public BiomePreset GetBiome(int biomeId)
    {
		return _biomePresets[biomeId];
    }

	public BiomeTerrainGenerator GetGenerator(int biomeId)
    {
		return _biomeGenerators[biomeId];
    }
}
