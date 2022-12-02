using System.Collections.Generic;
using UnityEngine;

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
		Debug.Log($"Triangle a:{a} b:{b} c:{c}");
		Tris.AddRange(new[] { a, b, c });
	}

	/// <summary>
	/// Neighbour lods should be in order of Left, Forward, Right, Backward
	/// </summary>
	/// <param name="lods"></param>
	public Mesh CreateMesh()
	{
		_mesh = new Mesh();
		_mesh.name = $"LOD {LOD}";
		_mesh.SetVertices(Verts);
		_mesh.SetTriangles(Tris, 0);
		_mesh.SetUVs(0, UVs);
		_mesh.SetNormals(CalculateNormals());
		_mesh.RecalculateTangents();

		_mesh.Optimize();

		return _mesh;
	}


	Vector3[] CalculateNormals()
	{
		var vertexNormals = new Vector3[Verts.Count];
		int triangleCount = Tris.Count / 3;
		for (int i = 0; i < triangleCount; i++)
		{
			int triIdx = i * 3;

			int vIdx1 = Tris[triIdx];
			int vIdx2 = Tris[triIdx + 1];
			int vIdx3 = Tris[triIdx + 2];

			var triangleNormal = TriNormalFromIndices(vIdx1, vIdx2, vIdx3);

			vertexNormals[vIdx1] += triangleNormal;
			vertexNormals[vIdx2] += triangleNormal;
			vertexNormals[vIdx3] += triangleNormal;
		}

		for (int i = 0; i < vertexNormals.Length; i++)
		{
			vertexNormals[i].Normalize();
		}

		return vertexNormals;

	}
	Vector3 TriNormalFromIndices(int i1, int i2, int i3)
	{
		var v1 = Verts[i1];
		var v2 = Verts[i2];
		var v3 = Verts[i3];

		var side21 = v2 - v1;
		var side31 = v3 - v1;
		return Vector3.Cross(side21, side31).normalized;
	}


}