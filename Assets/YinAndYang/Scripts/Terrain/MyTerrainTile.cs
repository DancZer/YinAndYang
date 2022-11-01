using UnityEngine;

public class MyTerrainTile
{
    public static Vector2Int Node00Idx = new(0, 0);
    public static Vector2Int Node01Idx = new(0, 1);
    public static Vector2Int Node11Idx = new(1, 1);
    public static Vector2Int Node10Idx = new(1, 0);

    public readonly MyTerrainTile Parent;
    public readonly string TileName;
    public readonly Rect Area;
    public readonly int Level;

    public MyTerrainTile Child00;
    public MyTerrainTile Child01;
    public MyTerrainTile Child11;
    public MyTerrainTile Child10;

    public Mesh Mesh;

    public bool IsRendered = false;
    public GameObject GameObject;

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

    public MyTerrainTile(int level, Rect area, MyTerrainTile parent = null)
    {
        TileName = $"Tile_{area.x:+000000;-000000}_{area.y:+000000;-000000}_{level:00}";
        Parent = parent;
        Area = area;
        Level = level;
    }
    public void Update(ViewDistnaceHandler viewDistnaceHandler)
    {
        if (viewDistnaceHandler.ShouldExpandTile(this))
        {
            if (!CanExpand) return;

            if (!IsExpanded)
            {
                Expand();
            }

            Child00.Update(viewDistnaceHandler);
            Child01.Update(viewDistnaceHandler);
            Child11.Update(viewDistnaceHandler);
            Child10.Update(viewDistnaceHandler);
        }
        else
        {
            if (IsExpanded)
            {
                Collapse();
            }
        }
    }

    private void Expand()
    {
        if (IsExpanded) return;

        var childSize = Level - 1;

        if (childSize < 0) return;

        var childAreaDim = Area.size / 2f;
        var parentAreaPos = Area.position;

        Child00 = CreateTile(childSize, parentAreaPos, childAreaDim, Node00Idx);
        Child01 = CreateTile(childSize, parentAreaPos, childAreaDim, Node01Idx);
        Child11 = CreateTile(childSize, parentAreaPos, childAreaDim, Node11Idx);
        Child10 = CreateTile(childSize, parentAreaPos, childAreaDim, Node10Idx);

        if (GameObject != null)
        {
            GameObject.Destroy(GameObject);
        }

        IsRendered = false;
    }

    private MyTerrainTile CreateTile(int size, Vector2 parentAreaPos, Vector2 areaSize, Vector2 idx)
    {
        var areaPos = new Vector2(parentAreaPos.x + areaSize.x * idx.x, parentAreaPos.y + areaSize.y * idx.y);
        return new MyTerrainTile(size, new Rect(areaPos, areaSize), this);
    }

    public void Collapse()
    {
        if (!IsExpanded) return;

        CollapseChild(Child00);
        CollapseChild(Child01);
        CollapseChild(Child11);
        CollapseChild(Child10);

        Child00 = Child01 = Child11 = Child10 = null;
        IsRendered = false;
    }

    private void CollapseChild(MyTerrainTile node)
    {
        if (node.IsExpanded)
        {
            node.Collapse();
        }

        if (node.GameObject != null)
        {
            GameObject.Destroy(node.GameObject);
        }
    }

    
}
