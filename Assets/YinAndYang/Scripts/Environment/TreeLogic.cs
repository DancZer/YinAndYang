using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeLogic : MonoBehaviour
{
    private const float MinScaleMultiplier = 0.0001f;
    public string ForestTypeName = "";

    public int ForestDensity = 5;
    public int ForestMinDistance = 4;
    public int ForestMaxDistance = 10;
    
    public float ForestMinMaturity = 0.5f;
    public float ForestMaxMaturity = 1f;

    public float ForestTimeToMature = 10;


    public float TargetMaturity = 1f;

    public float GrowPercentage { get; private set; }
    public bool IsLogicEnabled 
    {
        get
        {
            return _grabObject != null ? _grabObject.State == GrabState.PutDown : true;
        }
    }

    private float _maturity = MinScaleMultiplier;
    private float _deltaMaturityCounter = 0f;

    private GrabObject _grabObject;

    void Start()
    {
        transform.localScale = Vector3.one * _maturity;

        _grabObject = GetComponent<GrabObject>();
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

    public void SetMaturityBySizePercentage(float value)
    {
        if(value < 0f) value = MinScaleMultiplier;
        if(value > 1f) value = 1f;

        _maturity = TargetMaturity * value;
        GrowPercentage = _maturity / TargetMaturity;

        transform.localScale = Vector3.one * _maturity;
    }
}
