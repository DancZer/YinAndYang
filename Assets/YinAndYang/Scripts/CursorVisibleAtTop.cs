using UnityEngine;

public class CursorVisibleAtTop : MonoBehaviour
{
    void Update()
    { 
        var mousePos = Input.mousePosition;
        if(mousePos.y > Screen.height - 80 || !MiscHelper.IsOnTheScreen(mousePos))
        {
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
        }
    }
}
