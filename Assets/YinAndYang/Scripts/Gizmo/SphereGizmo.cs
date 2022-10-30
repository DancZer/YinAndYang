using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereGizmo : MonoBehaviour
{
    public Color SphereColor = Color.yellow;
    public Color LineColor = Color.blue;
    public float Radius = 5;

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = SphereColor;
        Gizmos.DrawSphere(transform.position, Radius);
        Gizmos.color = LineColor;
        Gizmos.DrawLine(transform.position, transform.position+transform.forward*Radius*1.5f);
    }
}
