using UnityEngine;

public class BuildingOnTerrain : MonoBehaviour
{
    private MapManager _terrainManager;
    private BuildingFootprint _buildingFootprint;

    public bool MoveBuildingToTerrain = true;

    void Start()
    {
        _terrainManager = StaticObjectAccessor.GetTerrainManager();
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
            _terrainManager.FlatTerrain(_buildingFootprint.GetFootprint(), transform.position.y);
        }

        enabled = false;
    }
}