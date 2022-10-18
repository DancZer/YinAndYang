using UnityEngine;
 
public class HandMovementLogic : MonoBehaviour {

    public GameObject HandObject;
    public GameObject GroundObject;
    public LayerMask HandLayerMask;

    public float HandHeight = 0.3f;

    private Vector3 handWordPos = new Vector3(0, 0, 0);

    private Collider groundCollider;
    
    void Start()
    {
        GroundObject.transform.position = Vector3.zero;
        GroundObject.transform.rotation = Quaternion.identity;

        groundCollider = GroundObject.GetComponent<Collider>();
    }

    void Update() 
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~HandLayerMask))
        {
            if(hit.collider != null)
            {
                var handWordPos = hit.point;

                if(groundCollider == hit.collider){
                    handWordPos.y = HandHeight;
                }

                HandObject.transform.position = handWordPos;
            }
        }
    }
}