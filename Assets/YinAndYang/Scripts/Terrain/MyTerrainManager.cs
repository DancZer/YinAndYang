using UnityEngine;
using FishNet;

public class MyTerrainManager : MonoBehaviour
{
    public GameObject TilePrefab;
    public float ChunkSize = 1000;
    public float NodeSubdividetMagnitude = 30;
    public float MinNodeSize = 10;
    public float HeightScale = 100;

    public LayerMask GroundMask;

    MyTerrainGenerator _terrainGenerator;
    QuadTreeNode<MyTerrainData> _root;

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        GenerateQuadTreeTerrainData();
    }

    private void GenerateQuadTreeTerrainData()
    {
        var halfChunkSize = ChunkSize / 2f;
        _root = new QuadTreeNode<MyTerrainData>(0, new Rect(-halfChunkSize, -halfChunkSize, ChunkSize, ChunkSize));

        GenerateNodeData(_root);
        DisplayTerrainData(_root);
    }

    private void GenerateNodeData(QuadTreeNode<MyTerrainData> node)
    {
        var data = _terrainGenerator.GenerateTerrainData(node.Area);

        Debug.Log($"GenerateNodeData {data.Area} {data.Magnitude * HeightScale}");

        if (data.Magnitude * HeightScale > NodeSubdividetMagnitude && data.Area.width > MinNodeSize)
        {
            Debug.Log($"GenerateNodeData Expand {node.Name}");
            node.Expand();

            foreach (var childNode in node.Children)
            {
                GenerateNodeData(childNode);
            }
        }
        else
        {
            Debug.Log($"GenerateNodeData Store {node.Name} {data.Area} {data.Magnitude * HeightScale}");
            node.Data = data;
        }
    }

    private void DisplayTerrainData(QuadTreeNode<MyTerrainData> node) 
    {
        if (node.IsExpanded)
        {
            foreach (var childNode in node.Children)
            {
                DisplayTerrainData(childNode);
            }
        }
        else
        {
            var terrainData = node.Data;

            var gameObject = Instantiate(TilePrefab, transform);
            gameObject.name = node.Name;
            gameObject.transform.position = new Vector3(terrainData.Area.position.x, 0, terrainData.Area.position.y);
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var collider = gameObject.GetComponent<MeshCollider>();

            collider.sharedMesh = meshFilter.mesh = terrainData.GetMesh(HeightScale);

            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.material.SetTexture("_MainTex", TextureFromColourMap(terrainData.ColorMap, terrainData.Resolution, terrainData.Resolution));
        }
    }
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        var texture = new Texture2D(width, height);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap); //TODO change it to Color32
        texture.Apply();
        return texture;
    }

    void Update()
    {
        if (!InstanceFinder.IsClient || Camera.main == null) return;
    }

    public Vector3 GetGroundPosAtCord(Vector3 pos)
    {
        var ray = new Ray(new Vector3(pos.x, 1000, pos.z), Vector3.down);

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, GroundMask))
        {
            return hit.point;
        }
        else
        {
            Debug.LogError($"Ground pos was not found at {pos}");
        }

        return pos;
    }

}