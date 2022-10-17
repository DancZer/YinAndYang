using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestLogic : MonoBehaviour
{
    public const float DeltaMaturityTreshold = 0.1f;
    
    public float NewTreeGrowPercentageTreshold = 0.5f;
    public float ForestUpdateTime = 5;

    public GameObject[] treePrefabArray;
    private Dictionary<string, GameObject> treePrefabDict = new Dictionary<string, GameObject>();

    private float lastUpdateTime = 0;

    private List<TreeLogic> globalTreeList = new List<TreeLogic>();
    private Dictionary<string, List<TreeLogic>> treeByName = new Dictionary<string, List<TreeLogic>>();

    // Start is called before the first frame update
    void Start()
    {
        foreach(var treePrefab in treePrefabArray){
            var logic = treePrefab.GetComponent<TreeLogic>();
            treePrefabDict.Add(logic.ForestTypeName, treePrefab);
        }

        FindForests();

        CreateRandomTrees();
    }

    private void CreateRandomTrees()
    {
        var i = 0;
        var distance = 50;
        foreach (var treePrefab in treePrefabArray)
        {
            var treeLogic = treePrefab.GetComponent<TreeLogic>();
            var newTree = Instantiate(treePrefabDict.GetValueOrDefault(treeLogic.ForestTypeName), new Vector3(Random.Range(distance * i, distance * (i+1)), 0, Random.Range(distance * i, distance * (i + 1))), Quaternion.identity);
            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lastUpdateTime + ForestUpdateTime > Time.timeSinceLevelLoad) return;

        lastUpdateTime = Time.timeSinceLevelLoad;

        FindForests();
        CheckForNewTree();
    }

    private void FindForests()
    {
        globalTreeList.Clear(); ;
        treeByName.Clear();

        foreach (var treeLogic in FindObjectsOfType<TreeLogic>())
        {
            globalTreeList.Add(treeLogic);

            if (treeByName.TryGetValue(treeLogic.ForestTypeName, out var treeLogics))
            {
                treeLogics.Add(treeLogic);
            }
            else
            {
                var list = new List<TreeLogic>();
                list.Add(treeLogic);
                treeByName.Add(treeLogic.ForestTypeName, list);
            }
        }

        foreach(var treeName in treeByName.Keys){
            if (!treePrefabDict.ContainsKey(treeName))
            {
                Debug.LogError($"Tree {treeName} does not have a prefab assigned to forest!");
            }
        }
    }

    private void CheckForNewTree()
    {
        foreach (var trees in treeByName.Values)
        {
            var treeTypeRef = trees[0];

            var maxTreeDistSqr = treeTypeRef.ForestMaxDistance * treeTypeRef.ForestMaxDistance;

            var processedTreeHash = new HashSet<TreeLogic>();
            var newTrees = new List<TreeLogic>();

            do
            {
                newTrees.Clear();

                foreach (var refTree in trees)
                {
                    if (!refTree.IsInTheGround) continue;
                    if (processedTreeHash.Contains(refTree)) continue; //tree already created new trees
                    if (refTree.GrowPercentage < NewTreeGrowPercentageTreshold) continue; //the tree is too young to make new

                    var densityCounter = 0;
                    var occupiedPositions = new List<Vector3>();

                    occupiedPositions.Add(refTree.transform.position);

                    foreach (var globalTree in globalTreeList)
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
                        var newTree = CreateNewTree(refTree, occupiedPositions);

                        newTrees.Add(newTree);
                        globalTreeList.Add(newTree);
                        processedTreeHash.Add(refTree);
                        break;
                    }
                }

                trees.AddRange(newTrees);
            } while (newTrees.Count > 0);
        }
    }

    private TreeLogic CreateNewTree(TreeLogic referenceTree, List<Vector3> occupiedPositions)
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

                Debug.Log($"CreateNewTree at {refPos} search angle {locatorAngleSum} distance {locatorRefVector.magnitude}");
            }
        }

        if ((newPos - refPos).sqrMagnitude >= maxTreeDistSqr)
        {
            Debug.LogWarning($"Tree is outside the ref. tree max distance: {referenceTree.ForestTypeName} at {newPos} in range {(newPos - refPos).magnitude}");
        }

        newPos.y = GetWorldYCord();
        var newTree =  Instantiate(treePrefabDict.GetValueOrDefault(referenceTree.ForestTypeName), newPos, referenceTree.transform.rotation);
        var newTreeLogic = newTree.GetComponent<TreeLogic>();

        newTreeLogic.TargetMaturity = Random.Range(referenceTree.ForestMinMaturity, referenceTree.ForestMaxMaturity);

        Debug.Log($"NewTree at {newPos} with target maturity {newTreeLogic.TargetMaturity}");

        return newTreeLogic;
    }

    private float GetWorldYCord()
    {
        return 0;
    }
}