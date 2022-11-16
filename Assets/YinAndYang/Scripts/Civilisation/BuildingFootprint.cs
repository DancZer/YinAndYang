using UnityEngine;

public class BuildingFootprint : MonoBehaviour
{
    public RectInt GetFootprint()
    {
        return new RectInt(transform.position.To2DInt() - transform.localScale.To2DInt() / 2, transform.localScale.To2DInt());
    }
}
