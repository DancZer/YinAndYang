using UnityEngine;
using FishNet;

public class MyTerrainManager : MonoBehaviour
{
    public GameObject TilePrefab;
    public float ChunkSize = 1000;
    public float NodeSubdividetMagnitude = 30;
    public float MinNodeSize = 10;
    public float HeightScale = 100;
    public TerrainType[] Regions;

    public LayerMask GroundMask;

    public MyTerrainDisplay.DrawMode EditorDrawMode;
    public bool EditorAutoUpdate;
    public MyTerrainDisplay EditorDisplay;
    public float EditorDisplaySize;
    [Range(1, 12)]public int LOD = 1;

    MyTerrainGenerator _terrainGenerator;
    QuadTreeNode<MyTerrainData> _root;

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();

        GenerateQuadTreeTerrainData();
    }

    private void GenerateQuadTreeTerrainData()
    {
        _root = new QuadTreeNode<MyTerrainData>(0, new Bounds(new Vector3(0,0,0), new Vector3(ChunkSize, 0, ChunkSize)));

        GenerateNodeData(_root);
        DisplayTerrainData(_root);
    }

    private void GenerateNodeData(QuadTreeNode<MyTerrainData> node)
    {
        var data = _terrainGenerator.GenerateTerrainData(node.Area);

        Debug.Log($"GenerateNodeData {data.Area} {data.Magnitude * HeightScale}");

        if (data.Magnitude * HeightScale > NodeSubdividetMagnitude && data.Area.size.x > MinNodeSize)
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
            var gameObject = Instantiate(TilePrefab, transform);
            gameObject.name = node.Name;

            var terrainDisplay = gameObject.GetComponent<MyTerrainDisplay>();
            terrainDisplay.DisplayTerrain(node.Data, Regions, HeightScale, MyTerrainDisplay.DrawMode.Mesh, LOD);
        }
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

    public void DrawTerrainInEditor()
    {
        var area = new Bounds(EditorDisplay.transform.position, new Vector3(EditorDisplaySize, 0, EditorDisplaySize));
        var data = GetComponent<MyTerrainGenerator>().GenerateTerrainData(area);

        if (EditorDrawMode == MyTerrainDisplay.DrawMode.NoiseMap)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.NoiseMap, LOD);
        }
        else if (EditorDrawMode == MyTerrainDisplay.DrawMode.ColourMap)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.ColourMap, LOD);
        }
        else if (EditorDrawMode == MyTerrainDisplay.DrawMode.Mesh)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.Mesh, LOD);
        }
    }
}