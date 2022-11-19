using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class HandMovement : NetworkBehaviour
{
    private enum HandActionState
    {
        None, GrabStarted, GrabTriggered, Grabbed, DropTriggered
    }

    public LayerMask HandLayerMask;
    public string GroundTag = "Ground";
    public float HandHeight = 0.3f;
    public float ThrowMinVelocity = 0.2f;
    public float MouseLongPressDeltaTime = .1f;
    public Vector3 GrabOffset = Vector3.zero;

    [HideInInspector] public GrabObject GrabObject = null;

    private float _mouseDownTimeStart;
    private int _handLayer;

    private Vector3 _lastVelocity;
    private Vector3 _handObjectLastPos;

    private GrabObject _grabObjectLongPress;
    private int _grabObjectLayerBak = -1;
    private HandActionState _handActuinState = HandActionState.None;
    private Transform _innerContainerTransform;
    private TerrainManager _myTerrainManager;

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            _myTerrainManager = StaticObjectAccessor.GetTerrainManager();
            _handLayer = HandLayerMask.GetLastLayer();
            _innerContainerTransform = transform.GetChild(0);
            
        }
        else
        {
            enabled = false;
        }
    }

    void Update() 
    {
        if (!Application.isFocused || !IsOwner || !MiscHelper.IsOnTheScreen(Input.mousePosition) || !_myTerrainManager.IsPlayerAreaLoaded) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~HandLayerMask))
        {
            HandleHandMove(hit, ray);
            HandleMouseButtons(hit);
        }

        _lastVelocity = (transform.position - _handObjectLastPos) / Time.deltaTime;
        _handObjectLastPos = transform.position;
    }

    private void HandleHandMove(RaycastHit hit, Ray ray){
        if(hit.collider == null) return;

        var handWordPos = hit.point;

        if(hit.collider.gameObject.tag == GroundTag)
        {
            handWordPos.y = HandHeight;
        }

        var handLocalRot = Quaternion.identity;

        if (GrabObject != null && GrabObject.State == GrabState.InHand){
            if(GrabObject.IsGrabAtTop){
                handWordPos.y += GrabObject.DropPosOffset.y;
            }

            if(!GrabObject.IsGrabAtTop)
            {
                handLocalRot = Quaternion.Euler(0, 0, -90);
            }
        }
        _innerContainerTransform.localRotation = handLocalRot;
        transform.SetPositionAndRotation(handWordPos, Quaternion.LookRotation(new Vector3(ray.direction.x, 0, ray.direction.z), Vector3.up));
    }

    private void HandleMouseButtons(RaycastHit hit)
    {
        if (_handActuinState == HandActionState.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hit.collider.gameObject.tag != GroundTag)
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
                    GrabObjectServer(_grabObjectLongPress);
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
                    DropObjectServer(_lastVelocity);
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
    public void GrabObjectServer(GrabObject grabObject, NetworkConnection conn = null)
    {
        GrabObjectObserver(grabObject);
    }

    [ObserversRpc]
    public void GrabObjectObserver(GrabObject grabObject)
    {
        GrabObject = grabObject;

        _grabObjectLayerBak = grabObject.gameObject.layer;

        var rigidbody = grabObject.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
        }

        grabObject.gameObject.SetLayerOnAll(_handLayer);

        if (IsOwner)
        {
            if (grabObject.SpawnObjectOnPickUp && grabObject.State == GrabState.PutDown)
            {
                SpawnObjectOnPickUp(grabObject.SpwanOnPickUpPrefab, grabObject.transform, Quaternion.identity);
            }
        }

        Quaternion objRot;

        if (grabObject.IsGrabAtTop)
        {
            objRot = Quaternion.Inverse(grabObject.transform.rotation) * transform.rotation;
        }
        else
        {
            objRot = Quaternion.Inverse(Quaternion.Euler(0, grabObject.transform.rotation.eulerAngles.y, 0)) * Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        grabObject.State = GrabState.InHand;
        grabObject.transform.parent = transform;
        grabObject.transform.localRotation = objRot;
        grabObject.transform.localPosition = GrabOffset+Vector3.Scale(grabObject.GrabOffset, grabObject.transform.localScale)*-1;
    }

    [ServerRpc]
    public void DropObjectServer(Vector3 lastVelocity, NetworkConnection conn = null)
    {
        DropObjectObserver(lastVelocity);
    }

    [ObserversRpc]
    public void DropObjectObserver(Vector3 lastVelocity)
    {
        GrabObject.State = GrabState.PutDown;
        GrabObject.transform.parent = null;
        GrabObject.gameObject.SetLayerOnAll(_grabObjectLayerBak);

        var rigidbody = GrabObject.GetComponent<Rigidbody>(); ;

        if (rigidbody != null)
        {
            rigidbody.isKinematic = GrabObject.IsKinematicOnRelease;
            
            if (lastVelocity.magnitude > ThrowMinVelocity)
            {
                rigidbody.isKinematic = false;
                rigidbody.velocity = lastVelocity;
                GrabObject.State = GrabState.Thrown;
            }

            if (rigidbody.isKinematic)
            {
                GrabObject.transform.position = _myTerrainManager.GetPosOnTerrain(new Vector3(GrabObject.transform.position.x, 0, GrabObject.transform.position.z)) + GrabObject.DropPosOffset;
            }
        }
        _grabObjectLayerBak = -1;
    
        GrabObject = null;
    }

    [ServerRpc]
    public void SpawnObjectOnPickUp(GameObject prefab, Transform transform, Quaternion rotation, NetworkConnection conn = null)
    {
        var newObj = Instantiate(prefab);
        newObj.transform.localScale = transform.localScale;
        newObj.transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        newObj.transform.rotation = rotation;
        ServerManager.Spawn(newObj, conn);
    }
}