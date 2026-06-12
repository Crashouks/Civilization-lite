using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DiplomacyManager : MonoBehaviour
{
    public static DiplomacyManager Instance;
    
    public bool isAtWar = false;
    public List<string> enemyNations = new List<string>();
    
    [Header("Налаштування AI цивілізацій")]
    public GameObject[] civPrefabs; // Префаби для різних цивілізацій
    public Dictionary<string, Color> civColors = new Dictionary<string, Color>(); // Кольори для кожної цивілізації
    public string[] civNames = { "Rome", "America", "Egypt", "Scythia" };
    
    [Header("Стартові позиції AI")]
    public Vector3Int[] aiSpawnPositions = new Vector3Int[3]; // 3 позиції для AI

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
            Debug.Log("Пропускаємо спавн AI — гра завантажена зі збереження");
            yield break;
        }

        if (manager == null)
        {
            Debug.LogError("Program1 не знайдено для спавну AI!");
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

        // ВИМКНЕНО - AI тепер керується через кнопку "Наступний хід" в Program1
        // yield return new WaitForSeconds(1f);
        // StartCoroutine(AITakeTurn());
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
            CreateCivilizationAI(civName, civColor);
        }
        else
        {
            Debug.LogWarning("Не вдалося знайти позицію для AI цивілізації " + civName);
        }
    }

    void CreateCivilizationAI(string civName, Color civColor)
    {
        // Перевіряємо чи вже існує CivilizationAI для цієї цивілізації
        CivilizationAI[] existingAIs = Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None);
        foreach (CivilizationAI existingAI in existingAIs)
        {
            if (existingAI != null && existingAI.civilizationName == civName)
            {
                Debug.Log($"CivilizationAI для {civName} вже існує");
                return;
            }
        }

        // Створюємо новий GameObject з CivilizationAI
        GameObject aiObj = new GameObject(civName + "_AI");
        CivilizationAI newAI = aiObj.AddComponent<CivilizationAI>();
        newAI.SetCivilizationName(civName);
        Debug.Log($"Створено CivilizationAI для {civName}");
    }
    
    Vector3Int FindAISpawnPosition(int index, Program1 manager)
    {
        // Розміщуємо AI цивілізації в різних кутках карти
        int width = 80;
        int height = 50;
        
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
        if (!manager.IsImpassable(center) && manager.GetUnitAt(center) == null && !manager.HasCityAt(center))
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

                    if (!manager.IsImpassable(checkPos) && manager.GetUnitAt(checkPos) == null && !manager.HasCityAt(checkPos))
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
        if (fog != null) fog.RevealAllPlayerUnits();
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
            
            Debug.Log("Додано колайдер до AI юніта: " + name);
        }
        
        // Додаємо AI компонент
        UnitAI ai = obj.GetComponent<UnitAI>() ?? obj.AddComponent<UnitAI>();
        
        // Додаємо анімації до AI юніта
        UnitAnimator animator = obj.GetComponent<UnitAnimator>() ?? obj.AddComponent<UnitAnimator>();
        
        // Фарбуємо юніт
        manager.Colorize(obj);
        
        // Додаємо до списку всіх юнітів
        manager.allUnits.Add(u);
        
        Debug.Log("Створено AI юніт: " + name + " на позиції " + cellPos + " з анімаціями");
        
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

        if (!enemyNations.Contains(targetCiv))
            enemyNations.Add(targetCiv);

        isAtWar = enemyNations.Count > 0;
        Debug.Log("Війна оголошена проти: " + targetCiv);

        if (manager != null)
        {
            manager.isAtWar = isAtWar;
            manager.pendingWarTargetCiv = "";
            manager.HideWarButton();
        }
    }

    public bool IsAtWarWith(string civName)
    {
        if (string.IsNullOrEmpty(civName) || civName == "Unknown" || civName == "Player")
            return false;
        return enemyNations.Contains(civName);
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
        foreach (Unit u in manager.allUnits)
        {
            if (u == null || !u.isPlayer) continue;

            float distance = Vector3Int.Distance(fromUnit.gridPosition, u.gridPosition);
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
        // Перевіряємо, чи можна оголосити війну
        return !isAtWar;
    }
    
    public void DeclareWar(string civilization1, string civilization2)
    {
        DeclareWar(civilization2);
    }
    
    public bool IsAtWar(string civilization1, string civilization2)
    {
        return isAtWar;
    }
    
    public void OnUnitDestroyed(string attackerCiv, string defenderCiv)
    {
        // Обробляємо знищення юніта
        Debug.Log($"Юніт знищено: {attackerCiv} атакував {defenderCiv}");
    }
    
    public void OnCityCaptured(string attackerCiv, string defenderCiv)
    {
        // Обробляємо захоплення міста
        Debug.Log($"Місто захоплено: {attackerCiv} захопив місто {defenderCiv}");
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
}
