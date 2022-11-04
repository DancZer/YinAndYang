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
	
	public Mesh GetMesh(float heightScale, int lod)
	{
		if (lod <= 0) lod = 1;

		var start = Time.realtimeSinceStartup;
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var tris = new List<int>();

		var meshResolution = Resolution / lod;
		var vertStep = Area.size.x / meshResolution;
		var uvStep = 1f / meshResolution;

		var offset = Area.OffsetXZ();

		; for (int z = 0; z < meshResolution+1; z++)
		{
			for (int x = 0; x < meshResolution+1; x++)
			{
				verts.Add(new Vector3(offset.x + x * vertStep, _heightMap[x, z] * heightScale, offset.z + z * vertStep));
				uvs.Add(new Vector2(x * uvStep, z * uvStep));

				if(x < meshResolution && z < meshResolution)
                {
					var idx = z * meshResolution + x;
					tris.AddRange(new[]{
						idx+z, idx+z+meshResolution+1, idx+z+meshResolution+2,
						idx+z, idx+z+meshResolution+2, idx+z+1
					});

					//Debug.Log($"GetMesh3 tris:{string.Join(",",tris)}");
				}
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
