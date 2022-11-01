using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Noise Preset", menuName = "Scriptables/Noise", order = 3)]
public class NoisePreset : ScriptableObject
{
    public float CoefX;
    public float CoefY;
    public float CoefXY;
    public float CoefWeight;

    public float Min;
    public float Max;

    public float SumMin;
    public float SumMax;

    public float GetNoiseValue(FastNoise noise, float x, float y, float sum)
    {
        var val = noise.GetSimplex(x * CoefX * CoefXY, y * CoefY * CoefXY) * CoefWeight;

        if (val < Min)
        {
            val = Min;
        }

        if (val > Max)
        {
            val = Max;
        }

        sum += val;

        if (sum < SumMin)
        {
            sum = SumMin;
        }

        if (sum > SumMax)
        {
            sum = SumMax;
        }

        return sum;
    }
}
