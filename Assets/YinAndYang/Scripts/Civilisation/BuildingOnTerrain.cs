using UnityEngine;
using FishNet.Object;

public class BuildingOnTerrain : NetworkBehaviour
{
    private TerrainManager _terrainManager;
    private BuildingFootprint _buildingFootprint;

    public bool MoveBuildingToTerrain = true;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _terrainManager = StaticObjectAccessor.GetTerrainManager();
        _buildingFootprint = GetComponentInChildren<BuildingFootprint>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServer)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (_terrainManager.IsLoading || _terrainManager.GetTileAt(transform.position.To2D()).CurrentState < TerrainTileState.BlendedHeightMap) return;

        if (MoveBuildingToTerrain)
        {
            transform.position = _terrainManager.GetPosOnTerrain(transform.position);
        }

        if(_buildingFootprint != null) {
            _terrainManager.FlatTerrain(_buildingFootprint.GetFootprint(), transform.position.y);
        }

        enabled = false;
    }
}