using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeDisappearLogic : MonoBehaviour
{
    public float TimeoutInSec = 10;
    public float FadeOutInSec = 5;

    private float _timeTillFadeOut;
    private float _transparentChangeInSec;

    private MeshRenderer _meshRenderer;
    private Material _material;

    // Start is called before the first frame update
    void Start()
    {
        _timeTillFadeOut = TimeoutInSec;

        _transparentChangeInSec = 1f/FadeOutInSec;
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _material = _meshRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {   
        _timeTillFadeOut -= Time.deltaTime;

        if(_timeTillFadeOut < 0){
            Color color = _material.color;
            var alpha = (-_timeTillFadeOut)*_transparentChangeInSec;
            Debug.Log($"FadeDisappearLogic {_timeTillFadeOut} {alpha}");
            color.a = Mathf.Clamp( alpha, 0, 1 );
            _material.color = color;
            
            if(_timeTillFadeOut < -FadeOutInSec){
                Destroy(gameObject);
            }
        }
    }
}
