using UnityEngine;
using FishNet.Object;

public class PlayerColorObject : NetworkBehaviour
{
    public Renderer[] Renderers;

    public void ChangeColor(Color color)
    {
        if(Renderers != null)
        {
            foreach(var ren in Renderers)
            {
                ren.material.color = color;
            }
        }
    }
}
