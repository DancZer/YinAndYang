using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class ForestManager : NetworkBehaviour
{
    public const float DeltaMaturityTreshold = 0.1f;
    
    public int StartTreeCount = 20;
    public int MaxTreeCount = 100;
        
    public float NewTreeGrowPercentageTreshold = 0.5f;
    public float ForestUpdateTime = 5;

    public GameObject[] treePrefabArray;
    private Dictionary<string, GameObject> _treePrefabDict = new Dictionary<string, GameObject>();

    private float _lastUpdateTime = 0;

    private List<TreeGrowthHandler> _globalTreeList = new List<TreeGrowthHandler>();
    private Dictionary<string, List<TreeGrowthHandler>> _treeByName = new Dictionary<string, List<TreeGrowthHandler>>();

    private Transform _worldObjectTransform;
    private TerrainManager _terrainManager;
    public override void OnStartServer()
    {
        base.OnStartServer();

        _worldObjectTransform = StaticObjectAccessor.GetWorldObject().transform;
        _terrainManager = StaticObjectAccessor.GetTerrainManager();

        foreach (var treePrefab in treePrefabArray){
            var handler = treePrefab.GetComponent<TreeGrowthHandler>();
            _treePrefabDict.Add(handler.ForestTypeName, treePrefab);
        }

        FindForests();
        CreateRandomTrees();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        enabled = false;
    }

    private void CreateRandomTrees()
    {
        var i = 0;
        var distance = 50;
        foreach (var treePrefab in treePrefabArray)
        {
            var referenceTree = treePrefab.GetComponent<TreeGrowthHandler>();
            var newTreeObj = CreateNewTree(referenceTree, new Vector3(Random.Range(distance * i, distance * (i+1)), 0, Random.Range(distance * i, distance * (i + 1))), Quaternion.identity, true);

            i++;
        }

        FindForests();

        while(_globalTreeList.Count < StartTreeCount){
            CheckForNewTree(true);
        }
        
        Debug.Log($"Tree count: {_globalTreeList.Count}");
    }

    void Update()
    {
        if (!IsServer) return;
        if (_lastUpdateTime + ForestUpdateTime > Time.timeSinceLevelLoad) return;

        _lastUpdateTime = Time.timeSinceLevelLoad;

        FindForests();

        if(_globalTreeList.Count >= MaxTreeCount) return;
        
        CheckForNewTree();
    }

    private void FindForests()
    {
        _globalTreeList.Clear(); ;
        _treeByName.Clear();

        foreach (var treeHandler in FindObjectsOfType<TreeGrowthHandler>())
        {
            _globalTreeList.Add(treeHandler);

            if (_treeByName.TryGetValue(treeHandler.ForestTypeName, out var treeHandlers))
            {
                treeHandlers.Add(treeHandler);
            }
            else
            {
                var list = new List<TreeGrowthHandler>();
                list.Add(treeHandler);
                _treeByName.Add(treeHandler.ForestTypeName, list);
            }
        }

        foreach(var treeName in _treeByName.Keys){
            if (!_treePrefabDict.ContainsKey(treeName))
            {
                Debug.LogError($"Tree {treeName} does not have a prefab assigned to forest!");
            }
        }
    }

    private void CheckForNewTree(bool init = false)
    {
        foreach (var trees in _treeByName.Values)
        {
            var treeTypeRef = trees[0];

            var maxTreeDistSqr = treeTypeRef.ForestMaxDistance * treeTypeRef.ForestMaxDistance;

            var processedTreeHash = new HashSet<TreeGrowthHandler>();
            var newTrees = new List<TreeGrowthHandler>();

            do
            {
                newTrees.Clear();

                foreach (var refTree in trees)
                {
                    if (!refTree.IsRooted) continue;
                    if (processedTreeHash.Contains(refTree)) continue; //tree already created new trees
                    if (refTree.GrowPercentage < NewTreeGrowPercentageTreshold) continue; //the tree is too young to make new

                    var densityCounter = 0;
                    var occupiedPositions = new List<Vector3>();

                    occupiedPositions.Add(refTree.transform.position);

                    foreach (var globalTree in _globalTreeList)
                    {
                        if (densityCounter >= treeTypeRef.ForestDensity) break;

                        if ((refTree.transform.position - globalTree.transform.position).sqrMagnitude < maxTreeDistSqr)
                        {
                            densityCounter++;

                            occupiedPositions.Add(globalTree.transform.position);
                        }
                    }

                    if (densityCounter < treeTypeRef.ForestDensity)
                    {
                        var newTreePos = FindSpotForNewTree(refTree, occupiedPositions);
                        var newTreeObj = CreateNewTree(refTree, newTreePos, refTree.transform.rotation, init);

                        newTrees.Add(newTreeObj);
                        _globalTreeList.Add(newTreeObj);
                        processedTreeHash.Add(refTree);
                        break;
                    }
                }

                trees.AddRange(newTrees);
            } while (newTrees.Count > 0 && !init);
        }
    }

    private Vector3 FindSpotForNewTree(TreeGrowthHandler referenceTree, List<Vector3> occupiedPositions)
    {
        var minTreeDistSqr = referenceTree.ForestMinDistance * referenceTree.ForestMinDistance;
        var maxTreeDistSqr = referenceTree.ForestMaxDistance * referenceTree.ForestMaxDistance;
        var refPos = referenceTree.transform.position;

        var found = false;
        var newPos = Vector3.zero;

        var locatorRefVector = Vector3.forward * Random.Range(referenceTree.ForestMinDistance, referenceTree.ForestMaxDistance);
        var locatorQuaterion = Quaternion.Euler(0, Random.Range(-180, 180f), 0);

        var locatorAngleStep = 15;
        var locatorAngleSum = 0;

        while (!found)
        {
            found = true;

            newPos = refPos + locatorQuaterion * locatorRefVector;

            foreach (var pos in occupiedPositions)
            {
                if ((newPos - pos).sqrMagnitude <= minTreeDistSqr)
                {
                    found = false;
                }
            }

            if (!found)
            {
                locatorQuaterion *= Quaternion.Euler(0, locatorAngleStep, 0);
                locatorAngleSum += locatorAngleStep;

                if (locatorAngleSum >= 360)
                {
                    locatorAngleSum %= 360;
                    locatorRefVector += Vector3.forward;
                }
            }
        }

        if ((newPos - refPos).sqrMagnitude >= maxTreeDistSqr)
        {
            Debug.LogWarning($"Tree is outside the ref. tree max distance: {referenceTree.ForestTypeName} at {newPos} in range {(newPos - refPos).magnitude}");
        }

        return _terrainManager.GetGroundPosAtCord(newPos);
    }

    private TreeGrowthHandler CreateNewTree(TreeGrowthHandler referenceTree, Vector3 pos, Quaternion rotation, bool init = false)
    {
        var newTreeObj =  Instantiate(_treePrefabDict.GetValueOrDefault(referenceTree.ForestTypeName), pos, rotation, _worldObjectTransform);
        var newTreeHandler = newTreeObj.GetComponent<TreeGrowthHandler>();

        newTreeHandler.TargetMaturity = Random.Range(referenceTree.ForestMinMaturity, referenceTree.ForestMaxMaturity);

        if (init)
        {
            newTreeHandler.SetMaturityBySizePercentage(1f);
        }

        ServerManager.Spawn(newTreeObj);

        return newTreeHandler;
    }
}