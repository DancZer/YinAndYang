using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeLogic : MonoBehaviour
{
    public string ForestTypeName = "";

    public int ForestDensity = 5;
    public int ForestMinDistance = 4;
    public int ForestMaxDistance = 10;
    
    public float ForestMinMaturity = 0.5f;
    public float ForestMaxMaturity = 1f;

    public float ForestTimeToMature = 10;


    public float TargetMaturity = 1f;

    public float GrowPercentage { get; private set; }

    private float maturity = 0;
    private float deltaMaturityCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = Vector3.one * maturity;
    }

    // Update is called once per frame
    void Update()
    {
        if (maturity >= TargetMaturity) return;

        var maturityTime = TargetMaturity / ForestTimeToMature;
        var deltaMaturity = maturityTime * Time.deltaTime;

        deltaMaturityCounter += deltaMaturity;

        if (deltaMaturityCounter < ForestLogic.DeltaMaturityTreshold) return;

        maturity += deltaMaturityCounter;
        deltaMaturityCounter = 0;

        if (maturity > TargetMaturity)
        {
            maturity = TargetMaturity;
            GrowPercentage = maturity / TargetMaturity;
        }

        transform.localScale = Vector3.one * maturity;
    }
}
