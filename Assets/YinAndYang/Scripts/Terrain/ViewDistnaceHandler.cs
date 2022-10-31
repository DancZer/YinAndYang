using UnityEngine;

public class ViewDistnaceHandler
{
    private InnerViewDistancePreset[] _viewDistancePresets;

    public ViewDistnaceHandler(ViewDistancePreset[] viewDistances)
    {
        _viewDistancePresets = new InnerViewDistancePreset[viewDistances.Length];

        for (int i = 0; i < _viewDistancePresets.Length; i++)
        {
            _viewDistancePresets[i] = new InnerViewDistancePreset(viewDistances[i]);
        }
    }

    public void ChangeCenter(Vector2 pos)
    {
        foreach (var preset in _viewDistancePresets)
        {
            preset.ChangeCenter(pos);
        }
    }

    public bool ShouldExpandTile(MyTerrainTile terrainTile)
    {
        foreach (var preset in _viewDistancePresets)
        {
            if (preset.Area.Overlaps(terrainTile.Area))
            {
                if (terrainTile.Level > preset.Preset.RequiredLevel) return true;
            }
        }

        return false;
    }

    public void OnDrawGizmos()
    {
        foreach (var preset in _viewDistancePresets)
        {
            var area = preset.Area;

            var p00 = new Vector3(area.x, 0, area.position.y);
            var p01 = new Vector3(area.x, 0, area.position.y + area.height);
            var p11 = new Vector3(area.x + area.width, 0, area.position.y + area.height);
            var p10 = new Vector3(area.x + area.width, 0, area.position.y);

            Gizmos.DrawLine(p00, p01);
            Gizmos.DrawLine(p01, p11);
            Gizmos.DrawLine(p11, p10);
            Gizmos.DrawLine(p10, p00);
        }
    }

    private class InnerViewDistancePreset
    {
        public readonly ViewDistancePreset Preset;
        public Rect Area;

        public InnerViewDistancePreset(ViewDistancePreset preset)
        {
            Preset = preset;
            Area.width = Area.height = preset.ViewDistance;
        }

        public void ChangeCenter(Vector2 pos)
        {
            Area.x = pos.x - Area.width/2;
            Area.y = pos.y - Area.height/2;
        }
    }
}