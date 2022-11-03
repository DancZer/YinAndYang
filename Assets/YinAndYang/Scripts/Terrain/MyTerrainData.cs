using System.Collections.Generic;
using UnityEngine;

public class MyTerrainData
{
	public readonly Rect Area;
    public readonly float Magnitude;
	public readonly int Resolution;

	float[,] _heightMap;

	public MyTerrainData(Rect area, float[,] heightMap, int resolution, float magnitude)
    {
		Area = area;
		Resolution = resolution;
		_heightMap = heightMap;
		Magnitude = magnitude;
	}

    public Mesh GetMesh(float heightScale)
    {
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var numFaces = 0;

		var vertStep = Area.size.x / Resolution;
		var uvStep = 1f / Resolution;

		for (int x = 0; x < Resolution; x++)
		{
			for (int y = 0; y < Resolution; y++)
			{
				verts.Add(new Vector3(x * vertStep, _heightMap[x, y] * heightScale, y * vertStep));
				verts.Add(new Vector3(x * vertStep, _heightMap[x, y+1] * heightScale, (y + 1) * vertStep));
				verts.Add(new Vector3((x + 1) * vertStep, _heightMap[x+1, y+1] * heightScale, (y + 1) * vertStep));
				verts.Add(new Vector3((x + 1) * vertStep, _heightMap[x+1, y] * heightScale, y * vertStep));

				uvs.Add(new Vector2(x * uvStep, y * uvStep));
				uvs.Add(new Vector2(x * uvStep, (y+1) * uvStep));
				uvs.Add(new Vector2((x+1) * uvStep, (y + 1) * uvStep));
				uvs.Add(new Vector2((x + 1) * uvStep, y * uvStep));

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

	public Color[] GetColorMap(TerrainType[] regions)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var currentHeight = _heightMap[x, y];

				foreach (var region in regions)
				{
					if (currentHeight < region.Height)
					{
						colorMap[y * Resolution + x] = region.Colour;
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

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var currentHeight = _heightMap[x, y];

				colorMap[y * Resolution + x] = Color.Lerp(Color.black, Color.white, currentHeight);
			}
		}

		return colorMap;
	}
}
