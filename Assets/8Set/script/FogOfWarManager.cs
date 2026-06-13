using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Налаштування")]
    public bool enableFogOfWar = true;
    public int warriorSightRange = 1;
    public int scoutSightRange = 2;
    public int citySightRange = 2;

    bool[,] explored;
    bool[,] visible;
    int mapWidth;
    int mapHeight;
    byte[,] overlayState;
    const byte OverlayNone = 255;
    const byte OverlayHidden = 0;
    const byte OverlayShrouded = 1;
    const byte OverlayVisible = 2;

    Tilemap fogTilemap;
    Tile unexploredTile;
    Tile exploredTile;
    Program1 program;
    static readonly Color HiddenTerrainColor = Color.black;
    static readonly Color ShroudedTerrainColor = new Color(0.14f, 0.14f, 0.16f, 1f);

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
        if (manager == null)
            return;

        enableFogOfWar = true;
        warriorSightRange = Mathf.Max(1, warriorSightRange);
        scoutSightRange = Mathf.Max(warriorSightRange + 1, scoutSightRange);

        mapWidth = manager.width;
        mapHeight = manager.height;
        explored = new bool[mapWidth, mapHeight];
        visible = new bool[mapWidth, mapHeight];
        overlayState = new byte[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                overlayState[x, y] = OverlayNone;

        if (!enableFogOfWar)
            return;

        CreateFogOverlay(manager);
        RefreshVisibility();
    }

    void CreateFogOverlay(Program1 manager)
    {
        if (manager.grid == null || manager.tilemap == null)
            return;

        if (fogTilemap != null)
            Destroy(fogTilemap.gameObject);

        GameObject fogObj = new GameObject("FogOfWarTilemap");
        fogObj.transform.SetParent(manager.grid.transform, false);

        fogTilemap = fogObj.AddComponent<Tilemap>();
        fogTilemap.tileAnchor = manager.tilemap.tileAnchor;
        fogTilemap.orientation = manager.tilemap.orientation;

        TilemapRenderer terrainRenderer = manager.tilemap.GetComponent<TilemapRenderer>();
        TilemapRenderer renderer = fogObj.AddComponent<TilemapRenderer>();
        int terrainOrder = terrainRenderer != null ? terrainRenderer.sortingOrder : 0;
        renderer.sortingOrder = terrainOrder + 6;
        renderer.sortingLayerID = terrainRenderer != null ? terrainRenderer.sortingLayerID : 0;
        if (terrainRenderer != null && terrainRenderer.sharedMaterial != null)
            renderer.sharedMaterial = terrainRenderer.sharedMaterial;

        Tile referenceTile = manager.GetFogReferenceTile();
        unexploredTile = CreateHexFogTile(referenceTile, Color.black);
        exploredTile = CreateHexFogTile(referenceTile, new Color(0.06f, 0.06f, 0.08f, 0.92f));
    }

    Tile CreateHexFogTile(Tile referenceTile, Color tint)
    {
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.colliderType = Tile.ColliderType.None;
        tile.color = tint;

        if (referenceTile != null && referenceTile.sprite != null)
        {
            tile.sprite = referenceTile.sprite;
            tile.transform = referenceTile.transform;
            return tile;
        }

        tile.sprite = CreateFallbackHexSprite(new Color(tint.r, tint.g, tint.b, 1f));
        return tile;
    }

    Sprite CreateFallbackHexSprite(Color color, float radiusScale = 0.52f)
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * radiusScale;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsidePointyHex(x + 0.5f, y + 0.5f, center, radius);
                tex.SetPixel(x, y, inside ? color : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool IsInsidePointyHex(float x, float y, Vector2 center, float radius)
    {
        float dx = Mathf.Abs(x - center.x) / radius;
        float dy = Mathf.Abs(y - center.y) / radius;
        return dy <= 0.866f && dy <= 0.866f - dx * 0.5f && dx <= 1f;
    }

    public void RevealAllPlayerUnits()
    {
        RefreshVisibility();
    }

    public void OnUnitMoved(Unit unit)
    {
        if (!enableFogOfWar || unit == null)
            return;

        if (program == null)
            program = Object.FindAnyObjectByType<Program1>();

        if (program == null)
            return;

        if (unit.isPlayer)
        {
            Vector3Int cell = unit.gridPosition;
            cell.z = 0;
            MarkExploredAround(cell, GetSightRange(unit));
            RefreshVisibility();
            return;
        }

        if (enableFogOfWar)
            UpdateEntityVisibility();
    }

    void MarkExploredAround(Vector3Int center, int range)
    {
        if (explored == null || program == null)
            return;

        center.z = 0;
        if (!program.IsValidMapCell(center))
            center = program.ResolveMapCell(center, program.tilemap.GetCellCenterWorld(center));

        foreach (Vector3Int cell in GetCellsInRange(center, range))
            SetCellExplored(cell);
    }

    void SetCellExplored(Vector3Int cell)
    {
        if (!IsInBounds(cell))
            return;

        explored[cell.x, cell.y] = true;
    }

    public void RefreshVisibility()
    {
        if (program == null)
            program = Object.FindAnyObjectByType<Program1>();
        if (visible == null || explored == null || program == null)
            return;

        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                visible[x, y] = false;

        foreach (Unit unit in program.allUnits)
        {
            if (unit == null || !unit.isPlayer)
                continue;

            Vector3Int cell = unit.gridPosition;
            cell.z = 0;
            MarkVisibleAround(cell, GetSightRange(unit));
        }

        foreach (City city in program.allCities)
        {
            if (city == null || !city.IsOwnedByPlayer())
                continue;

            Vector3Int cell = city.gridPosition;
            cell.z = 0;
            MarkVisibleAround(cell, citySightRange);
        }

        if (enableFogOfWar)
        {
            UpdateOverlay();
            UpdateEntityVisibility();
        }
    }

    public void RevealAround(Vector3Int center, int range)
    {
        if (explored == null || visible == null)
            return;

        MarkVisibleAround(center, range);

        if (enableFogOfWar)
        {
            UpdateOverlay();
            UpdateEntityVisibility();
        }
    }

    void MarkVisibleAround(Vector3Int center, int range)
    {
        if (explored == null || visible == null || program == null)
            return;

        center.z = 0;
        if (!program.IsValidMapCell(center))
            center = program.ResolveMapCell(center, program.tilemap.GetCellCenterWorld(center));

        foreach (Vector3Int cell in GetCellsInRange(center, range))
            SetCellExploredAndVisible(cell);
    }

    void SetCellExploredAndVisible(Vector3Int cell)
    {
        if (!IsInBounds(cell))
            return;

        explored[cell.x, cell.y] = true;
        visible[cell.x, cell.y] = true;
    }

    IEnumerable<Vector3Int> GetCellsInRange(Vector3Int center, int range)
    {
        if (program == null || range < 0)
            yield break;

        center.z = 0;
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<(Vector3Int cell, int dist)>();
        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            (Vector3Int cell, int dist) item = queue.Dequeue();

            if (program.IsValidMapCell(item.cell))
                yield return item.cell;

            if (item.dist >= range)
                continue;

            foreach (Vector3Int neighbor in program.GetNeighborCells(item.cell))
            {
                Vector3Int next = neighbor;
                next.z = 0;
                if (visited.Contains(next))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, item.dist + 1));
            }
        }
    }

    bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < mapWidth && cell.y < mapHeight;
    }

    int GetSightRange(Unit unit)
    {
        if (unit == null)
            return warriorSightRange;

        UnitTypeHelper.UnitKind kind = UnitTypeHelper.GetKind(unit);
        if (kind == UnitTypeHelper.UnitKind.Scout || kind == UnitTypeHelper.UnitKind.Settler)
            return scoutSightRange;

        return warriorSightRange;
    }

    public void OnCityRegistered(City city)
    {
        if (!enableFogOfWar || city == null || program == null || !city.IsOwnedByPlayer())
            return;

        Vector3Int cell = program.ResolveCityCell(city);
        MarkVisibleAround(cell, citySightRange);
        RefreshVisibility();
    }

    public bool IsVisible(Vector3Int cell)
    {
        if (!enableFogOfWar || visible == null)
            return true;
        if (!IsInBounds(cell))
            return false;
        return visible[cell.x, cell.y];
    }

    public bool IsExplored(Vector3Int cell)
    {
        if (!enableFogOfWar || explored == null)
            return true;
        if (!IsInBounds(cell))
            return false;
        return explored[cell.x, cell.y];
    }

    void UpdateOverlay()
    {
        if (fogTilemap == null || explored == null || program == null || program.tilemap == null || overlayState == null)
            return;

        bool fogTilesDirty = false;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!program.tilemap.HasTile(cell))
                    continue;

                byte state = !explored[x, y] ? OverlayHidden
                    : !visible[x, y] ? OverlayShrouded
                    : OverlayVisible;

                if (overlayState[x, y] == state)
                    continue;

                overlayState[x, y] = state;

                if (state == OverlayHidden)
                {
                    program.tilemap.SetColor(cell, HiddenTerrainColor);
                    fogTilemap.SetTile(cell, unexploredTile);
                }
                else if (state == OverlayShrouded)
                {
                    program.tilemap.SetColor(cell, ShroudedTerrainColor);
                    fogTilemap.SetTile(cell, exploredTile);
                }
                else
                {
                    program.tilemap.SetColor(cell, Color.white);
                    fogTilemap.SetTile(cell, null);
                }

                fogTilesDirty = true;
            }
        }

        if (fogTilesDirty)
            fogTilemap.RefreshAllTiles();
    }

    void UpdateEntityVisibility()
    {
        if (program == null)
            return;

        foreach (Unit unit in program.allUnits)
        {
            if (unit == null)
                continue;

            Vector3Int cell = unit.gridPosition;
            cell.z = 0;
            if (unit.isPlayer)
            {
                unit.SetFogVisibility(true);
                continue;
            }

            unit.SetFogVisibility(IsVisible(cell));
        }

        foreach (City city in program.allCities)
        {
            if (city == null)
                continue;

            Vector3Int cell = city.gridPosition;
            cell.z = 0;
            bool show = city.IsOwnedByPlayer() || IsVisible(cell);
            city.SetFogVisibility(show);
        }
    }

    public bool[] ExportExploredFlat()
    {
        if (explored == null)
            return null;

        bool[] flat = new bool[mapWidth * mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                flat[x + y * mapWidth] = explored[x, y];
        return flat;
    }

    public void ImportExploredFlat(bool[] flat)
    {
        if (flat == null || explored == null)
            return;

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
