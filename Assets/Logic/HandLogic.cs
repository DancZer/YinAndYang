using UnityEngine;
using System.Collections;
 
public class HandLogic : MonoBehaviour {

    public GameObject handObject;
    public GameObject groundObject;

    public float HandHeight = 0.3f;

    private Vector3 handWordPos = new Vector3(0, 0, 0);

    private Plane groundPlane;

    void Start()
    {
        groundObject.transform.position = Vector3.zero;
        groundObject.transform.rotation = Quaternion.identity;

        groundPlane = new Plane(Vector3.up, 0);
    }

    void Update() {

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float enter=.0f;
        if (groundPlane.Raycast(ray, out enter))
        {
            // some point of the plane was hit - get its coordinates
            var handWordPos = ray.GetPoint(enter);

            handWordPos.y = HandHeight;

            handObject.transform.position = handWordPos;
        }
    }
}