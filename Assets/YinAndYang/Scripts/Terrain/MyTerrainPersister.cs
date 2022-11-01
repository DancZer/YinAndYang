using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class MyTerrainPersister : MonoBehaviour
{
    private DirectoryInfo _terrainDir;
    void Start()
    {
        _terrainDir = new DirectoryInfo(GetTerrainPath());

        if (!_terrainDir.Exists)
        {
            _terrainDir.Create();
        }

        Debug.Log($"MyTerrainPersister {GetTerrainPath()}");
    }

    public List<Mesh> LoadMeshAll()
    {
        if (!_terrainDir.Exists)
        {
            Debug.LogError($"Could not load terrain. Cache dir does not exits {GetTerrainPath()}");
            return new List<Mesh>();
        }

        var meshList = new List<Mesh>();

        foreach (var fileName in Directory.GetFiles(GetTerrainPath(), "*.terrain"))
        {
            Debug.Log($"Loading terrain {fileName}");

            meshList.Add(LoadMesh(fileName));
            Debug.Log($"Terrain loaded from {fileName}");
        }

        return meshList;
    }

    public void SaveMesh(Mesh mesh)
    {
        if (!_terrainDir.Exists)
        {
            _terrainDir.Create();
        }

        var fileName = $"{GetTerrainPath()}/{mesh.name}.terrain";
        var meshInfo = new SerializableMeshInfo(mesh);

        var bf = new BinaryFormatter();
        var file = File.Create(fileName);
        bf.Serialize(file, meshInfo);
        file.Close();

        Debug.Log($"Terrain saved at {fileName}");
    }

    private Mesh LoadMesh(string fileName)
    {
        var bf = new BinaryFormatter();
        var file = File.Open(fileName, FileMode.Open);
        SerializableMeshInfo meshInfo = (SerializableMeshInfo)bf.Deserialize(file);
        file.Close();

        return meshInfo.GetMesh();
    }

    private string GetTerrainPath()
    {
        return $"{Application.persistentDataPath}/Terrain";
    }

    [System.Serializable]
    class SerializableMeshInfo
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public float[] vertices;
        [SerializeField]
        public int[] triangles;
        [SerializeField]
        public float[] uv;
        [SerializeField]
        public float[] uv2;
        [SerializeField]
        public float[] normals;
        [SerializeField]
        public float[] colors;

        public SerializableMeshInfo(Mesh m) // Constructor: takes a mesh and fills out SerializableMeshInfo data structure which basically mirrors Mesh object's parts.
        {
            name = m.name;
            vertices = new float[m.vertexCount * 3]; // initialize vertices array.
            for (int i = 0; i < m.vertexCount; i++) // Serialization: Vector3's values are stored sequentially.
            {
                vertices[i * 3] = m.vertices[i].x;
                vertices[i * 3 + 1] = m.vertices[i].y;
                vertices[i * 3 + 2] = m.vertices[i].z;
            }
            triangles = new int[m.triangles.Length]; // initialize triangles array
            for (int i = 0; i < m.triangles.Length; i++) // Mesh's triangles is an array that stores the indices, sequentially, of the vertices that form one face
            {
                triangles[i] = m.triangles[i];
            }
            uv = new float[m.uv.Length * 2]; // initialize uvs array
            for (int i = 0; i < m.uv.Length; i++) // uv's Vector2 values are serialized similarly to vertices' Vector3
            {
                uv[i * 2] = m.uv[i].x;
                uv[i * 2 + 1] = m.uv[i].y;
            }
            uv2 = new float[m.uv2.Length*2]; // uv2
            for (int i = 0; i < m.uv2.Length; i++)
            {
                uv[i * 2] = m.uv2[i].x;
                uv[i * 2 + 1] = m.uv2[i].y;
            }
            normals = new float[m.normals.Length*3]; // normals are very important
            for (int i = 0; i < m.normals.Length; i++) // Serialization
            {
                normals[i * 3] = m.normals[i].x;
                normals[i * 3 + 1] = m.normals[i].y;
                normals[i * 3 + 2] = m.normals[i].z;
            }
            colors = new float[m.colors.Length*4];
            for (int i = 0; i < m.colors.Length; i++)
            {
                var color = m.colors[i];

                colors[i] = color.r; 
                colors[i+1] = color.g;
                colors[i+2] = color.b;
                colors[i+3] = color.a;
            }
        }

        // GetMesh gets a Mesh object from currently set data in this SerializableMeshInfo object.
        // Sequential values are deserialized to Mesh original data types like Vector3 for vertices.
        public Mesh GetMesh()
        {
            Mesh m = new Mesh();
            m.name = name;
            List<Vector3> verticesList = new List<Vector3>();
            for (int i = 0; i < vertices.Length / 3; i++)
            {
                verticesList.Add(new Vector3(
                        vertices[i * 3], vertices[i * 3 + 1], vertices[i * 3 + 2]
                    ));
            }
            m.SetVertices(verticesList);
            m.triangles = triangles;
            List<Vector2> uvList = new List<Vector2>();
            for (int i = 0; i < uv.Length / 2; i++)
            {
                uvList.Add(new Vector2(
                        uv[i * 2], uv[i * 2 + 1]
                    ));
            }
            m.SetUVs(0, uvList);
            List<Vector2> uv2List = new List<Vector2>();
            for (int i = 0; i < uv2.Length / 2; i++)
            {
                uv2List.Add(new Vector2(
                        uv2[i * 2], uv2[i * 2 + 1]
                    ));
            }
            m.SetUVs(1, uv2List);
            List<Vector3> normalsList = new List<Vector3>();
            for (int i = 0; i < normals.Length / 3; i++)
            {
                normalsList.Add(new Vector3(
                        normals[i * 3], normals[i * 3 + 1], normals[i * 3 + 2]
                    ));
            }
            m.SetNormals(normalsList);
            m.colors = GetColors();

            return m;
        }

        private Color[] GetColors()
        {
            var result = new Color[colors.Length / 4];

            for (int i = 0; i < colors.Length / 4; i++)
            {
                result[i] = new Color(colors[i], colors[i + 1], colors[i + 2], colors[i + 3]);
            }

            return result;
        }
    }
}
