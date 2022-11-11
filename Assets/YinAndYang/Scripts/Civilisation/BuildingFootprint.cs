using UnityEngine;

public class BuildingFootprint : MonoBehaviour
{
    public Rect GetFootprint()
    {
        var rect =  new Rect(Vector2.zero, transform.localScale.To2D());
        rect.center = transform.position.To2D();

        return rect;
    }
}
