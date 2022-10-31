using UnityEngine;

public class MyTerrainManager : MonoBehaviour
{
    public float TileSize = 10;
    public int Level = 2;
    public ViewDistancePreset[] ViewDistanceForLevels;

    [ReadOnly] public float ChunkSize;

    private MyTerrainChunk _chunk;
    private MyTerrainRenderer _renderer;
    private Camera _playerCamera;

    private ViewDistnaceHandler _viewDistnaceHandler;

    // Start is called before the first frame update
    void Start()
    {
        ChunkSize = TileSize * Mathf.Pow(2, Level);

        _chunk = new MyTerrainChunk(Level, new Rect(ChunkSize / -2, ChunkSize / -2, ChunkSize, ChunkSize));
        _renderer = GetComponent<MyTerrainRenderer>();
        _playerCamera = Camera.main;
        _viewDistnaceHandler = new ViewDistnaceHandler(ViewDistanceForLevels);
    }

    // Update is called once per frame
    void Update()
    {
        var pos2d = new Vector2(_playerCamera.transform.position.x, _playerCamera.transform.position.z);

        _viewDistnaceHandler.ChangeCenter(pos2d);

        _chunk.Update(_viewDistnaceHandler);
        _renderer.Render(_chunk);
    }

    private void OnDrawGizmos()
    {
        if (_viewDistnaceHandler == null) return;

        _viewDistnaceHandler.OnDrawGizmos();
    }

    public Vector3 GetGroundPosAtCord(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }
}