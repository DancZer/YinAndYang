using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameResources
{
    Wood, Stone, Grain, Meat
}

public class GameResource : MonoBehaviour
{
    public GameResources Type;
    public int Amount;


    public static GameResource operator -(GameResource a, GameResource b)
    {
        if (a.Type != b.Type) throw new UnityException($"GameResources are not compatible {a.Type} {b.Type}");
        if (a.Amount < b.Amount) throw new UnityException($"GameResources are fulfill substract {a.Amount} {b.Amount}");

        var result = new GameResource
        {
            Type = a.Type,
            Amount = a.Amount - b.Amount
        };

        return result;
    }

    public static GameResource operator +(GameResource a, GameResource b)
    {
        if (a.Type != b.Type) throw new UnityException($"GameResources are not compatible {a.Type} {b.Type}");

        var result = new GameResource
        {
            Type = a.Type,
            Amount = a.Amount + b.Amount
        };

        return result;
    }
}

