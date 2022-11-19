using UnityEngine;

public static class MethodExtensions
{
    public static int GetLastLayer(this LayerMask layerMask)
    {
        var layer = layerMask.value;

        var layerIdx = 0;
        while ((layer = layer >> 1) > 0) layerIdx++;

        return layerIdx;
    }

    public static void SetLayerOnAll(this GameObject obj, int layer)
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layer;
        }
    }

    public static Vector2 ClosestPoint(this RectInt rect, Vector2Int pos)
    {
        var point = new Bounds((rect.position + rect.size/2).To3D(), rect.size.To3D()).ClosestPoint(pos.To3D());
        return new Vector2(point.x, point.z);
    }

    public static Vector2 To2D(this Vector3 vect)
    {
        return new Vector2(Mathf.Floor(vect.x), Mathf.Floor(vect.z));
    }

    public static Vector2Int To2DInt(this Vector3 vect)
    {
        return new Vector2Int(Mathf.FloorToInt(vect.x), Mathf.FloorToInt(vect.z));
    }
    public static Vector3 To3D(this Vector2 vect)
    {
        return new Vector3(vect.x, 0, vect.y);
    }
    public static Vector3 To3D(this Vector2Int vect)
    {
        return new Vector3(vect.x, 0, vect.y);
    }
    public static Vector2Int ToInt(this Vector2 vect)
    {
        return new Vector2Int((int)vect.x, (int)vect.y);
    }
}