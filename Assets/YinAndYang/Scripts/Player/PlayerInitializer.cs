using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class PlayerInitializer : NetworkBehaviour
{
    public GameObject HeadPrefab;
    public GameObject HandPrefab;

    [HideInInspector] public Quaternion HeadStartDir;
    [HideInInspector] public Vector3 HeadStartPos;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            HeadStartDir = Quaternion.Euler(35, transform.rotation.eulerAngles.y, 0);
            HeadStartPos = transform.position + transform.up * 7 + Quaternion.Euler(0, transform.rotation.y,0) * (-transform.forward*7);

            SpawnPlayerControl(HeadStartPos, HeadStartDir, RandomColor());
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
    public void SpawnPlayerControl(Vector3 pos, Quaternion dir, Color color, NetworkConnection conn = null)
    {
        GameObject headObj = Instantiate(HeadPrefab, pos, dir);
        GameObject handObj = Instantiate(HandPrefab, pos, dir);

        ServerManager.Spawn(headObj, conn);
        ServerManager.Spawn(handObj, conn);

        SetTempleObject(headObj, handObj);
        UpdateColor(color);
    }

    [ObserversRpc]
    public void SetTempleObject(GameObject headObj, GameObject handObj)
    {
        if (!IsOwner) return;

        headObj.GetComponent<HeadMovement>().PlayerInit = this;
        handObj.GetComponent<HandMovement>().PlayerInit = this;
    }
    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.C) && Application.isFocused)
        {
            UpdateColorServer(RandomColor());
        }
    }

    [ServerRpc]
    public void UpdateColorServer(Color color)
    {
        UpdateColor(color);
    }

    [ObserversRpc]
    public void UpdateColor(Color color)
    {
        PlayerColorObject[] components = FindObjectsOfType<PlayerColorObject>();

        foreach(var comp in components)
        {
            Debug.Log($"UpdateColor {this} {IsOwner} {comp.IsOwner}");
            if (!comp.IsOwner) continue;

            comp.ChangeColor(color);
        }
    }

}
