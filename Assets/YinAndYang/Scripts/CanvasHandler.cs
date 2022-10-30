using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasHandler : MonoBehaviour
{
    void Update()
    { 
        var mousePos = Input.mousePosition;
        if(mousePos.x < 200 && mousePos.y > Screen.height - 130 || !MiscHelper.IsOnTheScreen(mousePos))
        {
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
        }
    }
}