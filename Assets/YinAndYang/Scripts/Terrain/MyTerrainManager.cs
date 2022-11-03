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
            var gameObject = Instantiate(TilePrefab, transform);
            gameObject.name = node.Name;

            var terrainDisplay = gameObject.GetComponent<MyTerrainDisplay>();
            terrainDisplay.DisplayTerrain(node.Data, Regions, HeightScale, MyTerrainDisplay.DrawMode.Mesh);
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
        var objPos = EditorDisplay.transform.position;
        var objScale = EditorDisplay.transform.localScale;

        var area = new Rect(objPos.x, objPos.z, objScale.x, objScale.z);
        var data = GetComponent<MyTerrainGenerator>().GenerateTerrainData(area);

        if (EditorDrawMode == MyTerrainDisplay.DrawMode.NoiseMap)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.NoiseMap);
        }
        else if (EditorDrawMode == MyTerrainDisplay.DrawMode.ColourMap)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.ColourMap);
        }
        else if (EditorDrawMode == MyTerrainDisplay.DrawMode.Mesh)
        {
            EditorDisplay.DisplayTerrain(data, Regions, HeightScale, MyTerrainDisplay.DrawMode.Mesh);
        }
    }
}