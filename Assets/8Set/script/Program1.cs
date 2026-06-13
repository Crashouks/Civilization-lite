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

    public static string ParseCivNameFromUnitName(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return "Unknown";
        if (unitName.Contains("Rome")) return "Rome";
        if (unitName.Contains("America")) return "America";
        if (unitName.Contains("Egypt")) return "Egypt";
        if (unitName.Contains("Scythia")) return "Scythia";
        return "Unknown";
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
    [Tooltip("Додатковий масштаб гексів у світі")]
    public float hexWorldScale = 1.5f;
    public float cellWidth = 2.56f;
    public float cellHeight = 2.56f;

    // Значення з Hex World Tiles - Free (PPU 100 → 256px = 2.56 од.)
    const float PackCellSize = 2.56f;

    [Header("Параметри Карти")]
    public int width = 50;
    public int height = 40;
    public int mapBorderWidth = 2;
    public int minCityDistance = 2;
    public int maxAiCityExpansionDistance = 5;
    public int peacefulCityStandoff = 2;
    public float scale = 10f;
    public float seed;

    bool[,] mapBorder;

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

    [Header("Візуалізація Шляху")]
    public LineRenderer pathRenderer;

    [Header("Міста")]
    public GameObject cityPrefab;

    [Header("Інтерфейс")]
    public GameObject warButton;
    public GameObject settleButton;
    public GameObject nextTurnButton;

    public Unit selectedUnit;
    public City selectedCityForWar;
    public string pendingWarTargetCiv = "";
    public List<Unit> allUnits = new List<Unit>();
    public List<City> allCities = new List<City>();
    readonly Dictionary<Vector3Int, Unit> unitAtCell = new Dictionary<Vector3Int, Unit>();
    readonly Dictionary<Vector3Int, City> cityAtCell = new Dictionary<Vector3Int, City>();
    int pathPreviewFrame = -1;
    private bool isMoving = false;
    private bool suppressNextMapClick = false;
    Vector3Int lastPathPreviewCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    bool pathPreviewCached;

    public bool IsPlayerActionBusy => isMoving;

    public bool CanPlayerAct()
    {
        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        return turnManager != null && turnManager.CanPlayerAct();
    }

    public bool CanEndPlayerTurn()
    {
        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        if (turnManager == null || !turnManager.CanPlayerAct())
            return false;

        return !isMoving;
    }

    public bool AllPlayerUnitsSpentMovement()
    {
        foreach (Unit unit in allUnits)
        {
            if (unit == null || !unit.isPlayer)
                continue;

            if (unit.currentMovement > 0)
                return false;
        }

        return true;
    }

    public void NotifyTurnStateChanged()
    {
        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        turnManager?.RefreshEndTurnButtons();
    }

    public GameObject FindObjectByNameInScenePublic(string objectName) => FindObjectByNameInScene(objectName);

    void Start()
    {
        GameSettings.ApplySavedSettings();

        if (grid == null) grid = GetComponent<Grid>();
        if (grid == null) grid = FindAnyObjectByType<Grid>();
        if (tilemap == null) tilemap = GetComponentInChildren<Tilemap>();

        if (grid == null || tilemap == null)
        {
            Debug.LogError("Program1: Grid or Tilemap not found. Hex map cannot be generated.");
            return;
        }

        if (pathRenderer != null) pathRenderer.enabled = false;

        EnsureScoutPrefab();
        EnsureGameSystems();
        EnsureUIReferences();
        EnsureScreenEdgeUI();
        StartCoroutine(LateBindUIReferences());

        ConfigureGrid();

        if (PlayerPrefs.GetInt(SaveManager.LoadOnStartKey, 0) == 1)
        {
            PlayerPrefs.SetInt(SaveManager.LoadOnStartKey, 0);
            PlayerPrefs.Save();
            int slot = PlayerPrefs.GetInt(SaveManager.LoadSlotKey, SaveManager.ActiveSlot);
            if (SaveManager.Instance != null)
            {
                StartCoroutine(SaveManager.Instance.RestoreGameOnStart(this, slot));
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
        if (Object.FindAnyObjectByType<DiplomacyManager>() == null)
            new GameObject("DiplomacyManager").AddComponent<DiplomacyManager>();
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
            HideWarButton();
            yield return new WaitForSeconds(0.25f);
        }
    }

    void EnsureScreenEdgeUI()
    {
        GameObject mainScreen = FindObjectByNameInScene("MainScreenUI");
        if (mainScreen != null)
        {
            RectTransform screenRt = mainScreen.GetComponent<RectTransform>();
            if (screenRt != null)
            {
                screenRt.anchorMin = Vector2.zero;
                screenRt.anchorMax = Vector2.one;
                screenRt.offsetMin = Vector2.zero;
                screenRt.offsetMax = Vector2.zero;
            }
        }

        GameObject rightButtons = FindObjectByNameInScene("RightButtons");
        if (rightButtons != null)
        {
            RectTransform rt = rightButtons.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-24f, 24f);
            }
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

        if (warButton != null)
        {
            warButton.SetActive(false);
            UnityEngine.UI.Button btn = warButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = false;
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

        if (!CanPlayerAct())
        {
            ClearPath();
            return;
        }

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) HandleMouseClick();
        if (selectedUnit != null)
            UpdatePathPreview();
        else if (pathRenderer != null && pathRenderer.enabled)
            HidePathPreview();
    }

    void ConfigureGrid()
    {
        ResolveHexCellSize();

        grid.cellLayout = GridLayout.CellLayout.Hexagon;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        grid.cellSize = new Vector3(cellWidth, cellHeight, 0f);
        grid.cellGap = Vector3.zero;
        grid.transform.localScale = Vector3.one * hexWorldScale;

        tilemap.tileAnchor = new Vector3(0f, -2f / 3f, 0f);
        tilemap.orientation = Tilemap.Orientation.XY;

        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
                renderer.material = new Material(shader);
        }

        if (pathRenderer != null)
            pathRenderer.useWorldSpace = true;
    }

    public float GetUnitVisualYOffset()
    {
        return cellHeight * hexWorldScale * 0.39f;
    }

    public Vector3 GetUnitPositionForCell(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        return new Vector3(center.x, center.y - GetUnitVisualYOffset(), -0.1f);
    }

    public bool IsValidMapCell(Vector3Int cell)
    {
        cell.z = 0;
        if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
            return false;
        return tilemap != null && tilemap.HasTile(cell);
    }

    public bool IsMapBorderCell(Vector3Int cell)
    {
        if (mapBorder == null || cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
            return false;
        return mapBorder[cell.x, cell.y];
    }

    public bool IsPlayableCell(Vector3Int cell)
    {
        return IsValidMapCell(cell) && !IsImpassable(cell);
    }

    public int GetHexDistance(Vector3Int a, Vector3Int b)
    {
        a.z = 0;
        b.z = 0;
        if (a == b)
            return 0;

        OffsetToCube(a, out int ax, out int ay, out int az);
        OffsetToCube(b, out int bx, out int by, out int bz);
        return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
    }

    static void OffsetToCube(Vector3Int cell, out int x, out int y, out int z)
    {
        // Even-r offset layout (matches GetNeighbors).
        x = cell.x;
        z = cell.y - (cell.x + (cell.x & 1)) / 2;
        y = -x - z;
    }

    public bool IsFarEnoughFromCities(Vector3Int cell, int minDistance = -1)
    {
        if (minDistance < 0)
            minDistance = minCityDistance;

        foreach (City city in allCities)
        {
            if (city == null)
                continue;

            if (GetHexDistance(cell, city.gridPosition) < minDistance)
                return false;
        }

        return true;
    }

    public bool IsValidCitySite(Vector3Int cell)
    {
        cell.z = 0;
        return IsValidMapCell(cell)
            && !IsImpassable(cell)
            && !HasCityAt(cell)
            && IsFarEnoughFromCities(cell);
    }

    public bool IsWithinAiExpansionRange(string civName, Vector3Int cell, int maxDistance = -1)
    {
        if (maxDistance < 0)
            maxDistance = maxAiCityExpansionDistance;

        if (CountCivCities(civName) == 0)
            return true;

        int closest = int.MaxValue;
        foreach (City city in allCities)
        {
            if (city == null || city.ownerCivName != civName)
                continue;

            closest = Mathf.Min(closest, GetHexDistance(cell, city.gridPosition));
        }

        return closest <= maxDistance;
    }

    public bool IsValidAiCitySite(string civName, Vector3Int cell)
    {
        return IsValidCitySite(cell) && IsWithinAiExpansionRange(civName, cell);
    }

    public bool IsBlockedByPeacefulEnemyCity(Vector3Int cell, Unit mover)
    {
        if (mover == null || !mover.isPlayer)
            return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy == null)
            return false;

        cell.z = 0;
        string playerCiv = currentCivName;

        foreach (City city in allCities)
        {
            if (city == null || city.isPlayerCity)
                continue;

            string owner = city.ownerCivName;
            if (string.IsNullOrEmpty(owner))
                continue;

            if (diplomacy.AreAtWar(playerCiv, owner))
                continue;

            if (GetHexDistance(cell, city.gridPosition) < peacefulCityStandoff)
                return true;
        }

        return false;
    }

    public City GetCityAt(Vector3Int cell)
    {
        cell.z = 0;
        cityAtCell.TryGetValue(cell, out City city);
        return city;
    }

    public int CountCivUnits(string civName)
    {
        if (string.IsNullOrEmpty(civName))
            return 0;

        int count = 0;
        foreach (Unit unit in allUnits)
        {
            if (unit == null)
                continue;

            if (unit.GetCivName(this) == civName)
                count++;
        }
        return count;
    }

    public int CountCivCities(string civName)
    {
        if (string.IsNullOrEmpty(civName))
            return 0;

        int count = 0;
        foreach (City city in allCities)
        {
            if (city != null && city.ownerCivName == civName)
                count++;
        }
        return count;
    }

    public List<string> GetLivingCivNames()
    {
        var civs = new HashSet<string>();
        foreach (City city in allCities)
        {
            if (city != null && !string.IsNullOrEmpty(city.ownerCivName) && city.ownerCivName != "Unknown")
                civs.Add(city.ownerCivName);
        }
        foreach (Unit unit in allUnits)
        {
            if (unit == null)
                continue;

            string civ = unit.GetCivName(this);
            if (!string.IsNullOrEmpty(civ) && civ != "Unknown")
                civs.Add(civ);
        }

        if (DiplomacyManager.Instance != null)
        {
            var dead = new List<string>();
            foreach (string civ in civs)
            {
                if (DiplomacyManager.Instance.IsCivEliminated(civ))
                    dead.Add(civ);
            }
            foreach (string civ in dead)
                civs.Remove(civ);
        }

        return new List<string>(civs);
    }

    public bool CanAttackCity(Unit attacker, City city)
    {
        if (attacker == null || city == null)
            return false;

        if (!attacker.CanAttackThisTurn())
            return false;

        if (city.IsOwnedByPlayer() == attacker.isPlayer)
            return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy == null)
            return false;

        return diplomacy.AreAtWar(attacker.GetCivName(this), city.ownerCivName);
    }

    public void DestroyCity(City city, Unit attacker)
    {
        if (city == null)
            return;

        string ownerCiv = city.ownerCivName;
        string attackerCiv = attacker != null ? attacker.GetCivName(this) : currentCivName;
        bool wasPlayerCity = city.isPlayerCity;

        allCities.Remove(city);

        CivilizationAI[] aiControllers = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        foreach (CivilizationAI ai in aiControllers)
        {
            if (ai != null)
                ai.RemoveCity(city);
        }

        Destroy(city.gameObject);

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy != null)
            diplomacy.OnCityCaptured(attackerCiv, ownerCiv);

        if (wasPlayerCity || (attacker != null && attacker.isPlayer))
            SaveManager.Instance?.MarkUnsaved();

        NotifyTurnStateChanged();
    }

    public IEnumerator CaptureCityRoutine(Unit attacker, City city)
    {
        if (attacker == null || city == null || !CanAttackCity(attacker, city))
            yield break;

        isMoving = true;
        ClearPath();

        int damage = attacker.attackPower;
        if (CombatSystem.Instance != null)
            damage = CombatSystem.Instance.GetAttackDamage(attacker);

        UnitAnimator anim = attacker.GetComponent<UnitAnimator>();
        if (anim != null)
        {
            Vector3 cityPos = tilemap.GetCellCenterWorld(city.gridPosition);
            anim.FaceToward(cityPos - attacker.transform.position);
            anim.PlayAttack();
        }

        yield return new WaitForSeconds(0.45f);

        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
        bool destroyed = city.TakeDamage(damage, currentTurn);

        attacker.hasAttackedThisTurn = true;
        attacker.currentMovement = 0;

        if (destroyed)
        {
            DestroyCity(city, attacker);
        }
        else
        {
            Debug.Log(city.GetDisplayName() + " отримало " + damage + " шкоди. HP: " + city.currentHealth + "/" + city.maxHealth);
            if (GameUI.Instance != null && GameUI.Instance.IsShowingCity(city))
                GameUI.Instance.RefreshCityPanel();
        }

        isMoving = false;
        NotifyTurnStateChanged();
    }

    public void RefreshAllCityHealth(int currentTurn)
    {
        foreach (City city in allCities)
        {
            if (city != null)
                city.RefreshHealth(currentTurn);
        }
    }

    public void GetPlayableWorldBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        int innerMinX = Mathf.Clamp(mapBorderWidth, 0, width - 1);
        int innerMinY = Mathf.Clamp(mapBorderWidth, 0, height - 1);
        int innerMaxX = Mathf.Clamp(width - mapBorderWidth - 1, 0, width - 1);
        int innerMaxY = Mathf.Clamp(height - mapBorderWidth - 1, 0, height - 1);

        if (tilemap == null)
        {
            minX = maxX = minY = maxY = 0f;
            return;
        }

        Vector3 bottomLeft = tilemap.GetCellCenterWorld(new Vector3Int(innerMinX, innerMinY, 0));
        Vector3 topRight = tilemap.GetCellCenterWorld(new Vector3Int(innerMaxX, innerMaxY, 0));

        float halfW = cellWidth * hexWorldScale * 0.55f;
        float halfH = cellHeight * hexWorldScale * 0.55f;

        minX = Mathf.Min(bottomLeft.x, topRight.x) - halfW;
        maxX = Mathf.Max(bottomLeft.x, topRight.x) + halfW;
        minY = Mathf.Min(bottomLeft.y, topRight.y) - halfH;
        maxY = Mathf.Max(bottomLeft.y, topRight.y) + halfH;
    }

    public void ClampCameraPosition(Camera cam)
    {
        if (cam == null || !cam.orthographic || tilemap == null)
            return;

        GetPlayableWorldBounds(out float minX, out float maxX, out float minY, out float maxY);

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float clampMinX = minX + horzExtent;
        float clampMaxX = maxX - horzExtent;
        float clampMinY = minY + vertExtent;
        float clampMaxY = maxY - vertExtent;

        Vector3 pos = cam.transform.position;
        pos.x = clampMinX > clampMaxX ? (minX + maxX) * 0.5f : Mathf.Clamp(pos.x, clampMinX, clampMaxX);
        pos.y = clampMinY > clampMaxY ? (minY + maxY) * 0.5f : Mathf.Clamp(pos.y, clampMinY, clampMaxY);
        cam.transform.position = pos;
    }

    public Vector3Int ResolveMapCell(Vector3Int storedCell, Vector3 worldPosition)
    {
        storedCell.z = 0;
        if (IsValidMapCell(storedCell))
            return storedCell;

        if (grid != null)
        {
            Vector3Int fromGrid = grid.WorldToCell(worldPosition);
            fromGrid.z = 0;
            if (IsValidMapCell(fromGrid))
                return fromGrid;
        }

        if (tilemap != null)
        {
            Vector3Int fromTilemap = tilemap.WorldToCell(worldPosition);
            fromTilemap.z = 0;
            if (IsValidMapCell(fromTilemap))
                return fromTilemap;
        }

        foreach (Vector3Int neighbor in GetNeighborCells(storedCell))
        {
            if (IsValidMapCell(neighbor))
                return neighbor;
        }

        return storedCell;
    }

    public Vector3Int ResolveUnitCell(Unit unit)
    {
        if (unit == null)
            return Vector3Int.zero;

        Vector3Int oldCell = unit.gridPosition;
        oldCell.z = 0;
        Vector3Int cell = ResolveMapCell(unit.gridPosition, unit.transform.position);
        cell.z = 0;
        if (oldCell != cell)
            UpdateUnitCellIndex(unit, oldCell, cell);
        unit.gridPosition = cell;
        return cell;
    }

    public Vector3Int ResolveCityCell(City city)
    {
        if (city == null)
            return Vector3Int.zero;

        Vector3Int cell = ResolveMapCell(city.gridPosition, city.transform.position);
        city.gridPosition = cell;
        return cell;
    }

    public Tile GetFogReferenceTile()
    {
        if (grassTile != null) return grassTile;
        if (forestTile != null) return forestTile;
        if (waterTile != null) return waterTile;
        if (sandTile != null) return sandTile;
        return null;
    }

    Vector3 GetMouseWorldPoint()
    {
        if (Camera.main == null || Pointer.current == null)
            return Vector3.zero;

        Vector2 mousePos = Pointer.current.position.ReadValue();
        Vector3 screen = new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z);
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    float GetUnitPickRadius()
    {
        return cellWidth * hexWorldScale * 0.65f;
    }

    Vector3Int GetMouseCell()
    {
        Vector3 world = GetMouseWorldPoint();
        Vector3Int cell = grid != null ? grid.WorldToCell(world) : tilemap.WorldToCell(world);
        cell.z = 0;
        return cell;
    }

    bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null || Pointer.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId);
    }

    Unit FindPlayerUnitUnderMouse() => FindUnitUnderMouse(true);

    Unit FindUnitUnderMouse(bool playerOnly)
    {
        Vector3 world = GetMouseWorldPoint();
        Vector2 world2D = world;
        float pickRadius = GetUnitPickRadius();

        Unit bestUnit = null;
        float bestDist = pickRadius;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(world2D, pickRadius);
        foreach (Collider2D col in overlaps)
        {
            if (col == null) continue;

            Unit unit = col.GetComponentInParent<Unit>();
            if (unit == null) continue;
            if (playerOnly && !unit.isPlayer) continue;

            float dist = Vector2.Distance(world2D, col.bounds.center);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestUnit = unit;
            }
        }

        if (bestUnit != null)
            return bestUnit;

        Vector3Int hoveredCell = GetMouseCell();
        foreach (Vector3Int checkCell in GetCellAndNeighbors(hoveredCell))
        {
            Unit unit = GetUnitAt(checkCell);
            if (unit == null) continue;
            if (playerOnly && !unit.isPlayer) continue;

            float dist = Vector2.Distance(world2D, unit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestUnit = unit;
            }
        }

        return bestUnit;
    }

    public bool AreCellsAdjacent(Vector3Int a, Vector3Int b)
    {
        if (a == b) return false;
        foreach (Vector3Int neighbor in GetNeighbors(a))
        {
            if (neighbor == b)
                return true;
        }
        return false;
    }

    public bool CanUnitsFight(Unit a, Unit b)
    {
        if (a == null || b == null || a == b) return false;
        if (a.isPlayer == b.isPlayer) return false;
        if (!a.CanAttackThisTurn()) return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy == null)
            return false;

        return diplomacy.AreAtWar(a.GetCivName(this), b.GetCivName(this));
    }

    void TryIssueOrderAgainstEnemy(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null || !CanUnitsFight(attacker, defender))
            return;

        if (AreCellsAdjacent(attacker.gridPosition, defender.gridPosition))
        {
            StartCoroutine(ExecuteMeleeAttack(attacker, defender));
            return;
        }

        List<Vector3Int> path = FindPath(attacker.gridPosition, defender.gridPosition, attacker);
        path = TrimPathByMovement(path, attacker.currentMovement);
        if (path != null && path.Count > 0)
            StartCoroutine(ExecutePathMovement(attacker, path));
    }

    IEnumerator ExecuteMeleeAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) yield break;

        isMoving = true;
        ClearPath();
        yield return attacker.StartCoroutine(attacker.JumpAttack(defender, this));

        if (attacker != null)
            attacker.currentMovement = 0;

        isMoving = false;
        NotifyTurnStateChanged();
    }

    City FindCityUnderMouse()
    {
        Vector3 world = GetMouseWorldPoint();
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(world, GetUnitPickRadius());
        City bestCity = null;
        float bestDist = GetUnitPickRadius();

        foreach (Collider2D col in overlaps)
        {
            if (col == null) continue;

            City city = col.GetComponentInParent<City>();
            if (city == null) continue;

            FogOfWarManager fog = FogOfWarManager.Instance;
            if (fog != null && fog.enableFogOfWar && !city.IsOwnedByPlayer())
            {
                Vector3Int cell = ResolveCityCell(city);
                if (!fog.IsVisible(cell) && !fog.IsExplored(cell))
                    continue;
            }

            float dist = Vector2.Distance(world, col.bounds.center);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestCity = city;
            }
        }

        return bestCity;
    }

    IEnumerable<Vector3Int> GetCellAndNeighbors(Vector3Int cell)
    {
        yield return cell;
        foreach (Vector3Int neighbor in GetNeighbors(cell))
            yield return neighbor;
    }

    void SyncUnitToGrid(Unit unit)
    {
        if (unit == null || tilemap == null) return;

        Vector3 expected = GetUnitPositionForCell(unit.gridPosition);
        if (Vector3.Distance(unit.transform.position, expected) > 0.15f)
            unit.transform.position = expected;
    }

    Vector3 GetCellPathPoint(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        return new Vector3(center.x, center.y - GetUnitVisualYOffset(), -0.2f);
    }

    public void FocusCameraOnCell(Vector3Int cell)
    {
        if (Camera.main == null || tilemap == null) return;

        Vector3 camPos = tilemap.GetCellCenterWorld(cell);
        Camera.main.transform.position = new Vector3(camPos.x, camPos.y, Camera.main.transform.position.z);
        Camera.main.orthographicSize = cellHeight * hexWorldScale * 2.5f;
        ClampCameraPosition(Camera.main);
    }

    public void FocusCameraOnPlayerCapital()
    {
        City target = null;
        foreach (City city in allCities)
        {
            if (city != null && city.isPlayerCity && city.isCapital)
            {
                target = city;
                break;
            }
        }

        if (target == null)
        {
            foreach (City city in allCities)
            {
                if (city != null && city.isPlayerCity)
                {
                    target = city;
                    break;
                }
            }
        }

        if (target != null)
        {
            FocusCameraOnCell(target.gridPosition);
            return;
        }

        Unit playerUnit = allUnits.Find(u => u != null && u.isPlayer);
        if (playerUnit != null)
            FocusCameraOnCell(playerUnit.gridPosition);
    }

    void ResolveHexCellSize()
    {
        Tile refTile = grassTile != null ? grassTile : waterTile;
        if (refTile != null && refTile.sprite != null)
            cellWidth = refTile.sprite.bounds.size.x;
        else
            cellWidth = PackCellSize;

        cellHeight = cellWidth;
    }

    void FillHexMap()
    {
        if (tilemap == null) return;

        tilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xOffset = (y & 1) == 1 ? 0.5f : 0f;
                float xCoord = ((float)x + xOffset) / width * scale + seed;
                float yCoord = (float)y / height * scale + seed;
                float h = Mathf.PerlinNoise(xCoord, yCoord);
                float m = Mathf.PerlinNoise(xCoord + 2000f, yCoord + 2000f);
                float dist = Mathf.Abs(y - (height / 2f)) / (height / 2f);
                float t = Mathf.Clamp01(1f - dist + (Mathf.PerlinNoise(xCoord * 0.5f, yCoord * 0.5f) - 0.5f) * 0.2f);
                Tile tile = GetAdvancedBiome(h, m, t);
                if (tile != null)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        tilemap.RefreshAllTiles();
        tilemap.CompressBounds();
        ApplyMapBorder();
    }

    void ApplyMapBorder()
    {
        if (tilemap == null || width <= 0 || height <= 0)
            return;

        mapBorderWidth = Mathf.Clamp(mapBorderWidth, 1, Mathf.Min(width, height) / 2);
        mapBorder = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isBorder = x < mapBorderWidth
                    || y < mapBorderWidth
                    || x >= width - mapBorderWidth
                    || y >= height - mapBorderWidth;

                mapBorder[x, y] = isBorder;
                if (!isBorder)
                    continue;

                Tile borderTile = GetBorderMountainTile(y);
                if (borderTile != null)
                    tilemap.SetTile(new Vector3Int(x, y, 0), borderTile);
            }
        }

        tilemap.RefreshAllTiles();
    }

    Tile GetBorderMountainTile(int y)
    {
        if (snowMountainTile != null && y >= height - mapBorderWidth)
            return snowMountainTile;
        if (mountainTile != null)
            return mountainTile;
        if (snowMountainTile != null)
            return snowMountainTile;
        return forestTile != null ? forestTile : grassTile;
    }

    private void HandleMouseClick()
    {
        if (Camera.main == null) return;

        if (GameUI.Instance != null && GameUI.Instance.IsPauseMenuOpen)
            return;

        // Після натискання UI-кнопки (Declare war) в цей же кадр може спрацювати клік по мапі.
        // Супресимо 1 наступний мап-клік.
        if (suppressNextMapClick)
        {
            suppressNextMapClick = false;
            return;
        }

        // Якщо клік по UI (наприклад, кнопка війни), не обробляємо клік по карті.
        if (IsPointerOverBlockingUI())
            return;

        Unit clickedUnit = FindPlayerUnitUnderMouse();
        if (clickedUnit != null)
        {
            SyncUnitToGrid(clickedUnit);
            if (selectedUnit == clickedUnit)
                DeselectUnit();
            else
                SelectUnit(clickedUnit);
            return;
        }

        if (selectedUnit != null && selectedUnit.isPlayer)
        {
            Unit enemyUnit = FindUnitUnderMouse(false);
            if (enemyUnit != null && !enemyUnit.isPlayer)
            {
                TryIssueOrderAgainstEnemy(selectedUnit, enemyUnit);
                return;
            }
        }

        City clickedCity = FindCityUnderMouse();
        if (clickedCity != null)
        {
            if (selectedUnit != null && selectedUnit.isPlayer && CanAttackCity(selectedUnit, clickedCity))
            {
                if (AreCellsAdjacent(selectedUnit.gridPosition, clickedCity.gridPosition))
                    StartCoroutine(CaptureCityRoutine(selectedUnit, clickedCity));
                else
                {
                    List<Vector3Int> path = FindPath(selectedUnit.gridPosition, clickedCity.gridPosition, selectedUnit);
                    path = TrimPathByMovement(path, selectedUnit.currentMovement);
                    if (path != null && path.Count > 0)
                        StartCoroutine(ExecutePathMovement(selectedUnit, path));
                }
                return;
            }

            OnCityClicked(clickedCity);
            return;
        }

        Vector3Int clickedCell = GetMouseCell();
        clickedCell.z = 0;

        // Ховаємо панель міста при кліку на порожній тайл
        if (GameUI.Instance != null)
            GameUI.Instance.HideCityPanel();

        if (selectedUnit != null && IsPlayableCell(clickedCell))
        {
            if (clickedCell == selectedUnit.gridPosition)
                return;

            Unit defenderOnTile = GetUnitAt(clickedCell);
            if (defenderOnTile != null && defenderOnTile != selectedUnit && CanUnitsFight(selectedUnit, defenderOnTile))
            {
                TryIssueOrderAgainstEnemy(selectedUnit, defenderOnTile);
                return;
            }

            List<Vector3Int> path = FindPath(selectedUnit.gridPosition, clickedCell, selectedUnit);
            path = TrimPathByMovement(path, selectedUnit.currentMovement);
            if (path != null && path.Count > 0)
                StartCoroutine(ExecutePathMovement(selectedUnit, path));
        }
    }

    void UpdatePathPreview()
    {
        if (selectedUnit == null || tilemap == null)
        {
            HidePathPreview();
            return;
        }

        if (IsPointerOverBlockingUI())
        {
            HidePathPreview();
            return;
        }

        Vector3Int hoveredCell = GetMouseCell();
        hoveredCell.z = 0;

        if (pathPreviewCached && hoveredCell == lastPathPreviewCell)
            return;

        if (Time.frameCount == pathPreviewFrame)
            return;

        pathPreviewFrame = Time.frameCount;
        lastPathPreviewCell = hoveredCell;
        pathPreviewCached = true;

        if (!tilemap.HasTile(hoveredCell) || hoveredCell == selectedUnit.gridPosition)
        {
            HidePathPreview();
            return;
        }

        int maxPreviewCost = Mathf.Max(1, selectedUnit.currentMovement) * 2;
        List<Vector3Int> path = FindPath(selectedUnit.gridPosition, hoveredCell, selectedUnit, maxPreviewCost);
        path = TrimPathByMovement(path, selectedUnit.currentMovement);
        if (path != null && path.Count > 0)
            DrawPath(path);
        else
            HidePathPreview();
    }

    void DrawPath(List<Vector3Int> path)
    {
        if (pathRenderer == null || path == null || path.Count == 0 || selectedUnit == null) return;

        pathRenderer.useWorldSpace = true;
        pathRenderer.enabled = true;
        pathRenderer.positionCount = path.Count + 1;

        Vector3 startPos = selectedUnit.transform.position;
        startPos.z = -0.2f;
        pathRenderer.SetPosition(0, startPos);

        for (int i = 0; i < path.Count; i++)
            pathRenderer.SetPosition(i + 1, GetCellPathPoint(path[i]));
    }

    void HidePathPreview()
    {
        if (pathRenderer != null)
            pathRenderer.enabled = false;
    }

    void ClearPath()
    {
        pathPreviewCached = false;
        lastPathPreviewCell = new Vector3Int(int.MinValue, int.MinValue, 0);
        HidePathPreview();
    }
    void SelectUnit(Unit unit)
    {
        if (unit == null) return;

        SyncUnitToGrid(unit);
        if (selectedUnit != null && selectedUnit != unit)
            selectedUnit.Deselect();

        selectedUnit = unit;
        ClearPath();
        selectedUnit.Select();
    }
    void DeselectUnit() { if (selectedUnit != null) selectedUnit.Deselect(); selectedUnit = null; }

    public void DeselectUnitForTurnEnd()
    {
        DeselectUnit();
        ClearPath();
        HideCityPanelIfOpen();
    }

    void HideCityPanelIfOpen()
    {
        if (GameUI.Instance != null)
            GameUI.Instance.HideCityPanel();
    }

    public void OnCityClicked(City city)
    {
        if (city == null) return;

        if (!city.IsOwnedByPlayer())
        {
            FogOfWarManager fog = FogOfWarManager.Instance;
            if (fog != null && fog.enableFogOfWar)
            {
                Vector3Int cell = ResolveCityCell(city);
                if (!fog.IsVisible(cell) && !fog.IsExplored(cell))
                    return;
            }
        }

        DeselectUnit();
        ClearPath();

        if (city.IsOwnedByPlayer())
        {
            pendingWarTargetCiv = "";
            HideWarButton();
            if (GameUI.Instance != null)
                GameUI.Instance.ShowCityPanel(city);
            return;
        }

        if (GameUI.Instance != null)
            GameUI.Instance.ShowCityPanel(city);
        selectedCityForWar = city;
        pendingWarTargetCiv = ResolveWarTargetCiv(city);
        HideWarButton();
    }

    void ShowCityInfoPanel(City city)
    {
        if (GameUI.Instance != null)
            GameUI.Instance.ShowCityPanel(city);
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
            float d = GetHexDistance(u.gridPosition, city.gridPosition);
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

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int end, Unit mover = null, int maxCost = int.MaxValue)
    {
        start.z = 0;
        end.z = 0;

        if (start == end)
            return new List<Vector3Int>();

        if (mover != null && mover.isPlayer && IsBlockedByPeacefulEnemyCity(end, mover))
            return null;

        if (IsImpassable(end))
        {
            Unit endUnit = GetUnitAt(end);
            if (endUnit == null || mover == null || !CanUnitsFight(mover, endUnit))
                return null;
        }

        City endCity = GetCityAt(end);
        if (endCity != null && mover != null && !CanAttackCity(mover, endCity))
            return null;

        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int> { [start] = 0 };
        Dictionary<Vector3Int, Vector3Int?> cameFrom = new Dictionary<Vector3Int, Vector3Int?> { [start] = null };
        List<Vector3Int> open = new List<Vector3Int> { start };
        HashSet<Vector3Int> inOpen = new HashSet<Vector3Int> { start };
        HashSet<Vector3Int> closed = new HashSet<Vector3Int>();

        while (open.Count > 0)
        {
            int bestIndex = 0;
            int bestCost = costSoFar[open[0]];
            for (int i = 1; i < open.Count; i++)
            {
                int c = costSoFar[open[i]];
                if (c < bestCost)
                {
                    bestCost = c;
                    bestIndex = i;
                }
            }

            Vector3Int current = open[bestIndex];
            open.RemoveAt(bestIndex);
            inOpen.Remove(current);
            if (!closed.Add(current))
                continue;
            if (current == end) break;

            foreach (Vector3Int next in GetNeighbors(current))
            {
                Vector3Int neighbor = next;
                neighbor.z = 0;
                if (closed.Contains(neighbor)) continue;
                if (IsImpassable(neighbor)) continue;
                if (IsBlockedForPath(neighbor, start, end, mover)) continue;

                int newCost = costSoFar[current] + GetMovementCost(neighbor);
                if (newCost > maxCost) continue;

                if (costSoFar.TryGetValue(neighbor, out int oldCost) && newCost >= oldCost)
                    continue;

                costSoFar[neighbor] = newCost;
                cameFrom[neighbor] = current;
                if (inOpen.Add(neighbor))
                    open.Add(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(end)) return null;

        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int temp = end;
        while (temp != start)
        {
            path.Add(temp);
            temp = (Vector3Int)cameFrom[temp];
        }
        path.Reverse();
        return path;
    }

    bool IsBlockedForPath(Vector3Int cell, Vector3Int start, Vector3Int end, Unit mover)
    {
        if (mover != null && mover.isPlayer && IsBlockedByPeacefulEnemyCity(cell, mover))
            return true;

        if (cell == start)
            return false;

        if (cell == end)
        {
            if (mover != null && mover.isPlayer && IsBlockedByPeacefulEnemyCity(end, mover))
                return true;

            Unit endOccupant = GetUnitAt(cell);
            if (endOccupant != null && mover != null && CanUnitsFight(mover, endOccupant))
                return false;

            City endCity = GetCityAt(cell);
            if (endCity != null && mover != null && CanAttackCity(mover, endCity))
                return false;

            return endOccupant != null;
        }

        Unit occupant = GetUnitAt(cell);
        if (occupant == null || occupant == mover)
        {
            if (cell != start && cell != end && HasCityAt(cell))
                return true;
            return false;
        }

        return true;
    }

    List<Vector3Int> TrimPathByMovement(List<Vector3Int> path, int movementPoints)
    {
        if (path == null) return null;

        List<Vector3Int> trimmed = new List<Vector3Int>();
        int spent = 0;
        foreach (Vector3Int cell in path)
        {
            int cost = GetMovementCost(cell);
            if (spent + cost > movementPoints) break;
            spent += cost;
            trimmed.Add(cell);
        }
        return trimmed;
    }

    // Unity hex layout (pointy-top, odd-r) — matches Grid.CellLayout.Hexagon
    static readonly Vector3Int[] HexEvenRowOffsets =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(-1, -1, 0),
        new Vector3Int(0, -1, 0),
    };

    static readonly Vector3Int[] HexOddRowOffsets =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, -1, 0),
    };

    public List<Vector3Int> GetNeighborCells(Vector3Int cell) => GetNeighbors(cell);

    List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        Vector3Int[] offsets = (cell.y & 1) == 0 ? HexEvenRowOffsets : HexOddRowOffsets;
        List<Vector3Int> neighbors = new List<Vector3Int>(6);
        foreach (Vector3Int offset in offsets)
            neighbors.Add(cell + offset);
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
        if (!IsValidMapCell(cell))
            return true;

        if (IsMapBorderCell(cell))
            return true;

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

        if (unit != null)
        {
            UnitAnimator endAnim = unit.GetComponent<UnitAnimator>();
            if (endAnim != null)
                endAnim.ForceIdle();
        }

        if (unit != null && unit.currentMovement <= 0)
        {
            // Показуємо кнопку заснування міста якщо це поселенець
            ShowSettleButton();
            DeselectUnit();
        }
        isMoving = false;
        if (unit != null && unit.isPlayer)
            SaveManager.Instance?.MarkUnsaved();
        NotifyTurnStateChanged();
    }

    // ВИПРАВЛЕНО: Додано модифікатор PUBLIC, щоб файл units.cs міг бачити цей метод
    public Unit GetUnitAt(Vector3Int cell)
    {
        cell.z = 0;
        unitAtCell.TryGetValue(cell, out Unit unit);
        return unit;
    }

    public void RegisterUnitCell(Unit unit, Vector3Int cell)
    {
        if (unit == null)
            return;

        cell.z = 0;
        unitAtCell[cell] = unit;
    }

    public void UpdateUnitCellIndex(Unit unit, Vector3Int oldCell, Vector3Int newCell)
    {
        if (unit == null)
            return;

        oldCell.z = 0;
        newCell.z = 0;
        if (unitAtCell.TryGetValue(oldCell, out Unit atOld) && atOld == unit)
            unitAtCell.Remove(oldCell);
        unitAtCell[newCell] = unit;
    }

    public void UnregisterUnitCell(Unit unit)
    {
        if (unit == null)
            return;

        Vector3Int cell = unit.gridPosition;
        cell.z = 0;
        if (unitAtCell.TryGetValue(cell, out Unit at) && at == unit)
            unitAtCell.Remove(cell);
    }

    public void RebuildUnitCellIndex()
    {
        unitAtCell.Clear();
        foreach (Unit unit in allUnits)
        {
            if (unit == null)
                continue;

            Vector3Int cell = unit.gridPosition;
            cell.z = 0;
            unitAtCell[cell] = unit;
        }
    }

    public IEnumerator GenerateMapRoutine()
    {
        SaveManager.LoadedFromSaveThisSession = false;
        TurnManager.Instance?.SetPlayerDefeated(false);

        FillHexMap();
        yield return null;

        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.Initialize(this);

        SpawnStartingUnits();
        RebuildUnitCellIndex();
        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RefreshVisibility();
        NotifyTurnStateChanged();
        RefreshAllCityLabels();

        yield return WaitForAiCivilizationsSpawned();
        RebuildUnitCellIndex();

        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
            yield return turnManager.StartCoroutine(turnManager.BeginNewGameTurnCycle());
    }

    IEnumerator WaitForAiCivilizationsSpawned()
    {
        if (SaveManager.LoadedFromSaveThisSession)
            yield break;

        float timeout = 20f;
        float waited = 0f;
        while (waited < timeout)
        {
            if (DiplomacyManager.Instance != null && DiplomacyManager.Instance.AiSpawnComplete)
                yield break;

            waited += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void RefreshAllCityLabels()
    {
        foreach (City city in allCities)
        {
            if (city == null) continue;
            city.EnsureDisplayName(this);
            city.SetupLabel(city.ownerCivName, GetCivColor(city.ownerCivName));
        }
    }

    public void ClearAllGameObjects()
    {
        foreach (Unit unit in new List<Unit>(allUnits))
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        allUnits.Clear();
        unitAtCell.Clear();

        foreach (City city in new List<City>(allCities))
        {
            if (city != null) Destroy(city.gameObject);
        }
        allCities.Clear();
        cityAtCell.Clear();

        selectedUnit = null;
        tilemap.ClearAllTiles();
    }

    public IEnumerator RegenerateAndRestore(GameSaveData data)
    {
        FillHexMap();
        yield return null;

        foreach (CitySaveData cityData in data.cities)
        {
            Vector3Int pos = new Vector3Int(cityData.x, cityData.y, 0);
            SpawnCityFromSave(pos, cityData.isPlayerCity, cityData.ownerCivName, cityData.cityName, cityData.isCapital, cityData.foundedTurn, cityData.currentHealth);
        }

        foreach (UnitSaveData unitData in data.units)
        {
            SpawnUnitFromSave(unitData);
        }

        RebuildUnitCellIndex();
        RebuildCityCellIndex();

        InitializeFogOfWar();
        if (FogOfWarManager.Instance != null && data.exploredFlat != null)
            FogOfWarManager.Instance.ImportExploredFlat(data.exploredFlat);

        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        if (DiplomacyManager.Instance != null)
            DiplomacyManager.Instance.RestoreAiControllersFromSave(this);

        FocusCameraOnPlayerCapital();

        SaveManager.IsRestoringSave = false;
        if (SaveManager.Instance != null)
            SaveManager.Instance.MarkSaved();
        NotifyTurnStateChanged();
        RefreshAllCityLabels();

        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
            turnManager.ResumePlayerTurnAfterLoad();
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

    void InitializeFogOfWar()
    {
        if (FogOfWarManager.Instance == null) return;
        FogOfWarManager.Instance.Initialize(this);
    }

    void SpawnCityFromSave(Vector3Int pos, bool isPlayerCity, string ownerCiv, string cityName, bool isCapital = false, int foundedTurn = 1, int savedHealth = -1)
    {
        if (cityPrefab == null) return;

        GameObject cityObj = Instantiate(cityPrefab, Vector3.zero, Quaternion.identity);

        City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
        city.gridPosition = pos;
        city.isPlayerCity = isPlayerCity;
        city.ownerCivName = ownerCiv;
        city.isCapital = isCapital;
        city.cityName = string.IsNullOrEmpty(cityName) ? ownerCiv + " City" : cityName;
        city.foundedTurn = foundedTurn > 0 ? foundedTurn : 1;
        cityObj.name = ownerCiv + "_" + city.cityName;
        city.Init(pos, tilemap);

        Color civColor = GetCivColor(ownerCiv);
        if (!isPlayerCity && DiplomacyManager.Instance != null)
            civColor = DiplomacyManager.Instance.GetCivColor(ownerCiv);

        city.SetupLabel(ownerCiv, civColor);
        RegisterCity(city);

        if (savedHealth >= 0)
            city.currentHealth = savedHealth;
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
        unit.hasAttackedThisTurn = unitData.hasAttackedThisTurn;
        unit.ownerCivName = unitData.isPlayer ? currentCivName : unitData.civName;

        UnitHealth health = unit.GetComponent<UnitHealth>();
        if (health != null)
        {
            health.maxHealth = unit.health;
            health.currentHealth = unit.health;
        }

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
        if (unitType == "Archer") return warriorPrefab;
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

        FocusCameraOnCell(settlerPos);
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
        return true;
    }

    public bool TrySpawnUnitFromCity(City city, UnitTypeHelper.UnitKind kind)
    {
        if (city == null || !city.IsOwnedByPlayer()) return false;
        return TrySpawnUnitFromCityInternal(city, currentCivName, kind, true);
    }

    public bool TrySpawnAiUnitFromCity(City city, string civName, UnitTypeHelper.UnitKind kind)
    {
        if (city == null || string.IsNullOrEmpty(civName) || city.ownerCivName != civName)
            return false;

        return TrySpawnUnitFromCityInternal(city, civName, kind, false);
    }

    public City FindCityForRecruitment(string civName)
    {
        City capital = null;
        City anyCity = null;

        foreach (City city in allCities)
        {
            if (city == null || city.ownerCivName != civName)
                continue;

            anyCity = city;
            if (city.isCapital)
            {
                capital = city;
                break;
            }
        }

        return capital != null ? capital : anyCity;
    }

    bool TrySpawnUnitFromCityInternal(City city, string civName, UnitTypeHelper.UnitKind kind, bool isPlayerSpawn)
    {
        if (!CanSpawnUnit(isPlayerSpawn, kind))
            return false;

        EconomyManager economy = EconomyManager.Instance;
        if (economy == null)
            return false;

        GameObject prefab = GetPrefabForKind(kind);
        if (prefab == null)
            return false;

        Vector3Int spawnPos = FindSpawnNearCity(city.gridPosition);
        if (spawnPos == Vector3Int.zero && HasCityAt(city.gridPosition))
            spawnPos = FindValidSpawnPosition(city.gridPosition + new Vector3Int(1, 0, 0));

        if (spawnPos == Vector3Int.zero)
            return false;

        int cost = economy.GetUnitCost(kind);
        bool paid = isPlayerSpawn
            ? economy.TrySpendCoins(cost, "найм " + UnitTypeHelper.GetTypeName(kind))
            : economy.TrySpendAiCivCoins(civName, cost, "найм " + UnitTypeHelper.GetTypeName(kind));

        if (!paid)
            return false;

        string unitName = isPlayerSpawn
            ? UnitTypeHelper.GetTypeName(kind)
            : civName + "_" + UnitTypeHelper.GetTypeName(kind);
        Unit unit = CreateUnit(prefab, spawnPos, unitName, isPlayerSpawn, skipLimitCheck: !isPlayerSpawn);
        if (unit == null)
            return false;

        if (!isPlayerSpawn && unit.GetComponent<UnitAI>() == null)
            unit.gameObject.AddComponent<UnitAI>();

        if (SaveManager.Instance != null)
            SaveManager.Instance.MarkUnsaved();

        NotifyTurnStateChanged();
        return true;
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
        Vector3 worldPos = GetUnitPositionForCell(cellPos);
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);
        UnitSetup.Configure(obj, name, isPlayerUnit);
        Unit u = obj.GetComponent<Unit>();
        u.gridPosition = cellPos;
        u.isPlayer = isPlayerUnit;
        u.ownerCivName = isPlayerUnit ? currentCivName : ParseCivNameFromUnitName(name);
        ApplyUnitStats(u, name);
        allUnits.Add(u);
        RegisterUnitCell(u, cellPos);

        Colorize(obj);
        UnitSetup.FitPickCollider(obj);
        return u;
    }

    public void EndTurn()
    {
        foreach (Unit u in allUnits) u.ResetMovement();
        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RefreshVisibility();
    }
    void ApplyUnitStats(Unit unit, string unitName)
    {
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.ApplyStatsToUnit(unit);
            return;
        }

        UnitTypeHelper.UnitKind kind = UnitTypeHelper.GetKind(unitName);
        switch (kind)
        {
            case UnitTypeHelper.UnitKind.Scout:
                unit.maxMovement = 4;
                unit.health = 25;
                unit.attackPower = 10;
                break;
            case UnitTypeHelper.UnitKind.Warrior:
                unit.maxMovement = 3;
                unit.health = 50;
                unit.attackPower = 25;
                break;
            case UnitTypeHelper.UnitKind.Settler:
                unit.maxMovement = 3;
                unit.health = 50;
                unit.attackPower = 0;
                break;
        }
        unit.currentMovement = unit.maxMovement;
        unit.hasAttackedThisTurn = false;
    }

    public FogOfWarManager GetFogOfWar() => FogOfWarManager.Instance;

    public bool HasCityAt(Vector3Int cell)
    {
        cell.z = 0;
        return cityAtCell.ContainsKey(cell);
    }

    public void RegisterCityCell(City city, Vector3Int cell)
    {
        if (city == null)
            return;

        cell.z = 0;
        cityAtCell[cell] = city;
    }

    public void UnregisterCityCell(City city)
    {
        if (city == null)
            return;

        Vector3Int cell = city.gridPosition;
        cell.z = 0;
        if (cityAtCell.TryGetValue(cell, out City at) && at == city)
            cityAtCell.Remove(cell);
    }

    public void RebuildCityCellIndex()
    {
        cityAtCell.Clear();
        foreach (City city in allCities)
        {
            if (city == null)
                continue;

            Vector3Int cell = city.gridPosition;
            cell.z = 0;
            cityAtCell[cell] = city;
        }
    }

    public void RegisterCity(City city)
    {
        if (city == null)
            return;

        city.EnsureDisplayName(this);

        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        city.InitializeFoundedTurn(turnManager != null ? turnManager.currentTurn : 1);

        if (city.GetComponent<CityLabel>() == null)
            city.SetupLabel(city.ownerCivName, GetCivColor(city.ownerCivName));

        if (allCities.Contains(city))
            return;

        allCities.Add(city);
        RegisterCityCell(city, city.gridPosition);
        Colorize(city.gameObject);

        city.RefreshHealth(turnManager != null ? turnManager.currentTurn : 1);

        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.OnCityRegistered(city);
    }

    public void RemoveUnit(Unit unit)
    {
        if (unit == null)
            return;

        string civName = unit.GetCivName(this);

        if (allUnits.Contains(unit))
            allUnits.Remove(unit);

        UnregisterUnitCell(unit);

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy != null && !string.IsNullOrEmpty(civName))
            diplomacy.CheckCivElimination(civName);
    }

    // Метод для показу кнопки заснування міста
    public void ShowSettleButton()
    {
        if (selectedUnit != null && selectedUnit.name.Contains("Settler") && selectedUnit.currentMovement <= 0)
        {
            if (settleButton != null)
            {
                settleButton.SetActive(true);
            }
        }
    }

    // Метод для заснування міста з UI
    public void SettleCity()
    {
        if (!CanPlayerAct())
            return;

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

    public void NextTurn()
    {
        TurnManager tm = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        tm?.EndTurn();
    }

    public IEnumerator RunAiTurnPhase()
    {
        DeselectUnit();
        ClearPath();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.ProcessAiRecruitment(this);

        foreach (Unit unit in allUnits.ToArray())
        {
            if (unit != null)
                unit.ResetMovement();
        }

        CivilizationAI[] aiCivs = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        foreach (CivilizationAI ai in aiCivs)
        {
            if (ai == null || ai.civilizationName == "AI Civilization")
                continue;

            ai.ExecuteAITurn();
            float waitTimeout = 20f;
            float waited = 0f;
            while (ai.IsProcessingTurn())
            {
                waited += Time.deltaTime;
                if (waited >= waitTimeout)
                {
                    Debug.LogError("AI хід " + ai.civilizationName + " перевищив час очікування, примусово завершуємо");
                    ai.ForceEndProcessing();
                    break;
                }
                yield return null;
            }
        }

        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy != null)
        {
            diplomacy.RollAiWarDeclarations(this);
        }

        NotifyTurnStateChanged();
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

        if (selectedCityForWar.IsOwnedByPlayer())
        {
            selectedCityForWar = null;
            pendingWarTargetCiv = "";
            return;
        }

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