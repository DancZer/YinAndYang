using UnityEngine;

public class MyTerrainMeshProvider : MonoBehaviour
{
    public Mesh CreateMesh(Vector2 size)
    {
		var mesh = new Mesh
		{
			name = "Procedural Mesh"
		};

		mesh.vertices = new Vector3[] {
			Vector3.zero, new Vector3(size.x, 0, 0), new Vector3(0, 0, size.y), new Vector3(size.x, 0, size.y)
		};

		mesh.normals = new Vector3[] {
			Vector3.up, Vector3.up, Vector3.up, Vector3.up
		};
		/*
		mesh.tangents = new Vector4[] {
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f)
		};*/

		mesh.uv = new Vector2[] {
			Vector2.zero, Vector2.right, Vector2.up, Vector2.one
		};

		mesh.triangles = new int[] {
			0, 2, 1, 1, 2, 3
		};

		return mesh;
	}
}
