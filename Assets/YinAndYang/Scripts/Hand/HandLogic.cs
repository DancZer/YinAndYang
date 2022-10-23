using UnityEngine;
 
public class HandLogic : MonoBehaviour {

    public GameObject HandObject;
    public GameObject GroundObject;
    public LayerMask HandLayerMask;

    public GameObject DirtLumpPrefab;

    public float HandHeight = 0.3f;

    public float MouseLongPressDeltaTime = .1f;

    private Collider _groundCollider;
    private float _mouseDownTimeStart;
    private GrabObject _grabObject;
    private int _grabObjectLayerBak;

    private int _handLayer;
    
    void Start()
    {
        GroundObject.transform.position = Vector3.zero;
        GroundObject.transform.rotation = Quaternion.identity;

        _groundCollider = GroundObject.GetComponent<Collider>();
        _handLayer = HandLayerMask.GetLastLayer();
        Debug.Log($"HandLayerMask.value {HandLayerMask.value}");
        Debug.Log($"_handLayer {_handLayer}");
    }

    void Update() 
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~HandLayerMask))
        {
            HandleHandMove(hit);
            HandleMouseButtons(hit);
        }
    }

    private void HandleHandMove(RaycastHit hit){
        if(hit.collider == null) return;

        var handWordPos = hit.point;

        if(_groundCollider == hit.collider){
            handWordPos.y = HandHeight;
        }

        if(_grabObject != null && _grabObject.IsInHand){
            if(_grabObject.IsGrabAtTop){
                handWordPos.y += _grabObject.transform.localScale.y;
            }
            
            _grabObject.transform.position = handWordPos - Vector3.Scale(_grabObject.GrabOffset, _grabObject.transform.localScale);
        }

        HandObject.transform.position = handWordPos;
    }

    private void HandleMouseButtons(RaycastHit hit)
    {
        if (_mouseDownTimeStart == 0 && Input.GetMouseButtonDown(0) ){
            _mouseDownTimeStart = Time.realtimeSinceStartup;

            if(_groundCollider != hit.collider){
                var hitGameObject = hit.collider.gameObject;

                _grabObject = hitGameObject.GetComponent<GrabObject>();

                if(_grabObject == null){
                    _grabObject = hitGameObject.GetComponentInParent<GrabObject>();
                }
            }

        }else if (Input.GetMouseButtonUp(0)){
            _mouseDownTimeStart = 0;

            if(_grabObject != null){
                _grabObject.IsInHand = false;
                _grabObject.gameObject.SetLayerOnAll(_grabObjectLayerBak);
                _grabObject.transform.position = hit.point;
                _grabObject = null;
            }
        }
                   
        if(_grabObject != null && !_grabObject.IsInHand){
            if(_mouseDownTimeStart + MouseLongPressDeltaTime < Time.realtimeSinceStartup){
                Debug.Log("Mouse long press");
                
                Debug.Log($"_grabObject.gameObject.layer {_grabObject.gameObject.layer}");
                Debug.Log($"_handLayer {_handLayer}");

                _grabObjectLayerBak = _grabObject.gameObject.layer;
                _grabObject.gameObject.SetLayerOnAll(_handLayer);
                _grabObject.IsInHand = true;

                if(_grabObject.CreateLumpWhenGrab){
                    var dirtLump = Instantiate(DirtLumpPrefab, _grabObject.transform.position, Quaternion.identity);
                }
            }
        }
    }
}