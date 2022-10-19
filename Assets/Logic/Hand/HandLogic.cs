using UnityEngine;
 
public class HandLogic : MonoBehaviour {

    public GameObject HandObject;
    public GameObject GroundObject;
    public LayerMask HandLayerMask;

    public float HandHeight = 0.3f;

    public float MouseLongPressDeltaTime = .1f;

    private Collider _groundCollider;
    private float _mouseDownTime;
    
    void Start()
    {
        GroundObject.transform.position = Vector3.zero;
        GroundObject.transform.rotation = Quaternion.identity;

        _groundCollider = GroundObject.GetComponent<Collider>();
    }

    void Update() 
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~HandLayerMask))
        {
            HandleHandMove(hit);
            HandleMouseButton(hit);
        }
    }

    private void HandleHandMove(RaycastHit hit){
        if(hit.collider == null) return;

        var handWordPos = hit.point;

        if(_groundCollider == hit.collider){
            handWordPos.y = HandHeight;
        }

        HandObject.transform.position = handWordPos;
    }

    private void HandleMouseButton(RaycastHit hit)
    {
        if (_mouseDownTime == 0 && Input.GetMouseButtonDown(0) ){
            _mouseDownTime = Time.realtimeSinceStartup;
        }else if (Input.GetMouseButtonUp(0)){
            _mouseDownTime = 0;
        }
                   
        if(_mouseDownTime != 0){
            if(_mouseDownTime + MouseLongPressDeltaTime < Time.realtimeSinceStartup){
                Debug.Log("Mouse long press");
                
                if(_groundCollider != hit.collider){
                    var hitGameObject = hit.collider.gameObject;

                    GrabObject grabObject = hitGameObject.GetComponent<GrabObject>();

                    if(grabObject == null){
                        grabObject = hitGameObject.GetComponentInParent<GrabObject>();
                    }

                    Debug.Log($"hitGameObject {hitGameObject} grabObject {grabObject}");
                    if(grabObject != null){
                        grabObject.tag = LayerMask.LayerToName(HandLayerMask.value);
                        grabObject.transform.position = hit.point + grabObject.GrabOffset;
                    }
                }
            }
        }
    }
}