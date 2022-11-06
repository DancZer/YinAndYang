using UnityEngine;

public class BuildingOnTerrain : MonoBehaviour
{
    private MyTerrainManager _terrainManager;
    private BuildingFootprint _buildingFootprint;

    public bool MoveBuildingToTerrain = true;

    void Start()
    {
        _terrainManager = StaticObjectAccessor.GetMyTerrainManager();
        _buildingFootprint = GetComponentInChildren<BuildingFootprint>();
    }
    
    void Update()
    {
        if (_terrainManager.IsTerrainLoading) return;

        if (MoveBuildingToTerrain)
        {
            transform.position = _terrainManager.GetPosOnTerrain(transform.position);
        }

        if(_buildingFootprint != null) {
            var footprint = _buildingFootprint.GetFootprint();
            footprint.center = transform.position.ToXZ();
            _terrainManager.FlatTerrain(footprint, transform.position.y);
        }

        enabled = false;
    }
}