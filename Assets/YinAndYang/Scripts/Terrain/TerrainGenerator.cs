using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

[ExecuteInEditMode]
public class TerrainGenerator : NetworkBehaviour
{
	public enum TerrainDrawMode { HeightMap, ColourMap, Mesh };

	static int[] MeshStepSizeByLOD = { 4, 8, 12, 20, 24, 30, 48 };

	public TerrainDrawMode DrawMode;
#if UNITY_EDITOR
	public int EditorTerrainSize = 240;
	public bool EditorAutoUpdate;
	public ViewDistancePreset EditorViewDistance;
	public BuildingOnTerrain EditorBuilding;
	public float FlatAreaHeight = 10;
#endif
	public Material BaseMaterial;
	public int Resolution = 240;

	public float HeightMultiplier = 100;
	public bool UseHeightCurveEvaluator = true;
	public AnimationCurve HeightCurve;
	public MyTerrainRegionPreset[] Regions;

	public FastNoiseLite.NoiseType NoiseType;
	public FastNoiseLite.FractalType FractalType;
	public int Seed = 1234;
	public float Frequency = 0.5f;
	public int Octaves = 3;
	public float Gain = 2;
	public float Lacunarity = 2;

#if UNITY_EDITOR
	[ReadOnly] public float MinVal;
	[ReadOnly] public float MaxVal;

	[ReadOnly] public float MinHeight;
	[ReadOnly] public float MaxHeight;
#endif

#if UNITY_EDITOR

	Vector3 lastDrawPos;
	Vector3 buildingLastDrawPos;

    public void DrawTerrainInEditor()
	{
		InitMaterial();

		MinVal = MinHeight = float.MaxValue;
		MaxVal = MaxHeight = float.MinValue;
		
		var editorDisplay = transform.GetChild(0);

		var editorDisplayArea = new Rect(
			editorDisplay.position.x - EditorTerrainSize / 2f, 
			editorDisplay.position.z - EditorTerrainSize / 2f, 
			EditorTerrainSize, 
			EditorTerrainSize);

		TerrainTile tile = new TerrainTile(editorDisplayArea);
			
		GenerateTerrainData(tile);

		if(EditorBuilding != null) { 
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			var pos = EditorBuilding.transform.position;
			tile.FlatHeightMap(flatArea, pos.y);
		}

		if (EditorViewDistance == null) return;

		GenerateMeshData(tile, EditorViewDistance.DisplayLOD);
		GenerateMeshData(tile, EditorViewDistance.CollisionLOD);

		if (DrawMode == TerrainDrawMode.HeightMap || DrawMode == TerrainDrawMode.ColourMap)
		{
			var meshFilter = editorDisplay.GetComponent<MeshFilter>();
			var meshRenderer = editorDisplay.GetComponent<MeshRenderer>();

			meshFilter.sharedMesh = CreatePlaneMesh(tile.Area);
			meshRenderer.sharedMaterial = BaseMaterial;
		}
		else if (DrawMode == TerrainDrawMode.Mesh)
		{
			var tileDisplay = editorDisplay.GetComponent<TerrainTileDisplay>();
			tileDisplay.SetTile(tile);
			tileDisplay.Display(EditorViewDistance);
		}
	}

	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var offset = Vector2.zero;

		var numFaces = 1;
		verts.Add(new Vector3(offset.x, 0, offset.y));
		verts.Add(new Vector3(offset.x, 0, offset.y + area.size.y));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.y + area.size.y));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.y));

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

    void Update()
    {
		var editorDisplay = transform.GetChild(0);

		if (!editorDisplay.gameObject.activeSelf) return;

		var buildingPos = Vector3.zero;
		if(EditorBuilding != null)
        {
			buildingPos = EditorBuilding.transform.position;
		}

		if (lastDrawPos == editorDisplay.position && buildingLastDrawPos == buildingPos) return;
		lastDrawPos = editorDisplay.position;
		buildingLastDrawPos = buildingPos;

		DrawTerrainInEditor();
	}

#endif

	public void InitMaterial()
	{
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		var minHeight = heightCurve.Evaluate(-1) * HeightMultiplier;
		var maxHeight = heightCurve.Evaluate(1) * HeightMultiplier;
		var heightColourCount = Regions.Length;
		var heightColours = Regions.Select(r => r.Color).ToArray();
		var heightColoursStartHeight = Regions.Select(r => r.Height).ToArray();

		Debug.Log($"InitMaterial {minHeight} {maxHeight} {heightColourCount} {string.Join(",", heightColours)} {string.Join(",",heightColoursStartHeight)}");

		BaseMaterial.SetFloat("minHeight", minHeight);
		BaseMaterial.SetFloat("maxHeight", maxHeight);
		BaseMaterial.SetInt("heightColourCount", heightColourCount);
		BaseMaterial.SetColorArray("heightColours", heightColours);
		BaseMaterial.SetFloatArray("heightColoursStartHeight", heightColoursStartHeight);
	}

	public void GenerateTerrainData(TerrainTile tile)
	{
		var heightMap = CreateHeightMap(tile.Area);
		Color[] colorMap;

		if (DrawMode == TerrainDrawMode.HeightMap)
		{
			colorMap = CreateGrayscaleMap(heightMap);
		}
        else
        {
			colorMap = CreateColorMap(heightMap);
		}

		tile.SetMap(heightMap, colorMap, Resolution);
	}

	private float[] CreateHeightMap(Rect area)
    {
		var heightCurve = new AnimationCurve(HeightCurve.keys);
		var noise = new FastNoiseLite(Seed);
		
		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);
		noise.SetFrequency(Frequency);
		noise.SetFractalType(FractalType);
		noise.SetNoiseType(NoiseType);

		int NoiseMapSize = Resolution + 1;

		float[] heightMap = new float[NoiseMapSize * NoiseMapSize];

		var noiseMapStep = area.size.x / Resolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				var idx = y * NoiseMapSize + x;

				var val = noise.GetNoise(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep);
				var height = (UseHeightCurveEvaluator ? heightCurve.Evaluate(val) : heightMap[idx]) * HeightMultiplier;
				heightMap[idx] = height;
#if UNITY_EDITOR
				LogMinMax(ref MinVal, ref MaxVal, val);
				LogMinMax(ref MinHeight, ref MaxHeight, height);
#endif
			}
		}

		return heightMap;
	}

	private Color[] CreateGrayscaleMap(float[] heightMap)
	{
		Color[] grayscaleMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				grayscaleMap[y * Resolution + x] = Color.Lerp(Color.black, Color.white, heightMap[y * (Resolution+1) + x] / HeightMultiplier);
			}
		}

		return grayscaleMap;
	}

	private Color[] CreateColorMap(float[] heightMap)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var height = heightMap[y * (Resolution+1) + x];

				foreach (var region in Regions)
				{
					if (height < region.Height)
					{
						colorMap[y * Resolution + x] = region.Color;
						break;
					}
				}
			}
		}

		return colorMap;
	}

	public void GenerateMeshData(TerrainTile tile, int lod)
	{
		if (tile.HasMesh(lod)) return;

		var meshData = new TerrainMeshData(lod);

		var meshStepSize = MeshStepSizeByLOD[meshData.LOD];
		var meshResolution = tile.MapSize / meshStepSize;
		var vertStep = tile.Area.size.x / meshResolution;
		var uvStep = 1f / meshResolution;
#if UNITY_EDITOR
		var offset = tile.Area.size / -2f;
#else
		var offset = tile.Area.size / -2f;
#endif

		; for (int y = 0; y < meshResolution + 1; y++)
		{
			for (int x = 0; x < meshResolution + 1; x++)
			{
				meshData.Verts.Add(new Vector3(offset.x + x * vertStep, tile.HeightMap[y * meshStepSize * tile.HeightMapSize + x * meshStepSize], offset.y + y * vertStep));
				meshData.UVs.Add(new Vector2(x * uvStep, y * uvStep));

				if (x < meshResolution && y < meshResolution)
				{
					var idx = y * meshResolution + x;
					meshData.AddTriangle(idx + y, idx + y + meshResolution + 1, idx + y + meshResolution + 2);
					meshData.AddTriangle(idx + y, idx + y + meshResolution + 2, idx + y + 1);
				}
			}
		}

		tile.AddOrUpdateMesh(meshData);
	}
#if UNITY_EDITOR
	private void LogMinMax(ref float minVal, ref float maxVal, float val)
    {
		if (val < minVal)
		{
			minVal = val;
		}

		if (val > maxVal)
		{
			maxVal = val;
		}
	}
#endif
}

public class TerrainTile
{
	public readonly string Name;
	public readonly Rect Area;

	public int HeightMapSize;
	public float[] HeightMap;

	public int MapSize;
	public Color[] ColorMap;

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

	public void SetMap(float[] heightMap, Color[] colorMap, int size)
	{
		HeightMap = heightMap;
		ColorMap = colorMap;

		HeightMapSize = size + 1;
		MapSize = size;

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

	public readonly List<Vector3> Verts = new();
	public readonly List<Vector2> UVs = new();
	public readonly List<int> Tris = new();

	[System.NonSerialized]
	private Mesh _mesh;

    public TerrainMeshData()
    {
    }

	public TerrainMeshData(int lod)
	{
		LOD = lod < 0 ? 0 : lod;
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

}