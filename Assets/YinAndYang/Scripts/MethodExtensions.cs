using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MethodExtensions
{
    public static int GetLastLayer(this LayerMask layerMask)
    {
        var layer = layerMask.value;
        
        var layerIdx = 0;
        while((layer = layer>>1)>0) layerIdx++;

        return layerIdx;
    }

    public static void SetLayerOnAll(this GameObject obj, int layer) 
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true)) 
        {
            trans.gameObject.layer = layer;
        }
    }  
}
