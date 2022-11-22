using UnityEditor;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class BiomePresetConfigurator : MonoBehaviour
{
	[Range(1, 5)]
	public int TileCount = 1;

	public TerrainGenerator TerrainGenerator;

	public RequiredTileStatePreset TileStatePreset;
	public GameObject TilePrefab;

	public bool EditorAutoUpdateMesh;
	public BuildingFootprint EditorBuilding;

	BiomeLayerData _biomeData;

	Vector2 _lastGeneratedPos = new Vector2(float.MaxValue, float.MaxValue);

	int _width;
	TerrainTileDisplay[] _generatedTiles = new TerrainTileDisplay[0];

	private void DrawTerrainInEditor()
	{
		TerrainGenerator.SetupGenerator();
		_biomeData = TerrainGenerator.GetBiomeLayerData();

		_width = TileCount * 2 + 1;

		if (transform.childCount > 0)
        {
			Debug.Log($"Child found:{transform.childCount}");

			_generatedTiles = new TerrainTileDisplay[transform.childCount];

			for (int i = 0; i < transform.childCount; i++)
            {
				_generatedTiles[i] = transform.GetChild(i).GetComponent<TerrainTileDisplay>();
			}
		}

		if (_generatedTiles == null || _generatedTiles.Length != _width)
		{
			if(_generatedTiles != null && _generatedTiles.Length > 0) 
			{ 
				foreach (var tile in _generatedTiles)
				{
                    try { 
						DestroyImmediate(tile.gameObject);
					}catch(MissingReferenceException){}
				}
			}

			_generatedTiles = new TerrainTileDisplay[_width * _width];

			for (int y = -TileCount; y <= TileCount; y++)
			{
				for (int x = -TileCount; x <= TileCount; x++)
				{
					var pos = (transform.position.To2D() + new Vector2(x * TerrainGenerator.TilePhysicalSize, y * TerrainGenerator.TilePhysicalSize)).To3D();
					var obj = Instantiate(TilePrefab, pos.To2D(), Quaternion.identity, transform);
					var tile = obj.GetComponent<TerrainTileDisplay>();
					_generatedTiles[(y + TileCount) * _width + (x + TileCount)] = tile;
				}
			}
		}

		for (int y = -TileCount, yi=0 ; y <= TileCount; y++, yi++)
		{
			for (int x = -TileCount, xi=0; x <= TileCount; x++, xi++)
			{
				TerrainTileDisplay[] neighbours = null;

				if(yi > 0 && xi > 0 && yi<_width-1 && xi < _width - 1)
                {
					neighbours = new TerrainTileDisplay[4];
					neighbours[0] = _generatedTiles[yi * _width + (xi-1)];
					neighbours[1] = _generatedTiles[(yi+1) * _width + xi];
					neighbours[2] = _generatedTiles[yi * _width + (xi+1)];
					neighbours[3] = _generatedTiles[(yi-1) * _width + xi];
				}

				var tile = _generatedTiles[yi * _width + xi];
				tile.transform.position = (transform.position.To2D() + new Vector2(x * TerrainGenerator.TilePhysicalSize, y * TerrainGenerator.TilePhysicalSize)).To3D();
				UpdateEditorDisplay(tile, neighbours);
			}
		}
	}
	private void UpdateEditorDisplay(TerrainTileDisplay tileDisplay, TerrainTileDisplay[] neighbours = null)
	{
		if (tileDisplay == null) return;

		var tile = TerrainGenerator.CreateEmptyTile(tileDisplay.transform.position.To2D() - TerrainGenerator.TilePhysicalSizeHalfVect);

		TerrainGenerator.GenerateBiomeMap(tile);
		TerrainGenerator.GenerateHeightMap(tile);
		TerrainGenerator.BlendHeightMap(tile);

		if (EditorBuilding != null)
		{
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			tile.FlatHeightMap(flatArea, EditorBuilding.transform.position.y);
		}

		TerrainGenerator.GenerateAllMeshData(tile);

		if (neighbours != null && neighbours.Length == 4)
		{
			tile.MeshDatas[TileStatePreset.DisplayLOD].AdjustMeshToNeighboursLOD(neighbours.Select(n => TileStatePreset.DisplayLOD).ToArray());
		}

		tileDisplay.Display(tile, TileStatePreset, _biomeData);
	}

	public void UpdateEditor()
	{
		DrawTerrainInEditor();
	}

	void Update()
	{
		if (EditorAutoUpdateMesh)
		{
			var pos = transform.position.To2D();

			if (Vector2.Distance(pos, _lastGeneratedPos) > 10)
			{
				_lastGeneratedPos = pos;
				DrawTerrainInEditor();
			}
		}
	}
}
