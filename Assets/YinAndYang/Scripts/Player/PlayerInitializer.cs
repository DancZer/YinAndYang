using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class PlayerInitializer : NetworkBehaviour
{
    public GameObject HeadPrefab;
    public GameObject HandPrefab;

    [HideInInspector] [SyncVar] public Quaternion HeadStartDir;
    [HideInInspector] [SyncVar] public Vector3 HeadStartPos;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            HeadStartDir = Quaternion.Euler(35, transform.rotation.eulerAngles.y, 0);
            HeadStartPos = transform.position + transform.up * 7 + Quaternion.Euler(0, transform.rotation.y,0) * (-transform.forward*7);

            SpawnPlayerControl(gameObject, RandomColor());
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
    public void SpawnPlayerControl(GameObject templeObject, Color color, NetworkConnection conn = null)
    {
        GameObject headObj = Instantiate(HeadPrefab, HeadStartPos, HeadStartDir);
        GameObject handObj = Instantiate(HandPrefab, HeadStartPos, HeadStartDir);

        ServerManager.Spawn(headObj, conn);
        ServerManager.Spawn(handObj, conn);

        SetTempleObject(headObj, handObj);
        UpdateColor(color);
    }

    [ObserversRpc]
    public void SetTempleObject(GameObject headObj, GameObject handObj)
    {
        headObj.GetComponent<HeadMovement>().PlayerInit = this;
        handObj.GetComponent<HandMovement>().PlayerInit = this;
    }

        [ServerRpc]
    public void UpdateColorServer(Color color, NetworkConnection conn = null)
    {
        UpdateColor(color);
    }

    [ObserversRpc]
    public void UpdateColor(Color color)
    {
        PlayerColorObject[] components = GameObject.FindObjectsOfType<PlayerColorObject>();

        foreach(var c in components)
        {
            c.ChangeColor(color);
        }
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
