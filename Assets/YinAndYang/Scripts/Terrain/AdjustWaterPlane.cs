using UnityEngine;

public class AdjustWaterPlane : MonoBehaviour
{
    public TerrainTileStatePreset MaxViewDistance;

    public float MoveAfterDistance = 500;

    private Vector2 _lastPos;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main == null) return;

        var camPos = Camera.main.transform.position.To2DInt();
        if (Vector2.Distance(_lastPos, camPos) > MoveAfterDistance)
        {
            transform.position = camPos.To3D();
            _lastPos = camPos;

            Camera.main.farClipPlane = MaxViewDistance.Distance;
            transform.localScale = new Vector3(MaxViewDistance.Distance, 1, MaxViewDistance.Distance);
        }
    }
}
