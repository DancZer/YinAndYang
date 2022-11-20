using System.Collections.Generic;
using UnityEngine;

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
		VertexDataSize = meshResolution + 1;
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
			float stepDif = leftLODStep / selfLODStep;

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