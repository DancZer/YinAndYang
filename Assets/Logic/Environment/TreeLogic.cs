using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeLogic : MonoBehaviour
{
    public string ForestTypeName = "";

    public int ForestMinDistance = 4;
    public int ForestMaxDistance = 10;
    public int ForestDensity = 5;


    public float ForestMinMaturity = 0.5f;
    public float ForestMaxMaturity = 0.5f;

    public float ForestSpreadTime = 30;

    public float TimeToMature = 10;

    public float GrowPercentage { get; private set; }

    /// <summary>
    /// Defines the max maturity level. Default is 1f, which is the prefab default size.
    /// </summary>
    public float TargetMaturity = 1f;

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

        var maturityTime = TargetMaturity / TimeToMature;
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
