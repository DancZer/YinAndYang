using System.Collections.Generic;
using UnityEngine;

public class QuadTreeNode<T>
{
    public static Vector3Int Node00Idx = new(0, 0, 0);
    public static Vector3Int Node01Idx = new(0, 0, 1);
    public static Vector3Int Node11Idx = new(1, 0, 1);
    public static Vector3Int Node10Idx = new(1, 0, 0);

    public readonly QuadTreeNode<T> Parent;
    public readonly Bounds Area;
    public readonly int Level;
    public readonly string Name;

    public IReadOnlyCollection<QuadTreeNode<T>> Children
    {
        get
        {
            return new List<QuadTreeNode<T>>() { Child00, Child01, Child11, Child10 };
        }
    }

    public QuadTreeNode<T> Child00 { get; private set; }
    public QuadTreeNode<T> Child01 { get; private set; }
    public QuadTreeNode<T> Child11 { get; private set; }
    public QuadTreeNode<T> Child10 { get; private set; }

    public T Data;

    public bool IsExpanded
    {
        get
        {
            return Child00 != null;
        }
    }

    public bool CanExpand
    {
        get
        {
            return Level > 0;
        }
    }

    public QuadTreeNode(int level, Bounds area, QuadTreeNode<T> parent = null)
    {
        Parent = parent;
        Area = area;
        Level = level;
        Name = $"Node_{level:00000}_{area.center.x:+00000;-00000}_{area.center.y:+00000;-00000}";
    }

    public void Expand()
    {
        if (IsExpanded) return;

        var childLevel = Level + 1;
        var childAreaDim = Area.size / 2f;
        var parentAreaPos = Area.center;

        Child00 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node00Idx);
        Child01 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node01Idx);
        Child11 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node11Idx);
        Child10 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node10Idx);
    }

    private QuadTreeNode<T> CreateChild(int size, Vector3 parentAreaPos, Vector3 areaSize, Vector3 idx)
    {
        var areaPos = new Vector3(parentAreaPos.x + (areaSize.x * idx.x)/2f, 0, parentAreaPos.z + (areaSize.z * idx.z)/2f);

        return new QuadTreeNode<T>(size, new Bounds(areaPos, areaSize), this);
    }

    public void Collapse()
    {
        if (!IsExpanded) return;

        CollapseChild(Child00);
        CollapseChild(Child01);
        CollapseChild(Child11);
        CollapseChild(Child10);

        Child00 = Child01 = Child11 = Child10 = null;
    }

    private void CollapseChild(QuadTreeNode<T> node)
    {
        if (node.IsExpanded)
        {
            node.Collapse();
        }
    }
}
