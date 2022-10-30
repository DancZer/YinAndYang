using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class HandSpawner : NetworkBehaviour
{
    public GameObject HandPrefab;

    [HideInInspector] public GameObject HandObject;
    public Renderer BodyRenderer;

    public override void OnStartClient()
    {
        base.OnStartClient();

        BodyRenderer = transform.GetChild(0).GetComponent<Renderer>();

        if (IsOwner)
        {
            SpawnHand(RandomColor());
        }
        else
        {
            enabled = false;
        }
    }

    private Color RandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

    [ServerRpc]
    public void SpawnHand(Color color, NetworkConnection conn = null)
    {
        GameObject handObj = Instantiate(HandPrefab, transform.position + transform.forward, Quaternion.identity);

        ServerManager.Spawn(handObj, conn);
        SetHandObject(handObj, color);
    }
 
    [ObserversRpc]
    public void SetHandObject(GameObject handObj, Color color)
    {
        HandObject = handObj;
        UpdateColor(color);
    }

    [ServerRpc]
    public void UpdateColorServer(Color color, NetworkConnection conn = null)
    {
        UpdateColor(color);
    }

    [ObserversRpc]
    public void UpdateColor(Color color)
    {
        HandObject.GetComponent<HandMovement>().SetColor(color);
        BodyRenderer.material.color = color;
    }

    void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            UpdateColorServer(RandomColor());
        }
    }
}
