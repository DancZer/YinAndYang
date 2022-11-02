using System.Collections.Generic;
using UnityEngine;

public class QuadTreeNode<T>
{
    public static Vector2Int Node00Idx = new(0, 0);
    public static Vector2Int Node01Idx = new(0, 1);
    public static Vector2Int Node11Idx = new(1, 1);
    public static Vector2Int Node10Idx = new(1, 0);

    public readonly QuadTreeNode<T> Parent;
    public readonly Rect Area;
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

    public QuadTreeNode(int level, Rect area, QuadTreeNode<T> parent = null)
    {
        Parent = parent;
        Area = area;
        Level = level;
        Name = $"Node_{level:00000}_{area.x:+00000;-00000}_{area.y:+00000;-00000}";
    }

    public void Expand()
    {
        if (IsExpanded) return;

        var childLevel = Level + 1;
        var childAreaDim = Area.size / 2f;
        var parentAreaPos = Area.position;

        Child00 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node00Idx);
        Child01 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node01Idx);
        Child11 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node11Idx);
        Child10 = CreateChild(childLevel, parentAreaPos, childAreaDim, Node10Idx);
    }

    private QuadTreeNode<T> CreateChild(int size, Vector2 parentAreaPos, Vector2 areaSize, Vector2 idx)
    {
        var areaPos = new Vector2(parentAreaPos.x + areaSize.x * idx.x, parentAreaPos.y + areaSize.y * idx.y);

        return new QuadTreeNode<T>(size, new Rect(areaPos, areaSize), this);
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
