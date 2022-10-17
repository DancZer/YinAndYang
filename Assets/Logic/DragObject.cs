using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    public enum Action
    {
        Unknown, PickUp, PutDown
    }

    private Vector3 offset;

    private float zCoord;

    public bool IsPickedUp { get; private set; }

    private Action action = Action.Unknown;


    private void OnMouseDown()
    {
        if (!IsPickedUp)
        {
            zCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
            offset = gameObject.transform.position - GetMouseWorldPos();
            action = Action.PickUp;
        }
        else
        {
            action = Action.PutDown;
        }
    }

    private void OnMouseUp()
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

    private void OnMouseOver()
    {
        if (IsPickedUp)
        {
            transform.position = GetMouseWorldPos() + offset;
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        var mousePoint = Input.mousePosition;

        mousePoint.z = zCoord;

        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
