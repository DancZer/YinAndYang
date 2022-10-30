using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public enum GrabState
{
    InHand, PutDown, Thrown
}

public class GrabObject : NetworkBehaviour
{
    public Vector3 GrabOffset = Vector3.zero;
    public Vector3 DropPosOffset = Vector3.zero;
    public bool SpawnObjectOnPickUp = false;
    public bool IsGrabAtTop = false;
    public bool IsKinematicOnRelease = false;

    public GameObject SpwanOnPickUpPrefab;
    
    [SyncVar] [HideInInspector] public GrabState State = GrabState.PutDown;
}
