using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MyTerrainGenerator : MonoBehaviour
{
	public enum TerrainDrawMode { HeightMap, ColourMap, Mesh };

	public TerrainDrawMode DrawMode;
#if UNITY_EDITOR
	public int EditorTerrainSize = 240;
	public bool EditorAutoUpdate;
	[Range(0,6)] public int EditorLOD = 1;
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

	private void OnValidate()
    {
		lastDrawPos = Vector3.one * float.MaxValue;
    }

    public void DrawTerrainInEditor()
	{
		MinVal = MinHeight = float.MaxValue;
		MaxVal = MaxHeight = float.MinValue;
		
		var editorDisplay = transform.GetChild(0);

		var editorDisplayArea = new RectXZ(
			editorDisplay.position.x - EditorTerrainSize / 2f, 
			editorDisplay.position.z - EditorTerrainSize / 2f, 
			EditorTerrainSize, 
			EditorTerrainSize);

		MyTerrainData mapData = GenerateTerrainData(editorDisplayArea);

		if(EditorBuilding != null) { 
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			var pos = EditorBuilding.transform.position;
			flatArea.center = pos;
			mapData.FlatHeightMap(flatArea, pos.y);
		}

		MyTerrainMeshData meshData = GenerateMeshData(mapData, EditorLOD);

		var meshFilter = editorDisplay.GetComponent<MeshFilter>();
		var meshRenderer = editorDisplay.GetComponent<MeshRenderer>();

		if (DrawMode == TerrainDrawMode.HeightMap || DrawMode == TerrainDrawMode.ColourMap)
		{
			meshFilter.sharedMesh = CreatePlaneMesh(mapData.Area);
		}
		else if (DrawMode == TerrainDrawMode.Mesh)
		{
			meshData.CreateMesh();
			meshFilter.sharedMesh = meshData.Mesh;
		}
		meshData.CreateTexture();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", meshData.Texture);
	}

	private Mesh CreatePlaneMesh(RectXZ area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var offset = area.size / -2f;

		var numFaces = 1;
		verts.Add(new Vector3(offset.x, 0, offset.z));
		verts.Add(new Vector3(offset.x, 0, offset.z + area.size.z));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.z + area.size.z));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.z));

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

    public MyTerrainData GenerateTerrainData(RectXZ area)
	{
		var heightMap = CreateHeightMap(area);
		Color[] colorMap;

		if (DrawMode == TerrainDrawMode.HeightMap)
		{
			colorMap = CreateGrayscaleMap(heightMap);
		}
        else
        {
			colorMap = CreateColorMap(heightMap);
		}

		return new MyTerrainData(area, heightMap, colorMap);
	}

	private float[,] CreateHeightMap(RectXZ area)
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

		float[,] heightMap = new float[NoiseMapSize, NoiseMapSize];

		var noiseMapStep = area.size.x / Resolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				var val = noise.GetNoise(offset.x + x * noiseMapStep, offset.z + y * noiseMapStep);
				var height = (UseHeightCurveEvaluator ? heightCurve.Evaluate(val) : heightMap[x, y]) * HeightMultiplier;
				heightMap[x, y] = height;
#if UNITY_EDITOR
				LogMinMax(ref MinVal, ref MaxVal, val);
				LogMinMax(ref MinHeight, ref MaxHeight, height);
#endif
			}
		}

		return heightMap;
	}

	private Color[] CreateGrayscaleMap(float[,] heightMap)
	{
		Color[] grayscaleMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				grayscaleMap[y * Resolution + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y] / HeightMultiplier);
			}
		}

		return grayscaleMap;
	}

	private Color[] CreateColorMap(float[,] heightMap)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var height = heightMap[x, y];

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

	public MyTerrainMeshData GenerateMeshData(MyTerrainData terrainData, int lod)
	{
		var mesh = new MyTerrainMeshData(terrainData, lod, BaseMaterial);

		var meshStepSize = mesh.LOD + 1;
		var meshResolution = terrainData.HeightMapResolution / meshStepSize;
		var vertStep = terrainData.Area.size.x / meshResolution;
		var uvStep = 1f / meshResolution;
		var offset = terrainData.Area.size / -2f;

		; for (int y = 0; y < meshResolution + 1; y++)
		{
			for (int x = 0; x < meshResolution + 1; x++)
			{
				mesh.AddVertice(new Vector3(offset.x + x * vertStep, terrainData.HeightMap[x * meshStepSize, y * meshStepSize], offset.z + y * vertStep));
				mesh.AddUV(new Vector2(x * uvStep, y * uvStep));

				if (x < meshResolution && y < meshResolution)
				{
					var idx = y * meshResolution + x;
					mesh.AddTriangle(idx + y, idx + y + meshResolution + 1, idx + y + meshResolution + 2);
					mesh.AddTriangle(idx + y, idx + y + meshResolution + 2, idx + y + 1);
				}
			}
		}

		return mesh;
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


public class MyTerrainData
{
	public readonly RectXZ Area;
	public readonly int HeightMapResolution;

	public readonly float[,] HeightMap;
	public readonly Color[] ColorMap;

	private float _heightMapStepSize;

    public MyTerrainData(RectXZ area, float[,] heightMap, Color[] colorMap)
	{
		Area = area;
		HeightMapResolution = heightMap.GetLength(0)-1;
        HeightMap = heightMap;
        ColorMap = colorMap;

		_heightMapStepSize = HeightMapResolution / Area.width;
    }

	public float GetHeightAt(VectorXZ localPos)
    {
		var heightMapPos = VectorXZInt.FloorToInt(localPos * _heightMapStepSize);

		return HeightMap[heightMapPos.x, heightMapPos.z];
	}

	public bool FlatHeightMap(RectXZ flatArea, float flatValue)
    {
		var flatAreaLocal = new RectXZ(flatArea.position - Area.position, flatArea.size);

		var startPosScaled = VectorXZInt.FloorToInt((flatAreaLocal.position) * _heightMapStepSize)-VectorXZInt.one;
		var endPosScaled = VectorXZInt.FloorToInt((flatAreaLocal.position + flatArea.size) * _heightMapStepSize)+ VectorXZInt.one*2;

		var heightMapResolution = HeightMapResolution + 1;
		var startPos = VectorXZInt.Min(VectorXZInt.Max(startPosScaled, VectorXZInt.zero), new VectorXZInt(heightMapResolution, heightMapResolution));
		var endPos = VectorXZInt.Min(VectorXZInt.Max(endPosScaled, VectorXZInt.zero), new VectorXZInt(heightMapResolution, heightMapResolution));

		Debug.Log($"MyTerrainData.FlatHeightMap {Area} {HeightMapResolution} {_heightMapStepSize} Flat {flatArea} {flatAreaLocal} {flatValue} SP {startPosScaled} {startPos} EP {endPosScaled} {endPos}");

		var modified = false;
        for (int z = startPos.z; z < endPos.z; z++)
        {
			for (int x = startPos.x; x < endPos.x; x++)
			{
				HeightMap[x, z] = flatValue;
				modified = true;
			}
		}

		return modified;
	}
}

public class MyTerrainMeshData
{
	public readonly MyTerrainData TerrainData;
	public readonly int LOD;
	public readonly Material Material;
	public readonly string Name;

	List<Vector3> _verts = new List<Vector3>();
	List<Vector2> _uvs = new List<Vector2>();
	List<int> _tris = new List<int>();

	public Mesh Mesh { get; private set; }
	public Texture2D Texture { get; private set; }

	public MyTerrainMeshData(MyTerrainData terrainData, int lod, Material material)
	{
		LOD = lod < 0 ? 0 : lod;
		TerrainData = terrainData;
		Material = material;
		Name = $"Area:{terrainData.Area}_LOD:{lod}";
	}

	public void AddVertice(Vector3 vertice)
    {
		_verts.Add(vertice);
    }

	public void AddUV(Vector2 uv)
	{
		_uvs.Add(uv);
	}
	public void AddTriangle(int a, int b, int c)
	{
		_tris.AddRange(new[] { a, b, c });
	}

	public void CreateMesh()
    {
		Debug.Log($"MyTerrainMeshData.CreateMesh {TerrainData.Area}");
		if (Mesh != null) return;
		Debug.Log($"MyTerrainMeshData.CreateMesh Done {TerrainData.Area}");

		Mesh = new Mesh();
		Mesh.name = "Mesh" + Name;
		Mesh.SetVertices(_verts);
		Mesh.SetTriangles(_tris, 0);
		Mesh.SetUVs(0, _uvs);
		Mesh.RecalculateNormals();
		Mesh.RecalculateTangents();

		Mesh.Optimize();
	}

	public void CreateTexture()
	{
		Debug.Log($"MyTerrainMeshData.CreateTexture {TerrainData.Area}");
		if (Texture != null) return;
		Debug.Log($"MyTerrainMeshData.CreateTexture Done {TerrainData.Area}");

		Texture = new Texture2D(TerrainData.HeightMapResolution, TerrainData.HeightMapResolution);

		Texture.filterMode = FilterMode.Point;
		Texture.wrapMode = TextureWrapMode.Clamp;
		Texture.SetPixels(TerrainData.ColorMap);
		Texture.Apply();
	}
}