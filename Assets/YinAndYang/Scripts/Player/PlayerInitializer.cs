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

        SetTempleObject(headObj, handObj, conn.ClientId);
        UpdateColor(conn.ClientId, color);
    }

    [ObserversRpc]
    public void SetTempleObject(GameObject headObj, GameObject handObj, int clientId)
    {
        gameObject.name = $"Temple_{clientId:00}";
        headObj.name = $"Head_{clientId:00}";
        handObj.name = $"Hand_{clientId:00}";

        if (!IsOwner) return;

        var headMovement = headObj.GetComponent<HeadMovement>();
        headMovement.PlayerInit = this;
        headMovement.HandObject = handObj;
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
    public void UpdateColorServer(Color color, NetworkConnection conn = null)
    {
        UpdateColor(conn.ClientId, color);
    }

    [ObserversRpc]
    public void UpdateColor(int clientId, Color color)
    {
        PlayerColorObject[] components = FindObjectsOfType<PlayerColorObject>();

        foreach(var comp in components)
        {
            if (comp.OwnerId != clientId) continue;

            comp.ChangeColor(color);
        }
    }

}
