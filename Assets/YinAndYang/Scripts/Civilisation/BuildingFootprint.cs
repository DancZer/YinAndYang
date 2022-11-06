using UnityEngine;

public class BuildingFootprint : MonoBehaviour
{
    public RectXZ GetFootprint()
    {
        return new RectXZ(transform.position, transform.localScale);
    }
}
