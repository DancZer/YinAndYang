using System.Collections.Generic;
using UnityEngine;

public class MyTerrainGenerator : MonoBehaviour
{
	public enum TerrainDrawMode { HeightMap, ColourMap, Mesh };

	public TerrainDrawMode DrawMode;
#if UNITY_EDITOR
	public int EditorTerrainSize = 240;
	public bool EditorAutoUpdate;
	[Range(1,6)] public int EditorLOD = 1;
#endif
	public Material BaseMaterial;
	public int Resolution = 240;

	public float HeightMultiplier = 100;
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
	private void Start()
    {
		var editorDisplay = transform.GetChild(0);
		editorDisplay.gameObject.SetActive(false);
	}

#if UNITY_EDITOR
	public void DrawTerrainInEditor()
	{
		MinVal = MinHeight = float.MaxValue;
		MaxVal = MaxHeight = float.MinValue;

		MyTerrainData mapData = GenerateTerrainData(new Rect(new Vector2(transform.position.x-EditorTerrainSize/2f, transform.position.y-EditorTerrainSize/2f), new Vector2(EditorTerrainSize, EditorTerrainSize)));
		MyTerrainMeshData meshData = GenerateMeshData(mapData, EditorLOD);

		var editorDisplay = transform.GetChild(0);
		var meshFilter = editorDisplay.GetComponent<MeshFilter>();
		var meshRenderer = editorDisplay.GetComponent<MeshRenderer>();

		if (DrawMode == TerrainDrawMode.HeightMap || DrawMode == TerrainDrawMode.ColourMap)
		{
			meshFilter.mesh = CreatePlaneMesh(mapData.Area);
		}
		else if (DrawMode == TerrainDrawMode.Mesh)
		{
			meshData.CreateMesh();
			meshFilter.mesh = meshData.Mesh;
		}
		meshData.CreateTexture();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", meshData.Texture);
	}

	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var offset = area.position;

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
#endif

	public MyTerrainData GenerateTerrainData(Rect area)
	{
		var noiseMap = CreateNoiseMap(area);
		Color[] colorMap;

		if (DrawMode == TerrainDrawMode.HeightMap)
		{
			colorMap = CreateHeightMap(noiseMap);
		}
        else
        {
			colorMap = CreateColorMap(noiseMap);
		}

		return new MyTerrainData(area, Resolution, noiseMap, colorMap);
	}

	private float[,] CreateNoiseMap(Rect area)
    {
		var noise = new FastNoiseLite(Seed);
		
		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);
		noise.SetFrequency(Frequency);
		noise.SetFractalType(FractalType);
		noise.SetNoiseType(NoiseType);

		int NoiseMapSize = Resolution + 1;

		float[,] noiseMap = new float[NoiseMapSize, NoiseMapSize];

		var noiseMapStep = area.size.x / Resolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				var val = noise.GetNoise(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep);
				noiseMap[x, y] = val;
#if UNITY_EDITOR
				LogMinMax(ref MinVal, ref MaxVal, val);
#endif
			}
		}

		return noiseMap;
	}

	private Color[] CreateHeightMap(float[,] noiseMap)
	{
		Color[] heightMap = new Color[Resolution * Resolution];
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var height = heightCurve.Evaluate(noiseMap[x, y]);

				heightMap[y * Resolution + x] = Color.Lerp(Color.black, Color.white, height);
			}
		}

		return heightMap;
	}

	private Color[] CreateColorMap(float[,] noiseMap)
	{
		Color[] colorMap = new Color[Resolution * Resolution];
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var height = heightCurve.Evaluate(noiseMap[x, y]) * HeightMultiplier;

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
		var meshResolution = terrainData.Resolution / meshStepSize;
		var vertStep = terrainData.Area.size.x / meshResolution;
		var uvStep = 1f / meshResolution;
		var heightCurve = new AnimationCurve(HeightCurve.keys);
		var offset = Vector2.zero;

		; for (int y = 0; y < meshResolution + 1; y++)
		{
			for (int x = 0; x < meshResolution + 1; x++)
			{
				var height = heightCurve.Evaluate(terrainData.NoiseMap[x * meshStepSize, y * meshStepSize]) * HeightMultiplier;
#if UNITY_EDITOR
				LogMinMax(ref MinHeight, ref MaxHeight, height);
#endif
				mesh.AddVertice(new Vector3(offset.x + x * vertStep, height, offset.y + y * vertStep));
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
	public readonly Rect Area;
	public readonly int Resolution;

	public readonly float[,] NoiseMap;
	public readonly Color[] ColorMap;

    public MyTerrainData(Rect area, int resolution, float[,] heightMap, Color[] colorMap)
	{
		Area = area;
		Resolution = resolution;
        NoiseMap = heightMap;
        ColorMap = colorMap;
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
		if (Mesh != null) return;

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
		if (Texture != null) return;

		Texture = new Texture2D(TerrainData.Resolution, TerrainData.Resolution);

		Texture.filterMode = FilterMode.Point;
		Texture.wrapMode = TextureWrapMode.Clamp;
		Texture.SetPixels(TerrainData.ColorMap);
		Texture.Apply();
	}
}