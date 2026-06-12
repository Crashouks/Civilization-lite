using UnityEngine;
using UnityEngine.Tilemaps;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Налаштування")]
    public bool enableFogOfWar = false;
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
    private Program1 program;

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
        program = manager;
        if (manager == null) return;

        mapWidth = manager.width;
        mapHeight = manager.height;
        explored = new bool[mapWidth, mapHeight];
        visible = new bool[mapWidth, mapHeight];

        if (!enableFogOfWar) return;

        CreateFogOverlay(manager);
        RefreshVisibility();
    }

    void CreateFogOverlay(Program1 manager)
    {
        if (manager.grid == null) return;

        if (fogTilemap != null)
            Destroy(fogTilemap.gameObject);

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

    public void RevealAllPlayerUnits()
    {
        if (program == null) program = Object.FindAnyObjectByType<Program1>();
        if (program == null) return;

        foreach (Unit unit in program.allUnits)
        {
            if (unit != null && unit.isPlayer)
                RevealAround(unit.gridPosition, GetSightRange(unit));
        }

        foreach (City city in program.allCities)
        {
            if (city != null && city.isPlayerCity)
                RevealAround(city.gridPosition, citySightRange);
        }

        if (enableFogOfWar)
        {
            UpdateOverlay();
            UpdateEntityVisibility();
        }
    }

    public void OnUnitMoved(Unit unit)
    {
        if (!enableFogOfWar || unit == null || !unit.isPlayer) return;
        RevealAround(unit.gridPosition, GetSightRange(unit));
    }

    public void RefreshVisibility()
    {
        if (program == null) program = Object.FindAnyObjectByType<Program1>();
        if (visible == null || program == null) return;

        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                visible[x, y] = false;

        foreach (Unit unit in program.allUnits)
        {
            if (unit == null || !unit.isPlayer) continue;
            RevealAround(unit.gridPosition, GetSightRange(unit));
        }

        foreach (City city in program.allCities)
        {
            if (city == null || !city.isPlayerCity) continue;
            RevealAround(city.gridPosition, citySightRange);
        }

        if (enableFogOfWar)
        {
            UpdateOverlay();
            UpdateEntityVisibility();
        }
    }

    public void RevealAround(Vector3Int center, int range)
    {
        if (explored == null || visible == null) return;

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

        if (enableFogOfWar)
        {
            UpdateOverlay();
            UpdateEntityVisibility();
        }
    }

    int GetSightRange(Unit unit)
    {
        if (unit != null && UnitTypeHelper.GetKind(unit) == UnitTypeHelper.UnitKind.Scout)
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
        if (!enableFogOfWar || visible == null) return true;
        if (cell.x < 0 || cell.y < 0 || cell.x >= mapWidth || cell.y >= mapHeight) return false;
        return visible[cell.x, cell.y];
    }

    public bool IsExplored(Vector3Int cell)
    {
        if (!enableFogOfWar || explored == null) return true;
        if (cell.x < 0 || cell.y < 0 || cell.x >= mapWidth || cell.y >= mapHeight) return false;
        return explored[cell.x, cell.y];
    }

    void UpdateOverlay()
    {
        if (fogTilemap == null || explored == null || program == null) return;

        fogTilemap.ClearAllTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!program.tilemap.HasTile(cell)) continue;

                if (!explored[x, y])
                    fogTilemap.SetTile(cell, unexploredTile);
                else if (!visible[x, y])
                    fogTilemap.SetTile(cell, exploredTile);
            }
        }
    }

    void UpdateEntityVisibility()
    {
        if (program == null) return;

        foreach (Unit unit in program.allUnits)
        {
            if (unit == null) continue;
            bool show = unit.isPlayer || IsVisible(unit.gridPosition);
            SetObjectVisible(unit.gameObject, show);
        }

        foreach (City city in program.allCities)
        {
            if (city == null) continue;
            bool show = city.isPlayerCity || IsVisible(city.gridPosition);
            city.SetFogVisibility(show);
        }
    }

    void SetObjectVisible(GameObject obj, bool visibleToPlayer)
    {
        if (obj == null) return;

        foreach (SpriteRenderer sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = visibleToPlayer;

        foreach (Collider2D col in obj.GetComponentsInChildren<Collider2D>(true))
            col.enabled = visibleToPlayer;
    }

    public bool[] ExportExploredFlat()
    {
        if (explored == null) return null;

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
