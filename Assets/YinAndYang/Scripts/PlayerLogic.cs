using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerLogic : NetworkBehaviour
{
    [SyncVar] public int health = 10;

    public GameObject HandPrefab;

    private Color _color;

    [HideInInspector] public GameObject HandObject;
    public GameObject BodyObject;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsOwner)
            GetComponent<PlayerLogic>().enabled = false;

        _color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

        SpawnHand(HandPrefab, transform, this);
        ChangeColorServer(gameObject, _color);
    }

    [ServerRpc]
    public void SpawnHand(GameObject prefab, Transform transform, PlayerLogic player)
    {
        GameObject handObj = Instantiate(prefab, transform.position + transform.forward, Quaternion.identity);
        ServerManager.Spawn(handObj, player.NetworkManager.ClientManager.Connection);
        SetHandObject(handObj, player);
    }
 
    [ObserversRpc]
    public void SetHandObject(GameObject handObj, PlayerLogic player)
    {
        player.HandObject = handObj;
    }

    [ServerRpc]
    public void ChangeColorServer(GameObject player, Color color)
    {
        ChangeColor(player, color);
    }

    [ObserversRpc]
    public void ChangeColor(GameObject player, Color color)
    {
        var playerLogic = player.GetComponent<PlayerLogic>();

        if (playerLogic.HandObject != null)
        {
            playerLogic.HandObject.GetComponentInChildren<Renderer>().material.color = color;
        }

        if (playerLogic.BodyObject != null)
        {
            playerLogic.BodyObject.GetComponent<Renderer>().material.color = color;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UpdateHealth(this, -1);
        }
    }

    [ServerRpc]
    public void UpdateHealth(PlayerLogic player, int amountToChange)
    {
        player.health += amountToChange;
    }
}
