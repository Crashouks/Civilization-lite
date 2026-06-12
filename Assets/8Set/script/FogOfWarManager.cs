using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Дальність огляду")]
    public int defaultSightRange = 3;
    public int scoutSightRange = 4;
    public int citySightRange = 2;

    private bool[,] explored;
    private bool[,] visible;
    private int mapWidth;
    private int mapHeight;

    private Tilemap fogTilemap;
    private Tile unexploredTile;
    private Tile exploredTile;
    private Program1 mapManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(Program1 manager)
    {
        mapManager = manager;
        mapWidth = manager.width;
        mapHeight = manager.height;

        explored = new bool[mapWidth, mapHeight];
        visible = new bool[mapWidth, mapHeight];

        CreateFogOverlay(manager);
        RefreshVisibility();
        UpdateOverlay();
    }

    void CreateFogOverlay(Program1 manager)
    {
        if (manager.grid == null) return;

        GameObject fogObj = new GameObject("FogOfWarTilemap");
        fogObj.transform.SetParent(manager.grid.transform, false);

        fogTilemap = fogObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = fogObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 100;

        unexploredTile = CreateFogTile(new Color(0.02f, 0.02f, 0.05f, 0.95f));
        exploredTile = CreateFogTile(new Color(0.05f, 0.05f, 0.1f, 0.55f));
    }

    Tile CreateFogTile(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = color;
        return tile;
    }

    public void RefreshVisibility()
    {
        if (visible == null || mapManager == null) return;

        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                visible[x, y] = false;

        foreach (Unit unit in mapManager.allUnits)
        {
            if (unit == null || !unit.isPlayer) continue;
            RevealAround(unit.gridPosition, GetSightRange(unit));
        }

        foreach (City city in mapManager.allCities)
        {
            if (city == null || !city.isPlayerCity) continue;
            RevealAround(city.gridPosition, citySightRange);
        }

        UpdateOverlay();
        UpdateUnitVisibility();
    }

    public void RevealAround(Vector3Int center, int range)
    {
        if (explored == null) return;

        for (int x = center.x - range; x <= center.x + range; x++)
        {
            for (int y = center.y - range; y <= center.y + range; y++)
            {
                if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight) continue;
                if (HexDistance(center, new Vector3Int(x, y, 0)) > range) continue;

                explored[x, y] = true;
                visible[x, y] = true;
            }
        }

        UpdateOverlay();
        UpdateUnitVisibility();
    }

    int GetSightRange(Unit unit)
    {
        if (UnitTypeHelper.GetKind(unit) == UnitTypeHelper.UnitKind.Scout)
            return scoutSightRange;
        return defaultSightRange;
    }

    int HexDistance(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    public bool IsVisible(Vector3Int cell)
    {
        if (visible == null) return true;
        if (cell.x < 0 || cell.y < 0 || cell.x >= mapWidth || cell.y >= mapHeight) return false;
        return visible[cell.x, cell.y];
    }

    public bool IsExplored(Vector3Int cell)
    {
        if (explored == null) return true;
        if (cell.x < 0 || cell.y < 0 || cell.x >= mapWidth || cell.y >= mapHeight) return false;
        return explored[cell.x, cell.y];
    }

    void UpdateOverlay()
    {
        if (fogTilemap == null || explored == null) return;

        fogTilemap.ClearAllTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!mapManager.tilemap.HasTile(cell)) continue;

                if (!explored[x, y])
                    fogTilemap.SetTile(cell, unexploredTile);
                else if (!visible[x, y])
                    fogTilemap.SetTile(cell, exploredTile);
            }
        }
    }

    void UpdateUnitVisibility()
    {
        if (mapManager == null) return;

        foreach (Unit unit in mapManager.allUnits)
        {
            if (unit == null) continue;

            bool show = unit.isPlayer || IsVisible(unit.gridPosition);
            SetUnitVisible(unit, show);
        }

        foreach (City city in mapManager.allCities)
        {
            if (city == null) continue;

            bool show = city.isPlayerCity || IsVisible(city.gridPosition);
            SetObjectVisible(city.gameObject, show);
        }
    }

    void SetUnitVisible(Unit unit, bool visibleToPlayer)
    {
        SetObjectVisible(unit.gameObject, visibleToPlayer);
    }

    void SetObjectVisible(GameObject obj, bool visibleToPlayer)
    {
        if (obj == null) return;

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.enabled = visibleToPlayer;

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
            col.enabled = visibleToPlayer;
    }

    public bool[] ExportExploredFlat()
    {
        bool[] flat = new bool[mapWidth * mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                flat[x + y * mapWidth] = explored[x, y];
        return flat;
    }

    public void ImportExploredFlat(bool[] flat)
    {
        if (flat == null || explored == null) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = x + y * mapWidth;
                if (index < flat.Length)
                    explored[x, y] = flat[index];
            }
        }

        RefreshVisibility();
    }
}
