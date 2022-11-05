using UnityEngine;

public static class MethodExtensions
{
    public static int GetLastLayer(this LayerMask layerMask)
    {
        var layer = layerMask.value;
        
        var layerIdx = 0;
        while((layer = layer>>1)>0) layerIdx++;

        return layerIdx;
    }

    public static void SetLayerOnAll(this GameObject obj, int layer) 
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true)) 
        {
            trans.gameObject.layer = layer;
        }
    }

    public static Vector2 To2DMapPos(this Vector3 pos3d)
    {
        return new Vector2(pos3d.x, pos3d.z); ;
    }

    public static Bounds ToMapBounds(this Rect rect)
    {
        return new Bounds(new Vector3(rect.x + rect.size.x / 2f, 0, rect.y + rect.size.y / 2f), new Vector3(rect.size.x, 0, rect.size.x));
    }
}