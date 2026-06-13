using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DiplomacyManager : MonoBehaviour
{
    public static DiplomacyManager Instance;
    
    public bool isAtWar = false;
    public List<string> enemyNations = new List<string>();

    readonly HashSet<string> activeWarPairs = new HashSet<string>();
    readonly HashSet<string> eliminatedCivs = new HashSet<string>();

    [Header("AI дипломатія")]
    public float aiVsAiWarChance = 0.12f;

    [Header("Налаштування AI цивілізацій")]
    public GameObject[] civPrefabs; // Префаби для різних цивілізацій
    public Dictionary<string, Color> civColors = new Dictionary<string, Color>(); // Кольори для кожної цивілізації
    public string[] civNames = { "Rome", "America", "Egypt", "Scythia" };
    
    [Header("Стартові позиції AI")]
    public Vector3Int[] aiSpawnPositions = new Vector3Int[3]; // 3 позиції для AI

    public bool AiSpawnComplete { get; private set; }

    void Awake()
    {
        Instance = this;
        string playerCiv = PlayerPrefs.GetString("SelectedCiv", "Rome");
        
        // Спочатку без ворогів - мирний початок
        enemyNations.Clear();
            
        Debug.Log("Гравець обрав: " + playerCiv);
        Debug.Log("AI не має ворогів на початку - мирний режим");
    }
    
    void Start()
    {
        // Спавним AI цивілізації через невеликий затримку
        StartCoroutine(SpawnAICivilizations());

        // ВИМКНЕНО - AI тепер керується через кнопку "Наступний хід" в Program1
        // StartCoroutine(AITurnLoop());
    }
    
    IEnumerator AITurnLoop()
    {
        // ВИМКНЕНО - AI тепер керується через кнопку "Наступний хід" в Program1
        yield break;
    }
    
    IEnumerator SpawnAICivilizations()
    {
        Program1 manager = null;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            yield return new WaitForSeconds(0.25f);
            manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null) break;
        }

        if (SaveManager.LoadedFromSaveThisSession)
        {
            AiSpawnComplete = true;
            yield break;
        }

        if (manager == null)
        {
            AiSpawnComplete = true;
            yield break;
        }
        
        string playerCiv = PlayerPrefs.GetString("SelectedCiv", "Rome");
        int aiIndex = 0;

        // Створюємо список доступних цивілізацій для AI (виключаючи вибір гравця)
        List<string> availableCivs = new List<string>();
        foreach (string civ in civNames)
        {
            if (civ != playerCiv)
            {
                availableCivs.Add(civ);
            }
        }

        // Спавнимо 3 AI цивілізації (гравець + 3 AI = 4 всього)
        for (int i = 0; i < 3 && i < availableCivs.Count; i++)
        {
            Color civColor = GetCivColor(availableCivs[i]);
            SpawnAICiv(availableCivs[i], civColor, aiIndex, manager);
            aiIndex++;
        }
        
        Debug.Log("Спавнено " + aiIndex + " AI цивілізацій в мирному режимі");

        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RefreshVisibility();

        AiSpawnComplete = true;
    }
    
    public Color GetCivColor(string civName)
    {
        if (civColors.ContainsKey(civName))
        {
            return civColors[civName];
        }
        
        // Колір за замовчуванням якщо не знайдено
        switch (civName)
        {
            case "Rome": return Color.red;
            case "America": return Color.blue;
            case "Egypt": return Color.yellow;
            case "Scythia": return Color.green;
            default: return Color.gray;
        }
    }
    
    void SpawnAICiv(string civName, Color civColor, int index, Program1 manager)
    {
        Vector3Int preferredSpawn = FindAISpawnPosition(index, manager);
        Vector3Int spawnPos = FindFreeLandPosition(preferredSpawn, manager);
        
        if (spawnPos != Vector3Int.zero)
        {
            // Створюємо поселенця для AI
            Unit settlerUnit = manager.CreateUnit(manager.settlerPrefab, spawnPos, civName + "_Settler", false);
            if (settlerUnit != null)
            {
                settlerUnit.gameObject.name = civName + "_Settler";
                if (settlerUnit.GetComponent<UnitAI>() == null)
                {
                    settlerUnit.gameObject.AddComponent<UnitAI>();
                }
                ApplyUnitColor(settlerUnit.gameObject, civColor);
                Debug.Log("Створено AI поселенця: " + civName + "_Settler");
                
                // Вимога: при спавні одразу ставимо поселення.
                settlerUnit.CreateCity();

                // Fallback: якщо з будь-якої причини місто не створилось - створюємо примусово.
                if (!manager.HasCityAt(spawnPos))
                {
                    CreateAICityDirectly(manager, spawnPos, civName, civColor);
                    manager.RemoveUnit(settlerUnit);
                    Destroy(settlerUnit.gameObject);
                }
            }
            
            // Створюємо воїна для AI
            Vector3Int warriorSpawn = FindFreeLandPosition(FindValidPositionNear(spawnPos, manager), manager);
            if (warriorSpawn == Vector3Int.zero)
            {
                warriorSpawn = FindValidPositionNear(spawnPos, manager);
            }
            Unit warriorUnit = manager.CreateUnit(manager.warriorPrefab, warriorSpawn, civName + "_Warrior", false);
            if (warriorUnit != null)
            {
                warriorUnit.gameObject.name = civName + "_Warrior";
                if (warriorUnit.GetComponent<UnitAI>() == null)
                {
                    warriorUnit.gameObject.AddComponent<UnitAI>();
                }
                ApplyUnitColor(warriorUnit.gameObject, civColor);
                Debug.Log("Створено AI воїна: " + civName + "_Warrior");
            }
            
            Debug.Log("Створено AI цивілізацію " + civName + " з поселенцем та воїном");

            // Створюємо CivilizationAI контролер для цієї цивілізації
            CreateCivilizationAI(civName, civColor, manager);
        }
        else
        {
            Debug.LogWarning("Не вдалося знайти позицію для AI цивілізації " + civName);
        }
    }

    public void CreateCivilizationAI(string civName, Color civColor, Program1 manager = null)
    {
        // Перевіряємо чи вже існує CivilizationAI для цієї цивілізації
        CivilizationAI[] existingAIs = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        foreach (CivilizationAI existingAI in existingAIs)
        {
            if (existingAI != null && existingAI.civilizationName == civName)
            {
                if (manager != null)
                    existingAI.BindToManager(manager);
                Debug.Log($"CivilizationAI для {civName} вже існує");
                return;
            }
        }

        // Створюємо новий GameObject з CivilizationAI
        GameObject aiObj = new GameObject(civName + "_AI");
        CivilizationAI newAI = aiObj.AddComponent<CivilizationAI>();
        newAI.SetCivilizationName(civName);
        if (manager != null)
            newAI.BindToManager(manager);
        Debug.Log($"Створено CivilizationAI для {civName}");
    }
    
    Vector3Int FindAISpawnPosition(int index, Program1 manager)
    {
        int width = manager != null ? manager.width : 80;
        int height = manager != null ? manager.height : 50;
        
        Vector3Int[] corners = {
            new Vector3Int(5, 5, 0),           // Верхній лівий
            new Vector3Int(width - 5, 5, 0),    // Верхній правий
            new Vector3Int(width / 2, height - 5, 0) // Нижній центр
        };
        
        if (index < corners.Length)
        {
            return manager.FindValidSpawnPosition(corners[index]);
        }
        
        return manager.FindValidSpawnPosition(new Vector3Int(width / 2, height / 2, 0));
    }
    
    Vector3Int FindValidPositionNear(Vector3Int center, Program1 manager)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int checkPos = center + new Vector3Int(x, y, 0);
                    if (checkPos != center && !manager.IsImpassable(checkPos) && manager.GetUnitAt(checkPos) == null)
                    {
                        return checkPos;
                    }
                }
            }
        }
        return center;
    }

    Vector3Int FindFreeLandPosition(Vector3Int center, Program1 manager)
    {
        if (manager == null) return Vector3Int.zero;

        // Спочатку пробуємо сам центр.
        if (!manager.IsImpassable(center) && manager.GetUnitAt(center) == null && manager.IsValidCitySite(center))
        {
            return center;
        }

        // Шукаємо найближчу вільну клітинку.
        for (int radius = 1; radius <= 20; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int checkPos = center + new Vector3Int(x, y, 0);
                    if (checkPos.x < 0 || checkPos.x >= manager.width || checkPos.y < 0 || checkPos.y >= manager.height)
                        continue;

                    if (!manager.IsImpassable(checkPos) && manager.GetUnitAt(checkPos) == null && manager.IsValidCitySite(checkPos))
                    {
                        return checkPos;
                    }
                }
            }
        }

        return Vector3Int.zero;
    }

    void ApplyUnitColor(GameObject obj, Color civColor)
    {
        if (obj == null) return;

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
                sr.color = civColor;
                sr.material.SetColor("_Color", civColor);
                sr.material.EnableKeyword("_EMISSION");
                sr.material.SetColor("_EmissionColor", civColor * 0.5f);
            }
        }

        UnitSetup.ApplySorting(obj.transform);
    }

    void CreateAICityDirectly(Program1 manager, Vector3Int pos, string civName, Color civColor)
    {
        if (manager == null || manager.cityPrefab == null || manager.HasCityAt(pos)) return;

        bool isCapital = manager.allCities.Find(c => c != null && c.ownerCivName == civName) == null;
        string generatedName = CityLabel.GenerateCityName(civName, isCapital);

        Vector3 worldPos = manager.tilemap.GetCellCenterWorld(pos);
        GameObject cityObj = Instantiate(manager.cityPrefab, new Vector3(worldPos.x, worldPos.y - 0.2f, -0.15f), Quaternion.identity);
        cityObj.name = civName + "_" + generatedName;

        City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
        city.gridPosition = pos;
        city.isPlayerCity = false;
        city.ownerCivName = civName;
        city.isCapital = isCapital;
        city.cityName = generatedName;
        city.Init(pos, manager.tilemap);
        city.SetupLabel(civName, civColor);

        ApplyUnitColor(cityObj, civColor);
        manager.RegisterCity(city);

        FogOfWarManager fog = manager.GetFogOfWar();
        if (fog != null)
            fog.RefreshVisibility();
    }
    
    GameObject CreateAIUnit(GameObject prefab, Vector3Int cellPos, string name, Color color, bool isPlayer)
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (prefab == null || manager == null) return null;
        
        Vector3 worldPos = manager.tilemap.GetCellCenterWorld(cellPos);
        GameObject obj = Instantiate(prefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
        obj.name = name;
        
        Unit u = obj.GetComponent<Unit>() ?? obj.AddComponent<Unit>();
        u.gridPosition = cellPos;
        u.isPlayer = isPlayer; // Важливо: це AI юніт
        
        // Додаємо колайдер якщо його немає
        if (obj.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            
            // Налаштовуємо розмір колайдера
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                collider.size = sr.bounds.size;
            }
            else
            {
                collider.size = new Vector2(1f, 1f);
            }
        }
        
        // Додаємо AI компонент
        UnitAI ai = obj.GetComponent<UnitAI>() ?? obj.AddComponent<UnitAI>();
        
        // Додаємо анімації до AI юніта
        UnitAnimator animator = obj.GetComponent<UnitAnimator>() ?? obj.AddComponent<UnitAnimator>();
        
        // Фарбуємо юніт
        manager.Colorize(obj);
        
        // Додаємо до списку всіх юнітів
        manager.allUnits.Add(u);
        manager.RegisterUnitCell(u, cellPos);
        
        return obj; // Повертаємо створений об'єкт
    }
    
    public void DeclareWar(string targetCiv = null)
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (string.IsNullOrEmpty(targetCiv) && manager != null)
            targetCiv = manager.pendingWarTargetCiv;

        if (string.IsNullOrEmpty(targetCiv))
        {
            Debug.LogWarning("DeclareWar: ціль не вказана.");
            return;
        }

        string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");
        DeclareWarBetween(playerCiv, targetCiv, playerCiv);

        if (manager != null)
        {
            manager.pendingWarTargetCiv = "";
            manager.HideWarButton();
        }
    }

    public void DeclareWarBetween(string civA, string civB, string aggressorCiv = null)
    {
        if (string.IsNullOrEmpty(civA) || string.IsNullOrEmpty(civB) || civA == civB)
            return;

        if (eliminatedCivs.Contains(civA) || eliminatedCivs.Contains(civB))
            return;

        string warKey = MakeWarKey(civA, civB);
        if (activeWarPairs.Contains(warKey))
            return;

        activeWarPairs.Add(warKey);

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");

        if (civA == playerCiv && !enemyNations.Contains(civB))
            enemyNations.Add(civB);
        if (civB == playerCiv && !enemyNations.Contains(civA))
            enemyNations.Add(civA);

        SyncWarFlags(manager);
        Debug.Log("Війна: " + civA + " проти " + civB);

        if (civA == playerCiv || civB == playerCiv)
            SaveManager.Instance?.MarkUnsaved();

        if (aggressorCiv != null && aggressorCiv != playerCiv && manager != null)
        {
            if (civA == playerCiv || civB == playerCiv)
            {
                if (GameUI.Instance != null)
                    GameUI.Instance.ShowWarDeclaredBy(aggressorCiv);
            }
        }
    }

    static string MakeWarKey(string civA, string civB)
    {
        return string.CompareOrdinal(civA, civB) < 0 ? civA + "|" + civB : civB + "|" + civA;
    }

    void SyncWarFlags(Program1 manager)
    {
        isAtWar = enemyNations.Count > 0;
        if (manager != null)
            manager.isAtWar = isAtWar;
    }

    public bool AreAtWar(string civA, string civB)
    {
        if (string.IsNullOrEmpty(civA) || string.IsNullOrEmpty(civB) || civA == civB)
            return false;

        return activeWarPairs.Contains(MakeWarKey(civA, civB));
    }

    public bool IsCivAtWar(string civName, Program1 manager)
    {
        if (string.IsNullOrEmpty(civName) || manager == null)
            return false;

        string playerCiv = manager.currentCivName;
        if (AreAtWar(civName, playerCiv))
            return true;

        foreach (string other in manager.GetLivingCivNames())
        {
            if (other == civName)
                continue;

            if (AreAtWar(civName, other))
                return true;
        }

        return false;
    }

    public bool IsCivEliminated(string civName)
    {
        return !string.IsNullOrEmpty(civName) && eliminatedCivs.Contains(civName);
    }

    public bool IsCivEngagedInWar(string civName, Program1 manager)
    {
        if (string.IsNullOrEmpty(civName) || manager == null)
            return false;

        foreach (string other in manager.GetLivingCivNames())
        {
            if (other == civName)
                continue;

            if (AreAtWar(civName, other))
                return true;
        }

        return false;
    }

    public bool CanAiDeclareNewWar(string civName, Program1 manager)
    {
        return !IsCivEngagedInWar(civName, manager);
    }

    public void RollAiWarDeclarations(Program1 manager)
    {
        if (manager == null)
            return;

        List<string> aiCivs = manager.GetLivingCivNames();
        string playerCiv = manager.currentCivName;
        aiCivs.Remove(playerCiv);

        foreach (string aiCiv in aiCivs)
        {
            if (IsCivEliminated(aiCiv))
                continue;

            if (!CanAiDeclareNewWar(aiCiv, manager))
                continue;

            var targets = new List<string>();
            foreach (string other in manager.GetLivingCivNames())
            {
                if (other == aiCiv || IsCivEliminated(other))
                    continue;

                if (AreAtWar(aiCiv, other))
                    continue;

                targets.Add(other);
            }

            if (targets.Count == 0)
                continue;

            if (Random.value >= aiVsAiWarChance)
                continue;

            string target = targets[Random.Range(0, targets.Count)];
            DeclareWarBetween(aiCiv, target, aiCiv);
        }
    }

    public void RollAiVsAiWars(Program1 manager) => RollAiWarDeclarations(manager);

    public void CheckCivElimination(string civName)
    {
        if (string.IsNullOrEmpty(civName) || eliminatedCivs.Contains(civName))
            return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null)
            return;

        if (manager.CountCivUnits(civName) > 0 || manager.CountCivCities(civName) > 0)
            return;

        EliminateCiv(civName, manager);
    }

    void EliminateCiv(string civName, Program1 manager)
    {
        eliminatedCivs.Add(civName);
        enemyNations.Remove(civName);

        var warsToRemove = new List<string>();
        foreach (string key in activeWarPairs)
        {
            string[] parts = key.Split('|');
            if (parts.Length == 2 && (parts[0] == civName || parts[1] == civName))
                warsToRemove.Add(key);
        }
        foreach (string key in warsToRemove)
            activeWarPairs.Remove(key);

        CivilizationAI[] aiControllers = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        foreach (CivilizationAI ai in aiControllers)
        {
            if (ai != null && ai.civilizationName == civName)
                Destroy(ai.gameObject);
        }

        SyncWarFlags(manager);
        Debug.Log("Цивілізацію знищено: " + civName);

        if (civName == manager.currentCivName)
        {
            TurnManager turnManager = TurnManager.Instance;
            if (turnManager != null)
                turnManager.SetPlayerDefeated();
            if (GameUI.Instance != null)
                GameUI.Instance.ShowDefeatMessage();
        }
    }

    public bool IsAtWarWith(string civName)
    {
        if (string.IsNullOrEmpty(civName) || civName == "Unknown" || civName == "Player")
            return false;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");
        return AreAtWar(playerCiv, civName);
    }

    // Універсальний метод для UI кнопки "Declare war".
    // Можна безпечно прив'язати в Inspector саме цей метод.
    public void OnWarButtonPressed()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null)
        {
            return;
        }

        string targetCiv = manager.pendingWarTargetCiv;
        if (string.IsNullOrEmpty(targetCiv))
        {
            // fallback: якщо pending-ціль не встановлена, пробуємо старий обробник Program1
            manager.DeclareWarOnSelectedCity();
            return;
        }

        DeclareWar(targetCiv);
    }
    
    public IEnumerator AITakeTurn()
    {
        Debug.Log("AI починає свій хід...");
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null) yield break;

        // Отримуємо всі AI юніти
        List<Unit> aiUnits = manager.allUnits.FindAll(u => !u.isPlayer);
        
        // Кожен AI хід має починатися зі скидання руху.
        // Інакше юніти витратять рух один раз і стоятимуть AFK.
        foreach (Unit unit in aiUnits)
        {
            if (unit != null)
            {
                unit.ResetMovement();
            }
        }
        
        Debug.Log("AI хід: " + aiUnits.Count + " AI юнітів");
        
        foreach (Unit unit in aiUnits)
        {
            if (unit == null || unit.currentMovement <= 0) continue;

            // AI атакує гравця лише якщо його цивілізація в стані війни з гравцем.
            string aiCiv = unit.GetCivName(manager);
            if (IsAtWarWith(aiCiv))
            {
                Unit playerTarget = FindNearestPlayerUnit(unit, manager);
                if (playerTarget != null)
                {
                    List<Vector3Int> huntPath = manager.FindPath(unit.gridPosition, playerTarget.gridPosition);
                    if (huntPath != null && huntPath.Count > 0)
                    {
                        int maxMove = Mathf.Min(huntPath.Count, unit.currentMovement);
                        for (int i = 0; i < maxMove; i++)
                        {
                            yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { huntPath[i] }, manager.tilemap, manager));
                            if (unit == null || unit.currentMovement <= 0) break;
                        }
                    }
                }
            }
            
            // Перевіряємо чи це поселенець
            if (unit.name.Contains("Settler"))
            {
                // Автоматично засновуємо місто якщо це хороше місце
                if (unit.IsGoodCityLocation(unit.gridPosition, manager))
                {
                    Debug.Log("AI поселенець засновує місто");
                    unit.CreateCity();
                    continue;
                }
            }
            
            // Використовуємо UnitAI для руху
            UnitAI ai = unit.GetComponent<UnitAI>();
            if (ai != null)
            {
                int movementBefore = unit.currentMovement;
                yield return StartCoroutine(ai.TakeTurn());

                // Fallback: якщо AI не зміг виконати рух/дію, робимо простий крок.
                if (unit != null && unit.currentMovement == movementBefore && unit.currentMovement > 0)
                {
                    yield return StartCoroutine(FallbackMove(unit, manager));
                }
            }
            else
            {
                yield return StartCoroutine(FallbackMove(unit, manager));
            }
            
            yield return new WaitForSeconds(0.1f); // Невелика затримка між діями
        }
        
        Debug.Log("AI завершив свій хід");
    }

    Unit FindNearestPlayerUnit(Unit fromUnit, Program1 manager)
    {
        if (fromUnit == null || manager == null) return null;

        Unit nearest = null;
        float bestDistance = float.MaxValue;
        foreach (Unit u in manager.allUnits.ToArray())
        {
            if (u == null || !u.isPlayer) continue;

            float distance = manager.GetHexDistance(fromUnit.gridPosition, u.gridPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = u;
            }
        }

        return nearest;
    }

    IEnumerator FallbackMove(Unit unit, Program1 manager)
    {
        if (unit == null || manager == null || unit.currentMovement <= 0) yield break;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3Int randomPos = unit.gridPosition + new Vector3Int(Random.Range(-2, 3), Random.Range(-2, 3), 0);
            if (manager.IsImpassable(randomPos) || manager.GetUnitAt(randomPos) != null) continue;

            List<Vector3Int> path = manager.FindPath(unit.gridPosition, randomPos);
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, unit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { path[i] }, manager.tilemap, manager));
                }
                yield break;
            }
        }
    }
    
    // Нові методи для сумісності з CivilizationAI
    public void NextTurn()
    {
        // Оновлюємо дипломатичний стан
        Debug.Log("Дипломатичний хід завершено");
    }
    
    public bool CanDeclareWar(string civilization1, string civilization2)
    {
        return !AreAtWar(civilization1, civilization2);
    }

    public void DeclareWar(string civilization1, string civilization2)
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");
        string aggressor = civilization1 == playerCiv || civilization2 == playerCiv ? playerCiv : civilization1;
        DeclareWarBetween(civilization1, civilization2, aggressor);
    }

    public bool IsAtWar(string civilization1, string civilization2)
    {
        return AreAtWar(civilization1, civilization2);
    }

    public void OnUnitDestroyed(string attackerCiv, string defenderCiv)
    {
        Debug.Log($"Юніт знищено: {attackerCiv} атакував {defenderCiv}");
        CheckCivElimination(defenderCiv);
    }

    public void OnCityCaptured(string attackerCiv, string defenderCiv)
    {
        Debug.Log($"Місто знищено: {attackerCiv} проти {defenderCiv}");
        CheckCivElimination(defenderCiv);
    }
    
    public void OnTradeCompleted(string civ1, string civ2)
    {
        // Обробляємо завершення торгів
        Debug.Log($"Торгівля завершена: {civ1} та {civ2}");
    }
    
    public void OnBorderConflict(string civ1, string civ2)
    {
        // Обробляємо прикордонний конфлікт
        Debug.Log($"Прикордонний конфлікт: {civ1} та {civ2}");
    }

    public void ExportWarState(out List<string> warPairs, out List<string> eliminated)
    {
        warPairs = new List<string>(activeWarPairs);
        eliminated = new List<string>(eliminatedCivs);
    }

    public void ImportWarState(List<string> warPairs, List<string> eliminated, List<string> playerEnemies, bool atWar, Program1 manager, string playerCiv)
    {
        activeWarPairs.Clear();
        eliminatedCivs.Clear();
        enemyNations = playerEnemies ?? new List<string>();

        if (eliminated != null)
        {
            foreach (string civ in eliminated)
            {
                if (!string.IsNullOrEmpty(civ))
                    eliminatedCivs.Add(civ);
            }
        }

        if (warPairs != null && warPairs.Count > 0)
        {
            foreach (string key in warPairs)
            {
                if (!string.IsNullOrEmpty(key))
                    activeWarPairs.Add(key);
            }
        }
        else if (playerEnemies != null && !string.IsNullOrEmpty(playerCiv))
        {
            foreach (string enemy in playerEnemies)
            {
                if (!string.IsNullOrEmpty(enemy))
                    activeWarPairs.Add(MakeWarKey(playerCiv, enemy));
            }
        }

        SyncWarFlags(manager);
    }

    public void RestoreAiControllersFromSave(Program1 manager)
    {
        if (manager == null)
        {
            AiSpawnComplete = true;
            return;
        }

        var civs = new HashSet<string>();
        foreach (City city in manager.allCities)
        {
            if (city == null || string.IsNullOrEmpty(city.ownerCivName))
                continue;
            if (city.ownerCivName != manager.currentCivName && !IsCivEliminated(city.ownerCivName))
                civs.Add(city.ownerCivName);
        }

        foreach (Unit unit in manager.allUnits)
        {
            if (unit == null || unit.isPlayer)
                continue;

            string civName = unit.GetCivName(manager);
            if (!string.IsNullOrEmpty(civName) && civName != manager.currentCivName && !IsCivEliminated(civName))
                civs.Add(civName);
        }

        foreach (CivilizationAI ai in Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None))
        {
            if (ai != null)
                Destroy(ai.gameObject);
        }

        foreach (string civName in civs)
            CreateCivilizationAI(civName, GetCivColor(civName), manager);

        AiSpawnComplete = true;
    }

    public void MakePeace(string targetCiv)
    {
        if (string.IsNullOrEmpty(targetCiv))
            return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");

        activeWarPairs.Remove(MakeWarKey(playerCiv, targetCiv));
        enemyNations.Remove(targetCiv);
        SyncWarFlags(manager);

        if (manager != null)
            manager.pendingWarTargetCiv = "";

        SaveManager.Instance?.MarkUnsaved();
        Debug.Log("Оголошено мир з " + targetCiv);
    }
}
