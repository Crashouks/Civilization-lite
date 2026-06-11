using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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

    public Color GetCivColor(string civName)
    {
        if (civName == "Rome") return romeColor;
        if (civName == "America") return americaColor;
        if (civName == "Egypt") return egyptColor;
        if (civName == "Scythia") return scythiaColor;
        return Color.gray;
    }

    public void Colorize(GameObject obj)
    {
        if (obj.GetComponent<Unit>() == null) return;

        foreach (SpriteRenderer sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == null) continue;

            // Фарбуємо тільки одяг (body, clothes, armor, tunic, cape, etc.)
            string lowerName = sr.name.ToLower();
            bool isClothing = lowerName.Contains("body") || lowerName.Contains("clothes") ||
                            lowerName.Contains("armor") || lowerName.Contains("tunic") ||
                            lowerName.Contains("cape") || lowerName.Contains("robe") ||
                            lowerName.Contains("cloth") || lowerName.Contains("outfit");

            if (isClothing)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
                sr.color = currentCivColor;
                sr.material.SetColor("_Color", currentCivColor);
                sr.material.EnableKeyword("_EMISSION");
                sr.material.SetColor("_EmissionColor", currentCivColor * 0.5f);
            }
        }

        UnitSetup.ApplySorting(obj.transform);
    }

    [Header("Налаштування Сітки")]
    public Grid grid;
    public Tilemap tilemap;
    public float cellWidth = 1.0f;
    public float cellHeight = 0.866f;

    [Header("Параметри Карти")]
    public int width = 40;
    public int height = 30;
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

    [Header("Візуалізація Шляху")]
    public LineRenderer pathRenderer;

    public Unit selectedUnit;
    public City selectedCityForWar;
    public string pendingWarTargetCiv = "";
    public List<Unit> allUnits = new List<Unit>();
    private bool isMoving = false;
    private bool suppressNextMapClick = false;

    void Start()
    {
        if (grid == null) grid = GetComponentInParent<Grid>();
        if (tilemap == null) tilemap = GetComponentInChildren<Tilemap>();
        if (pathRenderer != null) pathRenderer.enabled = false;

        EnsureUIReferences();
        StartCoroutine(LateBindUIReferences());

        ConfigureGrid();
        if (seed == 0) seed = Random.Range(0f, 10000f);
        StartCoroutine(GenerateMapRoutine());
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

        if (nextTurnButton == null)
        {
            nextTurnButton = FindObjectByNameInScene("NextTurnButton");
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

        // Ховаємо панель інформації про місто при кліку на порожній тайл
        CityInfoPanel panel = Object.FindAnyObjectByType<CityInfoPanel>();
        if (panel != null)
        {
            panel.HidePanel();
        }

        if (selectedUnit != null && tilemap.HasTile(clickedCell))
        {
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

        // Якщо клік по власному місті - показуємо інформацію але без кнопок війни
        if (city.isPlayerCity)
        {
            ShowCityInfoPanel(city);
            return;
        }

        // Для ворожих міст показуємо панель з кнопками
        ShowCityInfoPanel(city);
    }

    void ShowCityInfoPanel(City city)
    {
        Debug.Log("ShowCityInfoPanel викликано для міста: " + city.cityName);

        CityInfoPanel panel = Object.FindAnyObjectByType<CityInfoPanel>();
        if (panel == null)
        {
            Debug.Log("CityInfoPanel не знайдено, створюємо новий");
            // Якщо панель не існує, створюємо її
            GameObject panelObj = new GameObject("CityInfoPanel");
            panel = panelObj.AddComponent<CityInfoPanel>();
        }

        if (panel != null)
        {
            Debug.Log("Викликаємо panel.ShowPanel для " + city.cityName);
            panel.ShowPanel(city);
        }
        else
        {
            Debug.LogError("Не вдалося створити CityInfoPanel!");
        }
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
        isMoving = true;
        ClearPath();

        UnitAnimator anim = unit.GetComponent<UnitAnimator>();
        if (path.Count > 0 && anim != null)
            anim.PlayWalk();

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

        fogOfWar = GetComponent<FogOfWarManager>() ?? gameObject.AddComponent<FogOfWarManager>();
        fogOfWar.Initialize(this);

        SpawnStartingUnits();
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

        Vector3 camPos = tilemap.GetCellCenterWorld(settlerPos);
        Camera.main.transform.position = new Vector3(camPos.x, camPos.y, -10f);

        if (fogOfWar != null)
            fogOfWar.RevealAllPlayerUnits();
    }

    // ВИПРАВЛЕНО: Додано модифікатор PUBLIC, щоб файл DiplomacyManager міг бачити цей метод
    public Unit CreateUnit(GameObject prefab, Vector3Int cellPos, string name, bool isPlayerUnit = true)
    {
        if (prefab == null) return null;
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
        GameObject obj = Instantiate(prefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
        UnitSetup.Configure(obj, name, isPlayerUnit);
        Unit u = obj.GetComponent<Unit>();
        u.gridPosition = cellPos;
        allUnits.Add(u);
        
        if (obj.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                collider.size = sr.sprite.bounds.size;
            else
                collider.size = new Vector2(1f, 1f);
        }

        Colorize(obj);
        return u;
    }

    public void EndTurn()
    {
        foreach (Unit u in allUnits) u.ResetMovement();
        if (fogOfWar != null) fogOfWar.RevealAllPlayerUnits();
    }

    // ================= ГОРОДА =================

    [Header("Міста")]
    public GameObject cityPrefab;

    [Header("Інтерфейс")]
    public GameObject warButton; // Пряме посилання на кнопку війни
    public GameObject settleButton; // Кнопка заснування міста
    public GameObject nextTurnButton; // Кнопка наступного ходу

    public List<City> allCities = new List<City>();

    FogOfWarManager fogOfWar;

    public FogOfWarManager GetFogOfWar() => fogOfWar;

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
            // Фарбуємо місто при реєстрації
            Colorize(city.gameObject);
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

    // Метод для наступного ходу
    public void NextTurn()
    {
        Debug.Log("=== НАСТУПНИЙ ХІД ===");

        // Скидаємо рух для всіх юнітів (гравця і AI)
        int unitsReset = 0;
        foreach (Unit unit in allUnits)
        {
            if (unit != null)
            {
                unit.currentMovement = 3; // Всі юніти отримують 3 ходи
                unitsReset++;
            }
        }
        Debug.Log($"Скинуто рух для {unitsReset} юнітів (по 3 ходи кожен)");

        // Виконуємо хід для всіх AI цивілізацій послідовно
        CivilizationAI[] aiCivs = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        Debug.Log($"Знайдено {aiCivs.Length} AI цивілізацій");

        if (aiCivs.Length == 0)
        {
            Debug.LogWarning("AI цивілізації не знайдено! Перевірте чи існують об'єкти CivilizationAI на сцені.");
        }

        // Запускаємо послідовні ходи AI
        StartCoroutine(ExecuteAITurnsSequentially(aiCivs));

        // Скидаємо вибір юніта
        DeselectUnit();
        ClearPath();
    }

    IEnumerator ExecuteAITurnsSequentially(CivilizationAI[] aiCivs)
    {
        foreach (CivilizationAI ai in aiCivs)
        {
            if (ai != null && ai.civilizationName != "AI Civilization")
            {
                Debug.Log($"Запускаємо хід для {ai.civilizationName}");
                ai.ExecuteAITurn();

                // Чекаємо поки цивілізація завершить свій хід
                while (ai.IsProcessingTurn())
                {
                    yield return null;
                }

                Debug.Log($"{ai.civilizationName} завершив хід");
            }
            else if (ai != null && ai.civilizationName == "AI Civilization")
            {
                Debug.LogWarning($"Пропускаємо CivilizationAI з назвою за замовчуванням: {ai.gameObject.name}");
            }
        }

        Debug.Log("=== ВСІ AI ЦИВІЛІЗАЦІЇ ЗАВЕРШИЛИ ХІД ===");
    }

    public void ShowWarButton()
    {
        EnsureUIReferences();
        if (warButton != null)
        {
            UnityEngine.UI.Button btn = warButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = true;
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
        EnsureUIReferences();

        if (warButton != null)
        {
            warButton.SetActive(false);
            UnityEngine.UI.Button btn = warButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = false;
        }

        UnityEngine.UI.Button[] buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (UnityEngine.UI.Button btn in buttons)
        {
            if (btn != null && btn.gameObject.name == "WarButton")
            {
                btn.gameObject.SetActive(false);
                btn.interactable = false;
            }
        }
    }

    IEnumerator HideWarButtonNextFrame()
    {
        yield return null;
        HideWarButton();
    }

    // Для кнопки UI в Inspector
    public void DeclareWar()
    {
        DeclareWarOnSelectedCity();
    }

    public void DeclareWarOnSelectedCity()
    {
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
        isAtWar = diplomacy.isAtWar;

        selectedCityForWar = null;
        pendingWarTargetCiv = "";
    }
}