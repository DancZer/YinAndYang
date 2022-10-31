using UnityEngine;

public class MyTerrainManager : MonoBehaviour
{
    public float TileSize = 10;
    public int Level = 2;
    public int PlayerNearAreaSize = 20;

    private MyTerrainChunk _chunk;
    private MyTerrainRenderer _renderer;
    private Camera _playerCamera;
    private Rect _playerNearArea;
    private Vector2 _playerNearAreaSizeHalf;

    // Start is called before the first frame update
    void Start()
    {
        var viewDistance = TileSize * Mathf.Pow(2, Level);

        _chunk = new MyTerrainChunk(Level, new Rect(viewDistance / -2, viewDistance / -2, viewDistance, viewDistance));
        _renderer = GetComponent<MyTerrainRenderer>();
        _playerCamera = Camera.main;
        _playerNearArea = new Rect(0, 0, PlayerNearAreaSize, PlayerNearAreaSize);
        _playerNearAreaSizeHalf = new Vector2(PlayerNearAreaSize / 2, PlayerNearAreaSize / 2);
    }

    // Update is called once per frame
    void Update()
    {
        var pos2d = new Vector2(_playerCamera.transform.position.x, _playerCamera.transform.position.z);
        _playerNearArea.position = pos2d - _playerNearAreaSizeHalf;

        _chunk.Update(_playerNearArea);
        _renderer.Render(_chunk);
    }

    private void OnDrawGizmos()
    {
        var p00 = new Vector3(_playerNearArea.x, 0, _playerNearArea.position.y);
        var p01 = new Vector3(_playerNearArea.x, 0, _playerNearArea.position.y + _playerNearArea.height);
        var p11 = new Vector3(_playerNearArea.x + _playerNearArea.width, 0, _playerNearArea.position.y + _playerNearArea.height);
        var p10 = new Vector3(_playerNearArea.x + _playerNearArea.width, 0, _playerNearArea.position.y);

        Gizmos.DrawLine(p00, p01);
        Gizmos.DrawLine(p01, p11);
        Gizmos.DrawLine(p11, p10);
        Gizmos.DrawLine(p10, p00);
    }

    public Vector3 GetGroundPosAtCord(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }
}
