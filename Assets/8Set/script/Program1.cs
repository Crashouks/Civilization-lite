using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class Program1 : MonoBehaviour
{
    // ================= ЦИВІЛІЗАЦІЇ (ОНОВЛЕНО) =================
    [Header("Налаштування Цивілізацій")]
    public Color romeColor = Color.red;
    public Color americaColor = Color.blue;
    public Color egyptColor = Color.yellow;
    public Color scythiaColor = Color.green;

    // Зроблено публічним для доступу з AI систем
    public Color currentCivColor;
    public string currentCivName;

    // Додано для сумісності з DiplomacyManager та логікою війни
    public bool isAtWar = false; 

    void Awake()
    {
        // Зчитуємо вибір з меню. Якщо нічого не вибрано, ставимо Rome
        currentCivName = PlayerPrefs.GetString("SelectedCiv", "Rome");

        if (currentCivName == "Rome") currentCivColor = romeColor;
        else if (currentCivName == "America") currentCivColor = americaColor;
        else if (currentCivName == "Egypt") currentCivColor = egyptColor;
        else if (currentCivName == "Scythia") currentCivColor = scythiaColor;
        else currentCivColor = Color.gray;

        Debug.Log("Гра почалася за: " + currentCivName);
    }

    public void Colorize(GameObject obj)
    {
        // ПЕРЕВІРКА: чи є цей об'єкт юнітом?
        // Ми перевіряємо, чи має об'єкт компонент "Unit"
        if (obj.GetComponent<Unit>() == null)
        {
            // Якщо це не юніт (наприклад, місто), ми виходимо з функції і нічого не фарбуємо
            return;
        }

        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.color = currentCivColor;
            sr.material.SetColor("_Color", currentCivColor);
            sr.material.EnableKeyword("_EMISSION");
            sr.material.SetColor("_EmissionColor", currentCivColor * 0.5f);
        }
    }

    [Header("Налаштування Сітки")]
    public Grid grid;
    public Tilemap tilemap;
    public float cellWidth = 1.0f;
    public float cellHeight = 0.866f;

    [Header("Параметри Карти")]
    public int width = 80;
    public int height = 50;
    public float scale = 10f;
    public float seed;

    [Header("Тайли Біомів")]
    public Tile waterTile;
    public Tile sandTile;
    public Tile grassTile;
    public Tile forestTile;
    public Tile jungleTile;
    public Tile snowTile;
    public Tile winterForestTile;
    public Tile mountainTile;
    public Tile snowMountainTile;

    [Header("Стартові Юніти")]
    public GameObject settlerPrefab;
    public GameObject warriorPrefab;
    public GameObject scoutPrefab;

    [Header("Ліміти Юнітів Гравця")]
    public int maxScouts = 4;
    public int maxWarriors = 20;

    [Header("Візуалізація Шляху")]
    public LineRenderer pathRenderer;

    public Unit selectedUnit;
    private City selectedCityForWar;
    public string pendingWarTargetCiv = "";
    public List<Unit> allUnits = new List<Unit>();
    private bool isMoving = false;
    private bool suppressNextMapClick = false;

    void Start()
    {
        if (grid == null) grid = GetComponentInParent<Grid>();
        if (tilemap == null) tilemap = GetComponentInChildren<Tilemap>();
        if (pathRenderer != null) pathRenderer.enabled = false;

        EnsureScoutPrefab();
        EnsureGameSystems();
        EnsureUIReferences();
        StartCoroutine(LateBindUIReferences());

        ConfigureGrid();

        if (PlayerPrefs.GetInt(SaveManager.LoadOnStartKey, 0) == 1)
        {
            PlayerPrefs.SetInt(SaveManager.LoadOnStartKey, 0);
            PlayerPrefs.Save();
            if (SaveManager.HasSave() && SaveManager.Instance != null)
            {
                SaveManager.Instance.ApplyLoadedGame(this);
                return;
            }
        }

        if (seed == 0) seed = Random.Range(0f, 10000f);
        StartCoroutine(GenerateMapRoutine());
    }

    void EnsureScoutPrefab()
    {
        if (scoutPrefab != null) return;
        scoutPrefab = Resources.Load<GameObject>("Thief");
    }

    void EnsureGameSystems()
    {
        if (Object.FindAnyObjectByType<GameUI>() == null)
            new GameObject("GameUI").AddComponent<GameUI>();
        if (EconomyManager.Instance == null)
            new GameObject("EconomyManager").AddComponent<EconomyManager>();
        if (SaveManager.Instance == null)
            new GameObject("SaveManager").AddComponent<SaveManager>();
        if (FogOfWarManager.Instance == null)
            new GameObject("FogOfWarManager").AddComponent<FogOfWarManager>();
    }

    IEnumerator LateBindUIReferences()
    {
        // Деякі UI об'єкти можуть створюватися пізніше за Program1.Start().
        for (int i = 0; i < 20; i++)
        {
            if (warButton != null && settleButton != null) yield break;

            EnsureUIReferences();
            yield return new WaitForSeconds(0.25f);
        }
    }

    void EnsureUIReferences()
    {
        if (warButton == null)
        {
            warButton = FindObjectByNameInScene("WarButton");
        }

        if (settleButton == null)
        {
            settleButton = FindObjectByNameInScene("SettleButton");
        }
    }

    GameObject FindObjectByNameInScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindChildRecursive(child, objectName);
            if (found != null) return found;
        }

        return null;
    }

    void Update()
    {
        if (isMoving) { ClearPath(); return; }
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) HandleMouseClick();
        if (selectedUnit != null) UpdatePathPreview(); else ClearPath();
    }

    void ConfigureGrid()
    {
        grid.cellLayout = GridLayout.CellLayout.Hexagon;
        grid.cellSize = new Vector3(cellWidth, cellHeight, 0);
        tilemap.tileAnchor = new Vector3(0f, 0f, 0);
    }

    private void HandleMouseClick()
    {
        if (Camera.main == null) return;

        // Після натискання UI-кнопки (Declare war) в цей же кадр може спрацювати клік по мапі.
        // Супресимо 1 наступний мап-клік.
        if (suppressNextMapClick)
        {
            suppressNextMapClick = false;
            return;
        }

        // Якщо клік по UI (наприклад, кнопка війни), не обробляємо клік по карті.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mousePos = Pointer.current.position.ReadValue();
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));

        RaycastHit2D hit = Physics2D.Raycast((Vector2)worldPoint, Vector2.zero);
        if (hit.collider != null)
        {
            Unit clickedUnit = hit.collider.GetComponent<Unit>();
            // Керування тільки юнітами гравця.
            if (clickedUnit != null && clickedUnit.isPlayer) { SelectUnit(clickedUnit); return; }

            // Клік по місту (ворожому) - показуємо кнопку війни.
            City clickedCity = hit.collider.GetComponent<City>();
            if (clickedCity != null)
            {
                OnCityClicked(clickedCity);
                return;
            }
        }

        Vector3Int clickedCell = tilemap.WorldToCell(worldPoint);
        clickedCell.z = 0;

        if (selectedUnit != null && tilemap.HasTile(clickedCell))
        {
            if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsVisible(clickedCell))
                return;

            if (GetUnitAt(clickedCell) == selectedUnit) DeselectUnit();
            else
            {
                List<Vector3Int> path = FindPath(selectedUnit.gridPosition, clickedCell);
                if (path != null && path.Count > 0) StartCoroutine(ExecutePathMovement(selectedUnit, path));
            }
        }
    }

    void UpdatePathPreview()
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
        Vector3Int hoveredCell = tilemap.WorldToCell(worldPoint);
        hoveredCell.z = 0;

        if (tilemap.HasTile(hoveredCell) && hoveredCell != selectedUnit.gridPosition)
        {
            List<Vector3Int> path = FindPath(selectedUnit.gridPosition, hoveredCell);
            if (path != null) { DrawPath(path); return; }
        }
        ClearPath();
    }

    void DrawPath(List<Vector3Int> path)
    {
        if (pathRenderer == null) return;
        pathRenderer.enabled = true;
        pathRenderer.positionCount = path.Count + 1;

        Vector3 startPos = tilemap.GetCellCenterWorld(selectedUnit.gridPosition);
        pathRenderer.SetPosition(0, new Vector3(startPos.x, startPos.y - 1f, -0.2f));

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 pos = tilemap.GetCellCenterWorld(path[i]);
            pathRenderer.SetPosition(i + 1, new Vector3(pos.x, pos.y - 1f, -0.2f));
        }
    }

    void ClearPath() { if (pathRenderer != null) pathRenderer.enabled = false; }
    void SelectUnit(Unit unit) { if (selectedUnit != null) selectedUnit.Deselect(); selectedUnit = unit; selectedUnit.Select(); }
    void DeselectUnit() { if (selectedUnit != null) selectedUnit.Deselect(); selectedUnit = null; }

    public void OnCityClicked(City city)
    {
        if (city == null) return;

        // Якщо клік по власному місту - показуємо панель найму юнітів.
        if (city.isPlayerCity)
        {
            pendingWarTargetCiv = "";
            HideWarButton();
            if (GameUI.Instance != null)
                GameUI.Instance.ShowSpawnPanel(city);
            return;
        }

        selectedCityForWar = city;
        pendingWarTargetCiv = ResolveWarTargetCiv(city);
        ShowWarButton();
    }

    string ResolveWarTargetCiv(City city)
    {
        if (city == null) return "";

        if (!string.IsNullOrEmpty(city.ownerCivName) && city.ownerCivName != "Unknown")
            return city.ownerCivName;

        string cityName = city.name;
        if (cityName.Contains("Rome")) return "Rome";
        if (cityName.Contains("America")) return "America";
        if (cityName.Contains("Egypt")) return "Egypt";
        if (cityName.Contains("Scythia")) return "Scythia";

        // Fallback: беремо найближчий AI-юніт як власника регіону міста.
        Unit nearest = null;
        float bestDistance = float.MaxValue;
        foreach (Unit u in allUnits)
        {
            if (u == null || u.isPlayer) continue;
            float d = Vector3Int.Distance(u.gridPosition, city.gridPosition);
            if (d < bestDistance)
            {
                bestDistance = d;
                nearest = u;
            }
        }

        if (nearest != null)
        {
            string unitName = nearest.name;
            if (unitName.Contains("Rome")) return "Rome";
            if (unitName.Contains("America")) return "America";
            if (unitName.Contains("Egypt")) return "Egypt";
            if (unitName.Contains("Scythia")) return "Scythia";
        }

        return "";
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
    {
        if (IsImpassable(end)) return null;
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        frontier.Enqueue(start);
        Dictionary<Vector3Int, Vector3Int?> cameFrom = new Dictionary<Vector3Int, Vector3Int?>();
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();
            if (current == end) break;
            foreach (Vector3Int next in GetNeighbors(current))
            {
                if (!IsImpassable(next) && !cameFrom.ContainsKey(next)) { frontier.Enqueue(next); cameFrom[next] = current; }
            }
        }
        if (!cameFrom.ContainsKey(end)) return null;
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int temp = end;
        while (temp != start) { path.Add(temp); temp = (Vector3Int)cameFrom[temp]; }
        path.Reverse();
        return path;
    }

    List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        Vector3Int[] directions = (cell.y % 2 == 0)
            ? new Vector3Int[] { new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0) }
            : new Vector3Int[] { new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0) };
        List<Vector3Int> neighbors = new List<Vector3Int>();
        foreach (var dir in directions) neighbors.Add(cell + dir);
        return neighbors;
    }

    public int GetMovementCost(Vector3Int cell)
    {
        TileBase tile = tilemap.GetTile(cell);
        if (tile == forestTile || tile == jungleTile || tile == winterForestTile) return 2;
        return 1;
    }

    public bool IsImpassable(Vector3Int cell)
    {
        TileBase tile = tilemap.GetTile(cell);
        return tile == null || tile == waterTile || tile == mountainTile || tile == snowMountainTile;
    }

    IEnumerator ExecutePathMovement(Unit unit, List<Vector3Int> path)
    {
        isMoving = true; ClearPath();
        yield return StartCoroutine(unit.MoveAlongPath(path, tilemap, this));
        if (unit.currentMovement <= 0) 
        {
            // Показуємо кнопку заснування міста якщо це поселенець
            ShowSettleButton();
            DeselectUnit();
        }
        isMoving = false;
    }

    // ВИПРАВЛЕНО: Додано модифікатор PUBLIC, щоб файл units.cs міг бачити цей метод
    public Unit GetUnitAt(Vector3Int cell) => allUnits.Find(u => u.gridPosition == cell);

    IEnumerator GenerateMapRoutine()
    {
        tilemap.ClearAllTiles();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xOffset = (y % 2 != 0) ? 0.5f : 0f;
                float xCoord = ((float)x + xOffset) / width * scale + seed;
                float yCoord = (float)y / height * scale + seed;
                float h = Mathf.PerlinNoise(xCoord, yCoord);
                float m = Mathf.PerlinNoise(xCoord + 2000f, yCoord + 2000f);
                float dist = Mathf.Abs(y - (height / 2f)) / (height / 2f);
                float t = Mathf.Clamp01(1f - dist + (Mathf.PerlinNoise(xCoord * 0.5f, yCoord * 0.5f) - 0.5f) * 0.2f);
                tilemap.SetTile(new Vector3Int(x, y, 0), GetAdvancedBiome(h, m, t));
            }
        }
        yield return new WaitForEndOfFrame();
        SpawnStartingUnits();
        InitializeFogOfWar();
    }

    void InitializeFogOfWar()
    {
        if (FogOfWarManager.Instance == null) return;
        FogOfWarManager.Instance.Initialize(this);
    }

    public void ClearAllGameObjects()
    {
        foreach (Unit unit in new List<Unit>(allUnits))
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        allUnits.Clear();

        foreach (City city in new List<City>(allCities))
        {
            if (city != null) Destroy(city.gameObject);
        }
        allCities.Clear();

        selectedUnit = null;
        tilemap.ClearAllTiles();
    }

    public IEnumerator RegenerateAndRestore(GameSaveData data)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xOffset = (y % 2 != 0) ? 0.5f : 0f;
                float xCoord = ((float)x + xOffset) / width * scale + seed;
                float yCoord = (float)y / height * scale + seed;
                float h = Mathf.PerlinNoise(xCoord, yCoord);
                float m = Mathf.PerlinNoise(xCoord + 2000f, yCoord + 2000f);
                float dist = Mathf.Abs(y - (height / 2f)) / (height / 2f);
                float t = Mathf.Clamp01(1f - dist + (Mathf.PerlinNoise(xCoord * 0.5f, yCoord * 0.5f) - 0.5f) * 0.2f);
                tilemap.SetTile(new Vector3Int(x, y, 0), GetAdvancedBiome(h, m, t));
            }
        }

        yield return new WaitForEndOfFrame();

        foreach (CitySaveData cityData in data.cities)
        {
            Vector3Int pos = new Vector3Int(cityData.x, cityData.y, 0);
            SpawnCityFromSave(pos, cityData.isPlayerCity, cityData.ownerCivName, cityData.cityName);
        }

        foreach (UnitSaveData unitData in data.units)
        {
            SpawnUnitFromSave(unitData);
        }

        InitializeFogOfWar();
        if (FogOfWarManager.Instance != null && data.exploredFlat != null)
            FogOfWarManager.Instance.ImportExploredFlat(data.exploredFlat);

        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        SaveManager.IsRestoringSave = false;
    }

    public void ApplyCivFromName(string civName)
    {
        currentCivName = civName;
        if (currentCivName == "Rome") currentCivColor = romeColor;
        else if (currentCivName == "America") currentCivColor = americaColor;
        else if (currentCivName == "Egypt") currentCivColor = egyptColor;
        else if (currentCivName == "Scythia") currentCivColor = scythiaColor;
        else currentCivColor = Color.gray;
    }

    void SpawnCityFromSave(Vector3Int pos, bool isPlayerCity, string ownerCiv, string cityName)
    {
        if (cityPrefab == null) return;

        Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
        GameObject cityObj = Instantiate(cityPrefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
        cityObj.name = string.IsNullOrEmpty(cityName) ? ownerCiv + "_City" : cityName;

        City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
        city.gridPosition = pos;
        city.isPlayerCity = isPlayerCity;
        city.ownerCivName = ownerCiv;
        RegisterCity(city);
    }

    void SpawnUnitFromSave(UnitSaveData unitData)
    {
        GameObject prefab = GetPrefabForType(unitData.unitType);
        if (prefab == null) return;

        Vector3Int pos = new Vector3Int(unitData.x, unitData.y, 0);
        string unitName = unitData.isPlayer ? unitData.unitType : unitData.civName + "_" + unitData.unitType;
        Unit unit = CreateUnit(prefab, pos, unitName, unitData.isPlayer, true);
        if (unit == null) return;

        unit.health = unitData.health;
        unit.currentMovement = unitData.currentMovement;

        if (!unitData.isPlayer)
        {
            Color civColor = DiplomacyManager.Instance != null
                ? DiplomacyManager.Instance.GetCivColor(unitData.civName)
                : Color.gray;
            ApplyUnitColor(unit.gameObject, civColor);
            if (unit.GetComponent<UnitAI>() == null)
                unit.gameObject.AddComponent<UnitAI>();
        }
    }

    GameObject GetPrefabForType(string unitType)
    {
        if (unitType == "Scout") return scoutPrefab != null ? scoutPrefab : warriorPrefab;
        if (unitType == "Warrior") return warriorPrefab;
        if (unitType == "Settler") return settlerPrefab;
        return warriorPrefab;
    }

    public void ApplyUnitColor(GameObject obj, Color color)
    {
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.color = color;
        }
    }

    Tile GetAdvancedBiome(float h, float m, float t)
    {
        if (h < 0.32f) return waterTile;
        if (h > 0.82f) return (t < 0.4f) ? snowMountainTile : mountainTile;
        if (t < 0.3f) return (m > 0.55f) ? winterForestTile : snowTile;
        if (t < 0.75f) { if (m < 0.35f) return sandTile; if (m > 0.6f) return forestTile; return grassTile; }
        return (m > 0.65f) ? jungleTile : grassTile;
    }

    public Vector3Int FindValidSpawnPosition(Vector3Int preferredPos)
    {
        if (!IsImpassable(preferredPos)) return preferredPos;

        for (int radius = 1; radius < 20; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int checkPos = preferredPos + new Vector3Int(x, y, 0);
                    if (checkPos.x >= 0 && checkPos.x < width && checkPos.y >= 0 && checkPos.y < height)
                    {
                        if (!IsImpassable(checkPos)) return checkPos;
                    }
                }
            }
        }
        return preferredPos;
    }

    void SpawnStartingUnits()
    {
        Vector3Int center = new Vector3Int(width / 2, height / 2, 0);
        Vector3Int settlerPos = FindValidSpawnPosition(center);
        CreateUnit(settlerPrefab, settlerPos, "Settler", true);

        Vector3Int warriorPos = FindValidSpawnPosition(settlerPos + new Vector3Int(1, 0, 0));
        CreateUnit(warriorPrefab, warriorPos, "Warrior", true);

        if (scoutPrefab != null)
        {
            Vector3Int scoutPos = FindValidSpawnPosition(settlerPos + new Vector3Int(-1, 0, 0));
            CreateUnit(scoutPrefab, scoutPos, "Scout", true);
        }

        Vector3 camPos = tilemap.GetCellCenterWorld(settlerPos);
        Camera.main.transform.position = new Vector3(camPos.x, camPos.y, -10f);
    }

    public int CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind kind)
    {
        return CountUnitsOfKind(true, kind);
    }

    public int CountUnitsOfKind(bool isPlayerUnit, UnitTypeHelper.UnitKind kind)
    {
        int count = 0;
        foreach (Unit u in allUnits)
        {
            if (u != null && u.isPlayer == isPlayerUnit && UnitTypeHelper.GetKind(u) == kind)
                count++;
        }
        return count;
    }

    public bool CanSpawnUnit(bool isPlayerUnit, UnitTypeHelper.UnitKind kind)
    {
        if (!isPlayerUnit) return true;

        if (kind == UnitTypeHelper.UnitKind.Scout && CountPlayerUnitsOfKind(kind) >= maxScouts)
        {
            Debug.Log("Досягнуто ліміт розвідників: " + maxScouts);
            return false;
        }

        if (kind == UnitTypeHelper.UnitKind.Warrior && CountPlayerUnitsOfKind(kind) >= maxWarriors)
        {
            Debug.Log("Досягнуто ліміт воїнів: " + maxWarriors);
            return false;
        }

        return true;
    }

    public bool TrySpawnUnitFromCity(City city, UnitTypeHelper.UnitKind kind)
    {
        if (city == null || !city.isPlayerCity) return false;
        if (!CanSpawnUnit(true, kind)) return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null) return false;

        int cost = economy.GetUnitCost(kind);
        if (!economy.TrySpendCoins(cost, "найм " + UnitTypeHelper.GetTypeName(kind)))
            return false;

        GameObject prefab = GetPrefabForKind(kind);
        if (prefab == null) return false;

        Vector3Int spawnPos = FindSpawnNearCity(city.gridPosition);
        if (spawnPos == Vector3Int.zero && HasCityAt(city.gridPosition))
            spawnPos = FindValidSpawnPosition(city.gridPosition + new Vector3Int(1, 0, 0));

        Unit unit = CreateUnit(prefab, spawnPos, UnitTypeHelper.GetTypeName(kind), true);
        return unit != null;
    }

    GameObject GetPrefabForKind(UnitTypeHelper.UnitKind kind)
    {
        switch (kind)
        {
            case UnitTypeHelper.UnitKind.Scout: return scoutPrefab != null ? scoutPrefab : warriorPrefab;
            case UnitTypeHelper.UnitKind.Warrior: return warriorPrefab;
            case UnitTypeHelper.UnitKind.Settler: return settlerPrefab;
            default: return warriorPrefab;
        }
    }

    Vector3Int FindSpawnNearCity(Vector3Int cityPos)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int pos = cityPos + new Vector3Int(x, y, 0);
                    if (pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height) continue;
                    if (IsImpassable(pos)) continue;
                    if (GetUnitAt(pos) != null) continue;
                    if (HasCityAt(pos)) continue;
                    return pos;
                }
            }
        }
        return Vector3Int.zero;
    }

    public Unit CreateUnit(GameObject prefab, Vector3Int cellPos, string name, bool isPlayerUnit = true, bool skipLimitCheck = false)
    {
        if (prefab == null) return null;

        if (!skipLimitCheck)
        {
            UnitTypeHelper.UnitKind kind = UnitTypeHelper.GetKind(name);
            if (!CanSpawnUnit(isPlayerUnit, kind))
                return null;
        }
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
        GameObject obj = Instantiate(prefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
        obj.name = name;
        Unit u = obj.GetComponent<Unit>() ?? obj.AddComponent<Unit>();
        u.gridPosition = cellPos;
        u.isPlayer = isPlayerUnit;
        ApplyUnitStats(u, name);
        allUnits.Add(u);

        if (isPlayerUnit && FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RevealAround(cellPos, FogOfWarManager.Instance.defaultSightRange);
        
        // Додаємо колайдер якщо його немає
        if (obj.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            
            // Налаштовуємо розмір колайдера
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                collider.size = sr.sprite.bounds.size;
            }
            else
            {
                collider.size = new Vector2(1f, 1f);
            }
        }

        Colorize(obj);
        return u;
    }

    void ApplyUnitStats(Unit unit, string unitName)
    {
        UnitTypeHelper.UnitKind kind = UnitTypeHelper.GetKind(unitName);
        switch (kind)
        {
            case UnitTypeHelper.UnitKind.Scout:
                unit.maxMovement = 4;
                unit.health = 12;
                unit.attackPower = 4;
                break;
            case UnitTypeHelper.UnitKind.Warrior:
                unit.maxMovement = 3;
                unit.health = 20;
                unit.attackPower = 25;
                break;
            case UnitTypeHelper.UnitKind.Settler:
                unit.maxMovement = 3;
                unit.health = 10;
                unit.attackPower = 0;
                break;
        }
        unit.currentMovement = unit.maxMovement;
    }

    public void EndTurn() { foreach (Unit u in allUnits) u.ResetMovement(); }

    // ================= ГОРОДА =================

    [Header("Міста")]
    public GameObject cityPrefab;

    [Header("Інтерфейс")]
    public GameObject warButton; // Пряме посилання на кнопку війни
    public GameObject settleButton; // Кнопка заснування міста

    public List<City> allCities = new List<City>();

    public bool HasCityAt(Vector3Int cell)
    {
        foreach (City c in allCities)
        {
            if (c.gridPosition == cell)
                return true;
        }
        return false;
    }

    public void RegisterCity(City city)
    {
        if (!allCities.Contains(city))
        {
            allCities.Add(city);
            Colorize(city.gameObject);

            if (city.isPlayerCity && FogOfWarManager.Instance != null)
                FogOfWarManager.Instance.RevealAround(city.gridPosition, FogOfWarManager.Instance.citySightRange);
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (allUnits.Contains(unit))
            allUnits.Remove(unit);
    }

    // Метод для показу кнопки заснування міста
    public void ShowSettleButton()
    {
        if (selectedUnit != null && selectedUnit.name.Contains("Settler") && selectedUnit.currentMovement <= 0)
        {
            if (settleButton != null)
            {
                settleButton.SetActive(true);
                Debug.Log("Кнопка заснування міста показана");
            }
            else
            {
                Debug.LogWarning("SettleButton не призначено в інспекторі!");
            }
        }
    }

    // Метод для заснування міста з UI
    public void SettleCity()
    {
        if (selectedUnit != null && selectedUnit.name.Contains("Settler"))
        {
            selectedUnit.CreateCity();

            // Ховаємо кнопку після використання
            if (settleButton != null)
            {
                settleButton.SetActive(false);
            }
        }
    }

    public void ShowWarButton()
    {
        EnsureUIReferences();
        if (warButton != null)
        {
            warButton.SetActive(true);
            Debug.Log("Кнопка війни показана");
        }
        else
        {
            Debug.LogWarning("warButton не призначено в Program1 (Inspector)!");
        }
    }

    public void HideWarButton()
    {
        if (warButton != null)
        {
            warButton.SetActive(false);
        }

        // Додатково ховаємо всі дублікати WarButton у сцені.
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj.name == "WarButton")
            {
                obj.SetActive(false);
            }
        }
    }

    IEnumerator HideWarButtonNextFrame()
    {
        yield return null;
        HideWarButton();
    }

    // Признач цей метод на UI кнопку "Оголосити війну"
    public void DeclareWarOnSelectedCity()
    {
        // Кнопка має зникати одразу після натискання.
        HideWarButton();
        StartCoroutine(HideWarButtonNextFrame());
        suppressNextMapClick = true;

        if (selectedCityForWar == null) return;

        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy == null)
        {
            selectedCityForWar = null;
            return;
        }

        // Оголошуємо війну конкретній цивілізації-власнику міста.
        string targetCiv = !string.IsNullOrEmpty(pendingWarTargetCiv) ? pendingWarTargetCiv : ResolveWarTargetCiv(selectedCityForWar);
        if (string.IsNullOrEmpty(targetCiv))
        {
            Debug.LogWarning("Не вдалося визначити цивілізацію для оголошення війни.");
            selectedCityForWar = null;
            pendingWarTargetCiv = "";
            return;
        }

        Debug.Log("Оголошення війни проти: " + targetCiv);
        diplomacy.DeclareWar(targetCiv);
        isAtWar = true;

        selectedCityForWar = null;
        pendingWarTargetCiv = "";
    }
}