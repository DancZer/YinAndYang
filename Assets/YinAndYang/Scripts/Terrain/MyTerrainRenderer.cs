using UnityEngine;
using UnityEngine.ProBuilder;

public class MyTerrainRenderer : MonoBehaviour
{
    public GameObject TilePrefab;
    private MyTerrainMeshProvider _meshProvider;

    private void Start()
    {
        _meshProvider = GetComponent<MyTerrainMeshProvider>();
    }

    public void Render(MyTerrainChunk chunk)
    {
        RenderTileRecursive(chunk.Root);
    }

    private void RenderTileRecursive(MyTerrainTile tile)
    {
        if (tile.IsExpanded)
        {
            if(tile.GameObject != null)
            {
                Destroy(tile.GameObject);
            }

            RenderTileRecursive(tile.Child00);
            RenderTileRecursive(tile.Child01);
            RenderTileRecursive(tile.Child11);
            RenderTileRecursive(tile.Child10);
        }
        else
        {
            RenderTile(tile);
        }
    }

    private void RenderTile(MyTerrainTile tile)
    {
        if (tile.IsRendered) return;

        if(tile.GameObject == null)
        {
            tile.GameObject = Instantiate(TilePrefab, transform);
        }
        tile.GameObject.transform.localPosition = GetTilePos(tile.Area.position);
        Debug.Log($"RenderTile {tile.Area.position} {tile.Area.size}");

        var meshFilter = tile.GameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = _meshProvider.CreateMesh(tile.Area.size);
        var meshCollider = tile.GameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
        var meshRenderer = tile.GameObject.GetComponent<MeshRenderer>();
        meshRenderer.material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

        tile.IsRendered = true;
    }

    private Vector3 GetTilePos(Vector2 pos2d)
    {
        return new Vector3(pos2d.x, 0, pos2d.y);
    }

}
