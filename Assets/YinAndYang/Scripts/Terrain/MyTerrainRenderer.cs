using UnityEngine;
using UnityEngine.ProBuilder;

public class MyTerrainRenderer : MonoBehaviour
{
    public GameObject TilePrefab;
    public void Render(MyTerrainTile tile)
    {
        RenderTileRecursive(tile);
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
            tile.GameObject.name = tile.TileName;
        }
        var meshFilter = tile.GameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = tile.Mesh;
        var meshCollider = tile.GameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
        
        //For debug tiles
        /*
        var meshRenderer = tile.GameObject.GetComponent<MeshRenderer>();
        meshRenderer.material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        */

        tile.IsRendered = true;
    }
}
