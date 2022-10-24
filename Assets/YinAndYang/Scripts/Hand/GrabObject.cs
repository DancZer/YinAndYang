using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GrabState
{
    InHand, PutDown, Thrown
}

public class GrabObject : MonoBehaviour
{
    public Vector3 GrabOffset = Vector3.zero;
    public Vector3 DropPosOffset = Vector3.zero;
    public bool CreateLumpWhenGrab = false;
    public bool IsGrabAtTop = false;
    public bool IsKinematicOnRelease = false;
    
    public GrabState State { get; set;} = GrabState.PutDown;
}
