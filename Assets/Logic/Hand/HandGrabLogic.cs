using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabLogic : MonoBehaviour
{
    public enum Action
    {
        Unknown, PickUp, PutDown
    }

    public Vector3 GrabOffset = Vector3.zero;

    public bool IsPickedUp { get; private set; }

    private Action action = Action.Unknown;

    private void OnHandMouseDown()
    {
        if (!IsPickedUp)
        {
            action = Action.PickUp;
        }
        else
        {
            action = Action.PutDown;
        }
    }

    private void OnHandMouseUp()
    {
        if (action == Action.PickUp)
        {
            IsPickedUp = true;
        }
        else if(action == Action.PutDown)
        {
            var onTheGroundPos = transform.position;
            onTheGroundPos.y = 0;
            transform.position = onTheGroundPos;
            IsPickedUp = false;
        }
    }

    private void OnHandMouseMove(Vector3 worldPos)
    {
        if (IsPickedUp)
        {
            transform.position = worldPos + GrabOffset;
        }
    }
}
