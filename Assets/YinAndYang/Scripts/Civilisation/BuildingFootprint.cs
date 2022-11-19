using UnityEngine;

public class BuildingFootprint : MonoBehaviour
{
    public Rect GetFootprint()
    {
        return new Rect(transform.position.To2D() - transform.localScale.To2D() / 2, transform.localScale.To2D());
    }
}
