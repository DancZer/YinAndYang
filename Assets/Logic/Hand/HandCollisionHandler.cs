using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCollisionHandler : MonoBehaviour
{
    private Collider collider;
    private HandGrabLogic colliderHandGrabLogic;

    void Start()
    {

    }

    void OnTriggerEnter(Collider collider)
    {
        if(collider != null) return;

        collider = collider;

        colliderHandGrabLogic = collider.GetComponentsInParent<HandGrabLogic>();
    }

    void OnTriggerExit(Collider collider)
    {
        if(collider == collider) {
            collider = null;
            colliderHandGrabLogic = null;
        }
    }
}
