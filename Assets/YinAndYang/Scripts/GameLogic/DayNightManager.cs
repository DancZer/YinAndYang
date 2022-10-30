using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class DayNightManager : NetworkBehaviour
{   
    //Scene References
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightPreset Preset;

    private GameTimeManager _timeManager;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _timeManager = StaticObjectAccessor.GetTimeManager();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServer)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        
        UpdateLighting(_timeManager.TimeOfTheDay / 24f);
    }

    [ObserversRpc]
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