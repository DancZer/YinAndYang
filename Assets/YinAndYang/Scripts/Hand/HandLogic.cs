using UnityEngine;
using FishNet.Object;
 
public class HandLogic : NetworkBehaviour
{
    private enum HandActionState
    {
        None, GrabStarted, GrabTriggered, Grabbed, DropTriggered
    }

    public LayerMask HandLayerMask;
    public GameObject DirtLumpPrefab;
    public float HandHeight = 0.3f;
    public float ThrowMinVelocity = 0.2f;
    public float MouseLongPressDeltaTime = .1f;

    [HideInInspector] public GrabObject GrabObject = null;
    [HideInInspector] public int GrabObjectLayerBak = -1;

    private GameObject _groundObject;
    private Transform _worldObjectTransform;

    private Collider _groundCollider;
    private float _mouseDownTimeStart;
    private int _handLayer;

    private Vector3 LastVelocity;
    private Vector3 _handObjectLastPos;

    private GrabObject _grabObjectLongPress;
    private HandActionState _handActuinState = HandActionState.None;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _worldObjectTransform = GameObject.FindGameObjectWithTag("WorldObject").transform;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _worldObjectTransform = GameObject.FindGameObjectWithTag("WorldObject").transform;
        _groundObject = GameObject.FindGameObjectWithTag("GroundObject");
        _groundCollider = _groundObject.GetComponent<Collider>();
        _handLayer = HandLayerMask.GetLastLayer();

        if (!IsOwner)
        {
            GetComponent<HandLogic>().enabled = false;
        }
    }

    
    void Update() 
    {
        if (IsOwner)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~HandLayerMask))
            {
                HandleHandMove(hit, ray);
                HandleMouseButtons(hit);
            }
        }

        LastVelocity = (transform.position - _handObjectLastPos) / Time.deltaTime;
        _handObjectLastPos = transform.position;
    }

    private void HandleHandMove(RaycastHit hit, Ray ray){
        if(hit.collider == null) return;

        var handWordPos = hit.point;

        if(_groundCollider == hit.collider){
            handWordPos.y = HandHeight;
        }

        //var handRot = Quaternion.LookRotation(new Vector3(ray.direction.x, 0, ray.direction.z), Vector3.up);
        var handRot = Quaternion.identity;

        if (GrabObject != null && GrabObject.State == GrabState.InHand){
            if(GrabObject.IsGrabAtTop){
                handWordPos.y += GrabObject.transform.localScale.y;
            }

            if(!GrabObject.IsGrabAtTop){
                handRot *= Quaternion.Euler(0,0,90);
            }
        }

        transform.SetPositionAndRotation(handWordPos, handRot * Quaternion.Euler(0,180,0));
    }

    private void HandleMouseButtons(RaycastHit hit)
    {
        Debug.Log($"HandleMouseButtons {_handActuinState} {Input.GetMouseButtonDown(0)} {Input.GetMouseButtonUp(0)} {_grabObjectLongPress} {GrabObject}");
        if (_handActuinState == HandActionState.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_groundCollider != hit.collider)
                {
                    _mouseDownTimeStart = Time.realtimeSinceStartup;

                    var hitGameObject = hit.collider.gameObject;

                    _grabObjectLongPress = hitGameObject.GetComponent<GrabObject>();

                    if (_grabObjectLongPress == null)
                    {
                        _grabObjectLongPress = hitGameObject.GetComponentInParent<GrabObject>();
                    }

                    if (_grabObjectLongPress != null)
                    {
                        _handActuinState = HandActionState.GrabStarted;
                    }
                }
            }
        }
        else if (_handActuinState == HandActionState.GrabStarted)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _handActuinState = HandActionState.None;
            }
            else
            {
                if (_mouseDownTimeStart + MouseLongPressDeltaTime < Time.realtimeSinceStartup)
                {
                    _handActuinState = HandActionState.GrabTriggered;
                    GrabObjectServer(_grabObjectLongPress, this);
                }
            }
        }
        else if (_handActuinState == HandActionState.GrabTriggered)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _grabObjectLongPress = null;
            }
            else
            {
                if (GrabObject != null && _grabObjectLongPress == null)
                {
                    _handActuinState = HandActionState.Grabbed;
                }
            }
        }
        else if (_handActuinState == HandActionState.Grabbed)
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (GrabObject != null)
                {
                    _handActuinState = HandActionState.DropTriggered;
                    DropObjectServer(this);
                }
            }
        }
        else if (_handActuinState == HandActionState.DropTriggered)
        {
            if (GrabObject == null)
            {
                _handActuinState = HandActionState.None;
            }
        }
    }

    [ServerRpc]
    public void GrabObjectServer(GrabObject grabObject, HandLogic hand)
    {
        Debug.Log($"GrabObjectServer {grabObject} {hand} {IsOwner}");
        GrabObjectObserver(grabObject, hand);
    }

    [ObserversRpc]
    public void GrabObjectObserver(GrabObject grabObject, HandLogic hand)
    {
        Debug.Log($"GrabObjectObserver {grabObject} {hand} {IsOwner} {grabObject.gameObject.layer}");

        hand.GrabObject = grabObject;
        hand.GrabObjectLayerBak = grabObject.gameObject.layer;

        var rigidbody = grabObject.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
        }

        grabObject.gameObject.SetLayerOnAll(_handLayer);

        Quaternion grabObjectRotation;
        if (grabObject.IsGrabAtTop)
        {
            grabObjectRotation = GrabObject.transform.rotation;
        }
        else
        {
            grabObjectRotation = Quaternion.Euler(0, 0, -90);
        }       

        if (IsOwner) 
        {
            if (grabObject.CreateLumpWhenGrab && grabObject.State == GrabState.PutDown)
            {
                SpawnDirtLump(DirtLumpPrefab, grabObject.transform.position, Quaternion.identity);
            }
        }

        grabObject.State = GrabState.InHand;
        grabObject.transform.rotation = grabObjectRotation;
        grabObject.transform.parent = hand.transform;
        grabObject.transform.localPosition = Vector3.Scale(grabObject.GrabOffset, grabObject.transform.localScale);
        
    }

    [ServerRpc]
    public void DropObjectServer(HandLogic hand)
    {
        Debug.Log($"DropObjectServer{hand} {IsOwner}");

        DropObjectObserver(hand);
    }

    [ObserversRpc]
    public void DropObjectObserver(HandLogic hand)
    {
        Debug.Log($"DropObjectObserver  {hand} {IsOwner} {hand.GrabObjectLayerBak}");

        var grabObject = hand.GrabObject;

        grabObject.State = GrabState.PutDown;
        grabObject.transform.parent = _worldObjectTransform;
        grabObject.gameObject.SetLayerOnAll(hand.GrabObjectLayerBak);

        var rigidbody = grabObject.GetComponent<Rigidbody>(); ;

        if (rigidbody != null)
        {
            rigidbody.isKinematic = grabObject.IsKinematicOnRelease;

            if (hand.LastVelocity.magnitude > ThrowMinVelocity)
            {
                rigidbody.isKinematic = false;
                rigidbody.velocity = hand.LastVelocity;
                grabObject.State = GrabState.Thrown;
            }

            if (rigidbody.isKinematic)
            {
                grabObject.transform.position = new Vector3(grabObject.transform.position.x, 0, grabObject.transform.position.z) + grabObject.DropPosOffset;
            }
        }

        hand.GrabObject = null;
        hand.GrabObjectLayerBak = -1;
    }

    [ServerRpc]
    public void SpawnDirtLump(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject dirtLump = Instantiate(prefab, position, rotation);
        dirtLump.transform.parent = _worldObjectTransform;
        ServerManager.Spawn(dirtLump);
    }
}