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
    public bool IsLogicEnabled = true;

    private float _maturity = 0;
    private float _deltaMaturityCounter = 0;

    void Start()
    {
        transform.localScale = Vector3.one / 1000;
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsLogicEnabled) return;
        if (_maturity >= TargetMaturity) return;

        var maturityTime = TargetMaturity / ForestTimeToMature;
        var deltaMaturity = maturityTime * Time.deltaTime;

        _deltaMaturityCounter += deltaMaturity;

        if (_deltaMaturityCounter < ForestLogic.DeltaMaturityTreshold) return;

        _maturity += _deltaMaturityCounter;
        _deltaMaturityCounter = 0;

        if (_maturity > TargetMaturity)
        {
            _maturity = TargetMaturity;
            GrowPercentage = _maturity / TargetMaturity;
        }

        if(_maturity > 0)
        {
            transform.localScale = Vector3.one * _maturity;
        }
    }
}
