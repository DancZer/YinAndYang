using UnityEngine;
 
public class HandLogic : MonoBehaviour {

    private const float ThrowMinVelocity = 0.2f;

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
    private Quaternion _grabObjectRotation;
    private Rigidbody _grabObjectRigidbody;
    private Vector3 _grabObjectLastVelocity;
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
            HandleHandMove(hit, ray);
            HandleMouseButtons(hit);
        }
    }

    private void HandleHandMove(RaycastHit hit, Ray ray){
        if(hit.collider == null) return;

        var handWordPos = hit.point;

        if(_groundCollider == hit.collider){
            handWordPos.y = HandHeight;
        }

        var handRot = Quaternion.LookRotation(new Vector3(ray.direction.x, 0, ray.direction.z), Vector3.up);

        if(_grabObject != null && _grabObject.IsInHand){
            if(_grabObject.IsGrabAtTop){
                handWordPos.y += _grabObject.transform.localScale.y;
            }
            var lastPos = _grabObject.transform.position;
            _grabObject.transform.position = handWordPos - Vector3.Scale(_grabObject.GrabOffset, _grabObject.transform.localScale);
            _grabObject.transform.rotation = _grabObjectRotation * handRot;

            _grabObjectLastVelocity = (_grabObject.transform.position - lastPos) / Time.deltaTime;

            if(!_grabObject.IsGrabAtTop){
                handRot *= Quaternion.Euler(0,0,90);
            }
        }

        HandObject.transform.position = handWordPos;
        HandObject.transform.rotation = handRot * Quaternion.Euler(0,180,0);
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
                
                
                if(_grabObjectRigidbody != null){
                    _grabObjectRigidbody.isKinematic = _grabObject.IsKinematicOnRelease;

                    if(_grabObjectLastVelocity.magnitude > ThrowMinVelocity){
                        _grabObjectRigidbody.isKinematic = false;
                        _grabObjectRigidbody.velocity = _grabObjectLastVelocity;
                    }

                    if(_grabObjectRigidbody.isKinematic){
                        _grabObject.transform.position = hit.point - _grabObject.DropPosOffset; 
                    }
                }
                
                _grabObjectRigidbody = null;
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

                _grabObjectRotation = _grabObject.transform.rotation;
                _grabObjectRigidbody = _grabObject.GetComponent<Rigidbody>();

                if(_grabObjectRigidbody != null){
                    _grabObjectRigidbody.isKinematic = true;
                }

                Debug.Log($"_grabObjectRotation {_grabObjectRotation.eulerAngles}");

                if(_grabObject.CreateLumpWhenGrab){
                    var dirtLump = Instantiate(DirtLumpPrefab, _grabObject.transform.position, Quaternion.identity);
                }
            }
        }
    }
}