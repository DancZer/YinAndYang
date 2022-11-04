using UnityEngine;
using FishNet;

public class MyTerrainManager : MonoBehaviour
{
    public GameObject TilePrefab;
    [Range(0, 6)] public int ChunkMaxLevel = 8;
    public float MergeNodeWithMagnitude = 30;
    public float MinNodeSize = 24;

    public LayerMask GroundMask;
    [ReadOnly] public float ChunkSize;

    MyTerrainGenerator _terrainGenerator;
    QuadTreeNode<MyTerrainData> _root;

    void Start()
    {
        _terrainGenerator = GetComponent<MyTerrainGenerator>();
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