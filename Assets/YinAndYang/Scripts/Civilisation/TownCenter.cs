using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenter : MonoBehaviour
{
    public string TownName;

    private GameObject _granary;
    private GameObject _stockpile;
    private List<GameObject> _buildings = new();

    public void SetGranary(GameObject granary)
    {
        _granary = granary;
    }

    public void SetStockpile(GameObject stockpile)
    {
        _stockpile = stockpile;
    }

    public void AddBuilding(GameObject building)
    {
        _buildings.Add(building);
    }
}
