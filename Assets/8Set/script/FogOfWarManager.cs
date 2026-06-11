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
}
