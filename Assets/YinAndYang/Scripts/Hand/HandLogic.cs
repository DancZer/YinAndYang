using UnityEngine;
using FishNet.Object;
 
public class HandLogic : NetworkBehaviour 
{
    public LayerMask HandLayerMask;
    public GameObject DirtLumpPrefab;
    public float HandHeight = 0.3f;
    public float ThrowMinVelocity = 0.2f;
    public float MouseLongPressDeltaTime = .1f;
    
    private GameObject _groundObject;
    private Transform _worldObjectTransform;

    private Collider _groundCollider;
    private float _mouseDownTimeStart;
    private GrabObject _grabObject;
    private int _grabObjectLayerBak;
    private Quaternion _grabObjectRotation;
    private Rigidbody _grabObjectRigidbody;
    private int _handLayer;

    private Vector3 _handObjectLastVelocity;
    private Vector3 _handObjectLastPos;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _worldObjectTransform = GameObject.FindGameObjectWithTag("WorldObject").transform;
        _groundObject = GameObject.FindGameObjectWithTag("GroundObject");
        _groundCollider = _groundObject.GetComponent<Collider>();
        _handLayer = HandLayerMask.GetLastLayer();

        if (!base.IsOwner)
            GetComponent<HandLogic>().enabled = false;
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

        if(_grabObject != null && _grabObject.State == GrabState.InHand){
            if(_grabObject.IsGrabAtTop){
                handWordPos.y += _grabObject.transform.localScale.y;
            }

            

            if(!_grabObject.IsGrabAtTop){
                handRot *= Quaternion.Euler(0,0,90);
            }
        }

        _handObjectLastVelocity = (handWordPos - _handObjectLastPos) / Time.deltaTime;
        _handObjectLastPos = handWordPos;

        transform.position = handWordPos;
        transform.rotation = handRot * Quaternion.Euler(0,180,0);
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
                _grabObject.State = GrabState.PutDown;
                _grabObject.gameObject.SetLayerOnAll(_grabObjectLayerBak);
                _grabObject.transform.parent = _worldObjectTransform;
                
                if(_grabObjectRigidbody != null){
                    _grabObjectRigidbody.isKinematic = _grabObject.IsKinematicOnRelease;

                    if(_handObjectLastVelocity.magnitude > ThrowMinVelocity){
                        _grabObjectRigidbody.isKinematic = false;
                        _grabObjectRigidbody.velocity = _handObjectLastVelocity;
                        _grabObject.State = GrabState.Thrown;
                    }

                    if(_grabObjectRigidbody.isKinematic){
                        _grabObject.transform.position = hit.point - _grabObject.DropPosOffset; 
                    }
                }
                
                _grabObjectRigidbody = null;
                _grabObject = null;
            }
        }
                   
        if(_grabObject != null && _grabObject.State != GrabState.InHand){
            if(_mouseDownTimeStart + MouseLongPressDeltaTime < Time.realtimeSinceStartup){
                Debug.Log("Mouse long press");
                
                Debug.Log($"_grabObject.gameObject.layer {_grabObject.gameObject.layer}");
                Debug.Log($"_handLayer {_handLayer}");

                _grabObjectLayerBak = _grabObject.gameObject.layer;
                _grabObject.gameObject.SetLayerOnAll(_handLayer);

                if(_grabObject.IsGrabAtTop){
                    _grabObjectRotation = _grabObject.transform.rotation;
                }else{
                    _grabObjectRotation = Quaternion.identity;
                }
                
                _grabObjectRigidbody = _grabObject.GetComponent<Rigidbody>();

                if(_grabObjectRigidbody != null){
                    _grabObjectRigidbody.isKinematic = true;
                }

                Debug.Log($"_grabObjectRotation {_grabObjectRotation.eulerAngles}");

                if(_grabObject.CreateLumpWhenGrab && _grabObject.State == GrabState.PutDown){
                    SpawnDirtLump(DirtLumpPrefab, _grabObject.transform.position, Quaternion.identity);
                }

                _grabObject.State = GrabState.InHand;
                _grabObject.transform.parent = transform;
                _grabObject.transform.localPosition = Vector3.Scale(_grabObject.GrabOffset, _grabObject.transform.localScale);
            }
        }
    }

    [ServerRpc]
    public void SpawnDirtLump(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject dirtLump = Instantiate(prefab, position, rotation);
        dirtLump.transform.parent = _worldObjectTransform;
        ServerManager.Spawn(dirtLump);
    }
}