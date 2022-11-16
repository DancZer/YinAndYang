using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

[ExecuteInEditMode]
public class TerrainGenerator : NetworkBehaviour
{
	public static int[] MeshStepSizeByLOD = { 4, 8, 12, 20, 24, 30, 48 };

	public Material BaseMaterial;

	public int Resolution = 240;

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

	[Range(0, 5)]
	public int BiomeBlendStepCount = 1;
	[Range(1, 5)]
	public int BiomeBlendStepSize = 1;

	Dictionary<int, BiomePreset> _biomeMap = new();
	FastNoiseLite _biomeMapNoise;
	float _biomeBlendPercentage;

	float _minHeight;
	float _maxHeight;

#if UNITY_EDITOR
	public int EditorTerrainSize = 240;
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

	public void DrawTerrainInEditor()
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

		var editorDisplayArea = new Rect(
				tileDisplay.transform.position.x - EditorTerrainSize / 2f,
				tileDisplay.transform.position.z - EditorTerrainSize / 2f,
				EditorTerrainSize,
				EditorTerrainSize);

		TerrainTile tile = new TerrainTile(editorDisplayArea);

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
		var area = new Rect(EditorBiomeDisplay.transform.position.x + Resolution / -2f, EditorBiomeDisplay.transform.position.z + Resolution / -2f, Resolution, Resolution);
		var colors = CreateBiomeColorMap(area);
		var tex = CreateTexture(colors);

		var meshFilter = EditorBiomeDisplay.GetComponent<MeshFilter>();
		meshFilter.sharedMesh = CreatePlaneMesh(area);
		var meshRenderer = EditorBiomeDisplay.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
	}

	public Texture CreateTexture(Color[] colors)
	{
		var texture = new Texture2D(Resolution, Resolution);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colors);
		texture.Apply();

		return texture;

	}

	private Color[] CreateBiomeColorMap(Rect area)
	{
		var biomeGenerator = new Dictionary<string, BiomeTerrainGenerator>();

		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var worldPosX = area.position.x + x;
				var worldPosY = area.position.y + y;

				var biome = GetBiome(worldPosX, worldPosY);
				var height = GetHeight(worldPosX, worldPosY, biomeGenerator);

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
					colorMap[y * Resolution + x] = color;
				}
				else
				{
					colorMap[y * Resolution + x] = Color.black;
				}

			}
		}

		return colorMap;
	}

	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		area.center = Vector2.zero;

		var numFaces = 1;
		verts.Add(new Vector3(area.position.x, 0, area.position.y));
		verts.Add(new Vector3(area.position.x, 0, area.position.y + area.size.y));
		verts.Add(new Vector3(area.position.x + area.size.x, 0, area.position.y + area.size.y));
		verts.Add(new Vector3(area.position.x + area.size.x, 0, area.position.y));

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
		Update();
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
				var pos = actual[i].position.To2D();
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


		_biomeMapNoise = new FastNoiseLite(Seed);
		_biomeMapNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
		_biomeMapNoise.SetFrequency(1f / BiomeSize);
		_biomeMapNoise.SetCellularDistanceFunction(BiomeDistanceFunction);
		_biomeMapNoise.SetCellularJitter(BiomeJitter);
		_biomeMapNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);

		_biomeMapNoise.SetDomainWarpType(DomainWarpType);
		_biomeMapNoise.SetDomainWarpAmp(DomainWarpAmp);

		_biomeMapNoise.SetFractalType(FractalType);
		_biomeMapNoise.SetFractalOctaves(FractalOctaves);
		_biomeMapNoise.SetFractalLacunarity(FractalLacunarity);
		_biomeMapNoise.SetFractalGain(FractalGain);

		_biomeMap.Clear();

		int biomeId = 0;
		foreach (var biome in Biomes)
		{
			_biomeMap.Add(biomeId, biome);
			biomeId++;
		}

		_biomeBlendPercentage = 1f / ((2 * BiomeBlendStepCount + 1) * (2 * BiomeBlendStepCount + 1));
	}

	private BiomePreset GetBiome(float x, float y)
	{
		_biomeMapNoise.DomainWarp(ref x, ref y);

		var biomeID = Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _biomeMapNoise.GetNoise(x, y)) * (_biomeMap.Count - 1));

		return _biomeMap[biomeID];
	}

	public void GenerateTerrainData(TerrainTile tile)
	{
		var heightMap = CreateHeightMap(tile.Area);

		tile.SetMap(heightMap, Resolution);
	}

	private float[] CreateHeightMap(Rect area)
    {
		var biomeGenerator = new Dictionary<string, BiomeTerrainGenerator>();

		int NoiseMapSize = Resolution + 1;

		float[] heightMap = new float[NoiseMapSize * NoiseMapSize];

		var noiseMapStep = area.size.x / Resolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				heightMap[y * NoiseMapSize + x] = GetHeight(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep, biomeGenerator);
			}
		}

		return heightMap;
	}

	private float GetHeight(float x, float y, Dictionary<string, BiomeTerrainGenerator> generatorCache)
    {
		var blendValue = 0f;

		for (int bY = -BiomeBlendStepCount; bY <= BiomeBlendStepCount; bY++)
		{
			for (int bX = -BiomeBlendStepCount; bX <= BiomeBlendStepCount; bX++)
			{
				var blendWordX = x + bX * BiomeBlendStepSize;
				var blendWordY = y + bY * BiomeBlendStepSize;

				var biome = GetBiome(blendWordX, blendWordY);

				if (!generatorCache.TryGetValue(biome.BiomeName, out var generator))
				{
					generator = new BiomeTerrainGenerator(biome, Seed);
					generatorCache.Add(biome.BiomeName, generator);
				}

				blendValue += _biomeBlendPercentage * generator.GetTerrainHeight(blendWordX, blendWordY);
			}
		}

		return blendValue;
	}

	public void GenerateMeshData(TerrainTile tile, int lod)
	{
		if (tile.HasMesh(lod)) return;

		lod = lod < 0 ? 0 : lod;

		var meshStepSize = MeshStepSizeByLOD[lod];
		var meshSizeInQuads = tile.MapSize / meshStepSize;
		var vertStep = tile.Area.size.x / meshSizeInQuads;
		var uvStep = 1f / meshSizeInQuads;
		var offset = tile.Area.size / -2f;

		var meshData = new TerrainMeshData(lod, meshSizeInQuads+1);

		; for (int y = 0; y < meshData.SizeInVert; y++)
		{
			for (int x = 0; x < meshData.SizeInVert; x++)
			{
				meshData.Verts.Add(new Vector3(offset.x + x * vertStep, tile.HeightMap[y * meshStepSize * tile.HeightMapSize + x * meshStepSize], offset.y + y * vertStep));
				meshData.UVs.Add(new Vector2(x * uvStep, y * uvStep));

				if (x < meshSizeInQuads && y < meshSizeInQuads)
				{
					var idx = y * meshSizeInQuads + x;
					meshData.AddTriangle(idx + y, idx + y + meshSizeInQuads + 1, idx + y + meshSizeInQuads + 2);
					meshData.AddTriangle(idx + y, idx + y + meshSizeInQuads + 2, idx + y + 1);
				}
			}
		}

		tile.AddOrUpdateMesh(meshData);
	}
}

public class TerrainTile
{
	public readonly string Name;
	public readonly Rect Area;
	public int MapSize;
	public int HeightMapSize;
	public float[] HeightMap;

	public Dictionary<int, TerrainMeshData> MeshDatas = new();
	public float HeightMapStepSize;

	public TerrainTile()
	{

	}

	public TerrainTile(Rect area)
	{
		Name = $"Tile {area}";
		Area = area;

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

	public void SetMap(float[] heightMap, int size)
	{
		MapSize = size;
		HeightMapSize = size + 1;

		HeightMap = heightMap;
		HeightMapStepSize = Area.width / MapSize;
	}

	public float GetHeightAt(Vector2 localPos)
	{
		var heightMapPos = Vector2Int.FloorToInt(localPos / HeightMapStepSize);

		return HeightMap[heightMapPos.y * HeightMapSize + heightMapPos.x];
	}


	public bool FlatHeightMap(Rect flatArea, float flatValue)
	{
		var flatAreaLocal = new Rect(flatArea.position - Area.position, flatArea.size);

		var startPosScaled = Vector2Int.FloorToInt((flatAreaLocal.position) * HeightMapStepSize) - Vector2Int.one;
		var endPosScaled = Vector2Int.FloorToInt((flatAreaLocal.position + flatArea.size) * HeightMapStepSize) + Vector2Int.one * 2;

		var startPos = Vector2Int.Min(Vector2Int.Max(startPosScaled, Vector2Int.zero), new Vector2Int(HeightMapSize, HeightMapSize));
		var endPos = Vector2Int.Min(Vector2Int.Max(endPosScaled, Vector2Int.zero), new Vector2Int(HeightMapSize, HeightMapSize));

		//Debug.Log($"MyTerrainData.FlatHeightMap {Area} {MapSize} {HeightMapStepSize} Flat {flatArea} {flatAreaLocal} {flatValue} SP {startPosScaled} {startPos} EP {endPosScaled} {endPos}");

		var modified = false;
		for (int y = startPos.y; y < endPos.y; y++)
		{
			for (int x = startPos.x; x < endPos.x; x++)
			{
				var idx = y * HeightMapSize + x;
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
	public readonly int SizeInVert;

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
		SizeInVert = size;
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
        for (int i = 0; i < SizeInVert; i++)
        {
			//x axis
			var vertexPos = 0 * SizeInVert + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = SizeInVert * SizeInVert + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];

			//y axis
			vertexPos = i * SizeInVert + 0;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = i * SizeInVert + SizeInVert;
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
		var lastIdx = SizeInVert - 1;

		//LEFT
		var neighbourLOD = lods[0];
		if (neighbourLOD > LOD)
        {
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep/(float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < SizeInVert; i++)
			{
                if (i >= endIdx)
                {
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
                    {
						endIdx = SizeInVert - 1;
                    }
                }

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * SizeInVert + 0];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * SizeInVert + 0];
				var resultVert = adjustedMeshVerts[i * SizeInVert + 0];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * SizeInVert + 0] = resultVert;
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

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[lastIdx * SizeInVert + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[lastIdx * SizeInVert + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[lastIdx * SizeInVert + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[lastIdx * SizeInVert + i] = resultVert;
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

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * SizeInVert + lastIdx];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * SizeInVert + lastIdx];
				var resultVert = adjustedMeshVerts[i * SizeInVert + lastIdx];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * SizeInVert + lastIdx] = resultVert;
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

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[0 * SizeInVert + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[0 * SizeInVert + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[0 * SizeInVert + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[0 * SizeInVert + i] = resultVert;
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

	public float GetTerrainHeight(float x, float y)
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