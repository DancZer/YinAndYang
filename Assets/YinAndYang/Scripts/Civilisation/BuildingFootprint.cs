using UnityEngine;

public class BuildingFootprint : MonoBehaviour
{
    public Rect GetFootprint()
    {
        return new Rect(transform.position.ToXZ(), transform.localScale.ToXZ());
    }
}
