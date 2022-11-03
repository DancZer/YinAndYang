//#define DEBUG_MESH_OPTIMIZE

using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MyTerrainData
{
	public readonly Bounds Area;
    public readonly float Magnitude;
	public readonly int Resolution;

	float[,] _heightMap;

	public MyTerrainData(Bounds area, float[,] heightMap, int resolution, float magnitude)
    {
		Area = area;
		Resolution = resolution;
		_heightMap = heightMap;
		Magnitude = magnitude;
	}

	public Mesh GetMesh(float heightScale)
	{
		var start = Time.realtimeSinceStartup;
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var numFaces = 0;

		var vertStep = Area.size.x / Resolution;
		var uvStep = 1f / Resolution;

		var offset = Area.OffsetXZ();

		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				verts.Add(new Vector3(offset.x + x * vertStep, _heightMap[x, z] * heightScale, offset.z + z * vertStep));
				verts.Add(new Vector3(offset.x + x * vertStep, _heightMap[x, z + 1] * heightScale, offset.z + (z + 1) * vertStep));
				verts.Add(new Vector3(offset.x + (x + 1) * vertStep, _heightMap[x + 1, z + 1] * heightScale, offset.z + (z + 1) * vertStep));
				verts.Add(new Vector3(offset.x + (x + 1) * vertStep, _heightMap[x + 1, z] * heightScale, offset.z + z * vertStep));

				uvs.Add(new Vector2(x * uvStep, z * uvStep));
				uvs.Add(new Vector2(x * uvStep, (z + 1) * uvStep));
				uvs.Add(new Vector2((x + 1) * uvStep, (z + 1) * uvStep));
				uvs.Add(new Vector2((x + 1) * uvStep, z * uvStep));

				numFaces++;
			}
		}

		var tris = new List<int>();
		int tl = verts.Count - 4 * numFaces;
		for (int i = 0; i < numFaces; i++)
		{
			var p0 = tl + i * 4;
			var p1 = tl + i * 4 + 1;
			var p2 = tl + i * 4 + 2;
			var p3 = tl + i * 4 + 3;

			tris.AddRange(new [] {
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

		Debug.Log($"GetMesh vertex:{verts.Count} time:{Time.realtimeSinceStartup - start}s");

		return mesh;
	}

	public Mesh GetMeshOpt(float heightScale)
	{
		var start = Time.realtimeSinceStartup;
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var faces = new List<(int a, int b, int c, int d)>();
		var tris = new List<int>();

		var vertStep = Area.size.x / Resolution;
		var uvStep = 1f / Resolution;

		var offset = Area.OffsetXZ();

;		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				//V0
				if(x == 0 && z == 0)
                {
					verts.Add(new Vector3(offset.x + x * vertStep, _heightMap[x, z] * heightScale, offset.z + z * vertStep));
					uvs.Add(new Vector2(x * uvStep, z * uvStep));
				}

				//V1
				if (x == 0)
				{
					verts.Add(new Vector3(offset.x + x * vertStep, _heightMap[x, z + 1] * heightScale, offset.z + (z + 1) * vertStep));
					uvs.Add(new Vector2(x * uvStep, (z + 1) * uvStep));
				}

				//V2
				verts.Add(new Vector3(offset.x + (x + 1) * vertStep, _heightMap[x + 1, z + 1] * heightScale, offset.z + (z + 1) * vertStep));
				uvs.Add(new Vector2((x + 1) * uvStep, (z + 1) * uvStep));

				//V3
				if (z == 0)
				{
					verts.Add(new Vector3(offset.x + (x + 1) * vertStep, _heightMap[x + 1, z] * heightScale, offset.z + z * vertStep));
					uvs.Add(new Vector2((x + 1) * uvStep, z * uvStep));
				}

				var fi = z * Resolution + x;
				(int a, int b, int c, int d) face;

				if (fi == 0)
                {
					faces.Add(face=(0, 1, 2, 3));
                }
                else if(z == 0)
                {
					var prevFace = faces[fi - 1];
					faces.Add(face=(prevFace.d, prevFace.c, prevFace.c+2, prevFace.d+2));
				}
				else if (x == 0)
				{
					var bottomFace = faces[fi - Resolution];
					faces.Add(face=(bottomFace.b, verts.Count-2, verts.Count-1, bottomFace.c));
				}
				else
				{
					var bottomFace = faces[fi - Resolution];
					faces.Add(face=(bottomFace.b, verts.Count - 2, verts.Count - 1, bottomFace.c));
				}

				tris.AddRange(new[]{
					face.a, face.b, face.c,
					face.a, face.c, face.d
				});

			}
		}

		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		mesh.Optimize();

		Debug.Log($"GetMeshOpt vertex:{verts.Count} time:{Time.realtimeSinceStartup - start}s");

		return mesh;
	}

	public Color[] GetColorMap(TerrainType[] regions)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var currentHeight = _heightMap[x, z];

				foreach (var region in regions)
				{
					if (currentHeight < region.Height)
					{
						colorMap[z * Resolution + x] = region.Colour;
						break;
					}
				}
			}
		}

		return colorMap;
	}

	public Color[] GetGrayscaleMap()
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var currentHeight = _heightMap[x, z];

				colorMap[z * Resolution + x] = Color.Lerp(Color.black, Color.white, currentHeight);
			}
		}

		return colorMap;
	}
}
