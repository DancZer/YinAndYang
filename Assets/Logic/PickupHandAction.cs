using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabAction : MonoBehaviour
{
    public enum Action
    {
        Unknown, PickUp, PutDown
    }

    public bool IsPickedUp { get; private set; }

    private Action action = Action.Unknown;


    public void OnHandMouseDown()
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

    public void OnHandMouseUp()
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

    public void OnHandMouseMove(Vector3 wordPos)
    {
        if (IsPickedUp)
        {
            transform.position = wordPos;
        }
    }
}
