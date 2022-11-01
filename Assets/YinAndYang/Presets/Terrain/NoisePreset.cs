using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Noise Preset", menuName = "Scriptables/Noise", order = 3)]
public class NoisePreset : ScriptableObject
{
    public FastNoise.NoiseType NoiseType;
    public float Frequency = 0.020f;

    public float Weight;

    public float Min;
    public float Max;

    public float SumMin;
    public float SumMax;

    public float GetNoiseValue(FastNoise noise, float x, float y, float sum)
    {
        noise.SetNoiseType(NoiseType);
        noise.SetFrequency(Frequency);

        var val = noise.GetNoise(x, y) * Weight;

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
