using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

[ExecuteAlways]
public class DayNightManager : NetworkBehaviour
{   
    //Scene References
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightPreset Preset;

    [SerializeField] [Range(0, 24)] private float EditorTimeOfDay;


    private TimeManager _timeManager;

    //Try to find a directional light to use if we haven't set one
    protected override void OnValidate()
    {
        base.OnValidate();

        if (DirectionalLight != null)
            return;

        //Search for lighting tab sun
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        //Search scene for light that fits criteria (directional)
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        _timeManager = StaticObjectAccessor.GetTimeManager();
    }

    private void Update()
    {
        if (Preset == null)
            return;

        if (Application.isPlaying)
        {
            if (IsServer)
            {
                UpdateLighting(_timeManager.TimeOfTheDay / 24f);
            }
        }
        else
        {
            UpdateLighting(EditorTimeOfDay / 24f);
        }   
    }


    private void UpdateLighting(float timePercent)
    {
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);

            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
        }

    }
}