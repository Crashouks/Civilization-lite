using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Налаштування видимості")]
    public int visionRadius = 2;
    public bool enableFogOfWar = false; // Тимчасово вимкнено затемнення карти

    Tilemap fogTilemap;
    Tile unexploredTile;
    Program1 program;

    readonly HashSet<Vector3Int> explored = new HashSet<Vector3Int>();

    public void Initialize(Program1 manager)
    {
        if (!enableFogOfWar) return; // Пропускаємо ініціалізацію якщо затемнення вимкнено

        program = manager;
        if (program == null || program.tilemap == null) return;

        Grid grid = program.grid != null ? program.grid : program.tilemap.layoutGrid;
        if (grid == null) return;

        GameObject fogObj = new GameObject("FogOfWar");
        fogObj.transform.SetParent(grid.transform, false);

        fogTilemap = fogObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = fogObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 20;

        unexploredTile = CreateFogTile(new Color(0.02f, 0.02f, 0.05f, 1f));

        CoverMap();
        RevealAllPlayerUnits();
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
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        return tile;
    }

    void CoverMap()
    {
        if (fogTilemap == null || program == null) return;

        for (int x = 0; x < program.width; x++)
        {
            for (int y = 0; y < program.height; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                fogTilemap.SetTile(cell, unexploredTile);
            }
        }
    }

    public void RevealAllPlayerUnits()
    {
        if (program == null) return;

        foreach (Unit unit in program.allUnits)
        {
            if (unit != null && unit.isPlayer)
                RevealAroundCell(unit.gridPosition);
        }

        RefreshFogVisuals();
        UpdateEntityVisibility();
    }

    public void OnUnitMoved(Unit unit)
    {
        if (!enableFogOfWar || program == null) return; // Перевірка якщо туман вимкнено або program не ініціалізовано
        if (unit == null || !unit.isPlayer) return;

        RevealAroundCell(unit.gridPosition);
        RefreshFogVisuals();
        UpdateEntityVisibility();
    }

    void RevealAroundCell(Vector3Int center)
    {
        if (program == null) return; // Перевірка на null
        foreach (Vector3Int cell in GetCellsInRadius(center, visionRadius))
            explored.Add(cell);
    }

    void RefreshFogVisuals()
    {
        if (fogTilemap == null || program == null) return;

        for (int x = 0; x < program.width; x++)
        {
            for (int y = 0; y < program.height; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                if (explored.Contains(cell))
                    fogTilemap.SetTile(cell, null);
                else
                    fogTilemap.SetTile(cell, unexploredTile);
            }
        }
    }

    void UpdateEntityVisibility()
    {
        if (program == null) return;

        foreach (City city in program.allCities)
        {
            if (city == null) continue;
            bool show = city.isPlayerCity || IsExplored(city.gridPosition);
            city.SetFogVisibility(show);
        }
    }

    IEnumerable<Vector3Int> GetCellsInRadius(Vector3Int center, int radius)
    {
        if (program == null) yield break; // Перевірка на null

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell = center + new Vector3Int(x, y, 0);
                if (cell.x < 0 || cell.y < 0 || cell.x >= program.width || cell.y >= program.height)
                    continue;

                if (Vector3Int.Distance(center, cell) <= radius + 0.5f)
                    yield return cell;
            }
        }
    }

    public bool IsExplored(Vector3Int cell) => explored.Contains(cell);
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
