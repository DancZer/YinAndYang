using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 [ExecuteInEditMode]
public class TownGenerator : MonoBehaviour
{
    [Range(10, 300)]public float RingSize = 50;
    [Range(1, 36)]public int FirstRingSlice = 6;

    public GameObject TownCenterPrefab;
    public GameObject GranaryPrefab;
    public GameObject StockpilePrefab;
    public GameObject SmallHousePrefab;

    public GameObject ManPrefab;
    public GameObject WomanPrefab;

    public int MinPopulation = 10;
    public int MaxPopulation = 100;

#if UNITY_EDITOR
    public Transform CenterOfEditorTown;
    public float SizeInEditor = 100;
#else
    TerrainManager _terainManager;
#endif

#if UNITY_EDITOR
    bool ReGenerateEditorTown;

    void Update()
    {
        if (!ReGenerateEditorTown) return;

        GenerateTownInEditor();

        ReGenerateEditorTown = false;
    }

    void OnValidate()
    {
        ReGenerateEditorTown = true;
    }

    public void GenerateTownInEditor()
    {
        if (CenterOfEditorTown == null) return;

        for (int i = CenterOfEditorTown.childCount-1; i >= 0; i--)
        {
            var child = CenterOfEditorTown.GetChild(i);

            DestroyImmediate(child.gameObject);
        }

        Generate(CenterOfEditorTown.position, SizeInEditor);
    }

#else
    void Start()
    {
        _terainManager = StaticObjectAccessor.GetTerrainManager();
    }
#endif

    public void Generate(Vector3 center, float size)
    {
        var townCenter = GenerateTown(center, size);

        var population = Random.Range(MinPopulation, MaxPopulation);

        GeneratePopulation(townCenter, center, size, population);
    }

    TownCenter GenerateTown(Vector3 center, float size)
    {
        var townCenterObj = CreateObject(TownCenterPrefab, center, Vector3.up);
        var townCenter = townCenterObj.GetComponent<TownCenter>();

        var startIdx = Random.Range(0, 3);
        var granaryObj = CreateObject(GranaryPrefab, GetBuildingPos(startIdx, center), center);
        townCenter.SetGranary(granaryObj);

        startIdx += Random.Range(1, 3);
        var stockpileObj = CreateObject(StockpilePrefab, GetBuildingPos(startIdx, center), center);
        townCenter.SetStockpile(stockpileObj);

        for (var i = 0; i < Random.Range(1, 10); i++)
        {
            startIdx += Random.Range(1, 2);
            var buildingObj = CreateObject(SmallHousePrefab, GetBuildingPos(startIdx, center), center);

            townCenter.AddBuilding(buildingObj);
        }

        for (var i = 0; i < Random.Range(1, 10); i++)
        {
            startIdx += Random.Range(2, 3);
            var buildingObj = CreateObject(SmallHousePrefab, GetBuildingPos(startIdx, center), center);

            townCenter.AddBuilding(buildingObj);
        }

        return townCenter;
    }

    Vector3 GetBuildingPos(int idx, Vector3 center)
    {
        var ringIdx = 1;
        var ringSlice = FirstRingSlice;
        var sliceIdx = idx;

        while (sliceIdx >= ringSlice)
        {
            ringIdx++;
            sliceIdx -= ringSlice;
            ringSlice *= 2;
        }

        var dir = Quaternion.Euler(0, (360 / ringSlice) * sliceIdx, 0) * Vector3.forward;

        var pos = dir * ringIdx*RingSize;
        var buildingPos = center + pos;

        return buildingPos;
    }

    void GeneratePopulation(TownCenter townCenter, Vector3 center, float townSize, int size)
    {
        for (var i = 0; i < size; i++)
        {
            var pos = new Vector3(
                Random.Range(-townSize, townSize),
                0,
                Random.Range(-townSize, townSize));

            pos += center;

            var obj = CreateObject(i % 2 == 0 ? ManPrefab : WomanPrefab, pos, center);
            var person = obj.GetComponent<Person>();

            person.TownCenter = townCenter;
        }
    }

    GameObject CreateObject(GameObject prefab, Vector3 pos, Vector3 lookAt)
    {
        var dirPos = pos;
        dirPos.y = lookAt.y = 0;

        var dir = Quaternion.FromToRotation(Vector3.forward, dirPos - lookAt);

        var obj = Instantiate(prefab, GetPosOnTerrain(pos), dir);
#if UNITY_EDITOR
        obj.transform.parent = CenterOfEditorTown;
#endif

        return obj;
    }

    Vector3 GetPosOnTerrain(Vector3 pos)
    {
#if UNITY_EDITOR
        return pos;
#else
        return _terainManager.GetPosOnTerrain(pos);
#endif
    }
}
