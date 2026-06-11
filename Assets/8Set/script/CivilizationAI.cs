using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CivilizationAI : MonoBehaviour
{
    [Header("Налаштування Цивілізації")]
    public string civilizationName = "AI Civilization";
    public Color civilizationColor = Color.red;
    
    // Зроблено поля публічними для доступу з інших скриптів
    [Header("Стратегічні параметри")]
    public float aggressionLevel = 0.5f; // 0 = мирний, 1 = агресивний
    public float expansionPriority = 0.7f; // Пріоритет розширення
    public float warDeclarationChance = 0.3f; // Шанс оголосити війну за хід
    
    // Зроблено публічним для доступу з UnitAI
    public bool isAtWar = false;
    public GameObject settlerPrefab;
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    
    private Program1 mapManager;
    private List<Unit> aiUnits = new List<Unit>();
    private List<City> aiCities = new List<City>();
    private Program1 playerCivilization;
    private int turnCounter = 0;
    private bool isProcessingTurn = false; // Прапорець для відстеження стану ходу
    
    void Start()
    {
        mapManager = Object.FindAnyObjectByType<Program1>();
        playerCivilization = Object.FindAnyObjectByType<Program1>();

        // Більше не запускаємо автоматичний цикл - чекаємо кнопки "Наступний хід"

        // Запускаємо затримку для заповнення юнітів після спавну
        StartCoroutine(DelayedPopulateUnits());
    }

    IEnumerator DelayedPopulateUnits()
    {
        // Чекаємо 3 секунди щоб DiplomacyManager спавнив юнітів
        yield return new WaitForSeconds(3f);
        PopulateAIUnits();
    }

    public void SetCivilizationName(string name)
    {
        civilizationName = name;
        civilizationColor = GetCivColor(name);
    }

    Color GetCivColor(string civName)
    {
        switch (civName)
        {
            case "Rome": return Color.red;
            case "America": return Color.blue;
            case "Egypt": return Color.yellow;
            case "Scythia": return Color.green;
            default: return Color.gray;
        }
    }

    void PopulateAIUnits()
    {
        if (mapManager == null) return;

        aiUnits.Clear();
        aiCities.Clear();

        // Додаємо юнітів цієї цивілізації
        foreach (Unit unit in mapManager.allUnits)
        {
            if (unit != null && !unit.isPlayer)
            {
                // Перевіряємо чи цей юніт належить до нашої цивілізації
                if (unit.name.Contains(civilizationName))
                {
                    aiUnits.Add(unit);
                }
            }
        }

        // Додаємо міста цієї цивілізації
        foreach (City city in mapManager.allCities)
        {
            if (city != null && city.ownerCivName == civilizationName)
            {
                aiCities.Add(city);
            }
        }

        Debug.Log($"{civilizationName}: всього {aiUnits.Count} AI юнітів, {aiCities.Count} міст");
    }

    public void ExecuteAITurn()
    {
        Debug.Log($"{civilizationName}: ExecuteAITurn викликано");
        isProcessingTurn = true;
        // Оновлюємо список юнітів та міст перед ходом
        PopulateAIUnits();
        StartCoroutine(ExecuteAITurnCoroutine());
    }

    public bool IsProcessingTurn()
    {
        return isProcessingTurn;
    }

    IEnumerator ExecuteAITurnCoroutine()
    {
        Debug.Log($"=== ХІД ЦИВІЛІЗАЦІЇ {civilizationName} (Хід {turnCounter}) ===");

        // 1. Перевіряємо дипломатичний стан
        yield return StartCoroutine(ManageDiplomacy());

        // 2. Створюємо нових юнітів
        yield return StartCoroutine(ManageUnitProduction());

        // 3. Керуємо існуючими юнітами
        yield return StartCoroutine(ManageUnits());

        // 4. Засновуємо нові міста
        yield return StartCoroutine(ManageExpansion());

        // 5. Надаємо дипломатичній системі знати про наступний хід
        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy != null)
        {
            diplomacy.NextTurn();
        }

        turnCounter++;
        isProcessingTurn = false;
        Debug.Log($"=== ХІД {civilizationName} ЗАВЕРШЕНО ===");
    }

    IEnumerator ManageDiplomacy()
    {
        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        
        if (diplomacy != null)
        {
            string playerName = playerCivilization != null ? playerCivilization.currentCivName : "Player";
            
            // Перевіряємо, чи можемо оголосити війну
            if (!isAtWar && diplomacy.CanDeclareWar(civilizationName, playerName))
            {
                if (Random.value < warDeclarationChance)
                {
                    diplomacy.DeclareWar(civilizationName, playerName);
                    isAtWar = true;
                    Debug.Log($"{civilizationName} оголошує війну!");
                }
            }
            
            // Перевіряємо поточний статус
            isAtWar = diplomacy.IsAtWar(civilizationName, playerName);
        }
        else if (!isAtWar && Random.value < warDeclarationChance)
        {
            // Резервна логіка, якщо дипломатична система недоступна
            isAtWar = true;
            Debug.Log($"{civilizationName} оголошує війну!");
            
            if (playerCivilization != null)
            {
                playerCivilization.isAtWar = true;
            }
        }
        
        yield return null;
    }
    
    IEnumerator ManageUnitProduction()
    {
        // Створюємо юнітів у містах
        foreach (City city in aiCities)
        {
            if (city != null)
            {
                // Вирішуємо, якого юніта створити
                GameObject unitToCreate = DecideUnitToCreate();
                
                if (unitToCreate != null && Random.value < 0.4f) // 40% шанс створити юніта
                {
                    Vector3Int spawnPos = FindValidPositionNearCity(city.gridPosition);
                    if (spawnPos != Vector3Int.zero)
                    {
                        CreateAIUnit(unitToCreate, spawnPos);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
        }
        
        // Якщо немає міст, створюємо стартові юніти
        if (aiCities.Count == 0 && aiUnits.Count < 3)
        {
            CreateStartingUnits();
        }
        
        yield return null;
    }
    
    GameObject DecideUnitToCreate()
    {
        if (isAtWar)
        {
            // Під час війни створюємо більше військових
            return Random.value < 0.7f ? warriorPrefab : archerPrefab;
        }
        else
        {
            // У мирний час пріоритет розширення
            return Random.value < expansionPriority ? settlerPrefab : warriorPrefab;
        }
    }
    
    IEnumerator ManageUnits()
    {
        Debug.Log($"{civilizationName}: керуємо {aiUnits.Count} юнітами");

        if (aiUnits.Count == 0)
        {
            Debug.LogWarning($"{civilizationName}: немає юнітів для керування!");
            yield break;
        }

        foreach (Unit unit in aiUnits)
        {
            if (unit != null)
            {
                Debug.Log($"{civilizationName}: юніт {unit.name} має {unit.currentMovement} ходів");

                // Юніт рухається поки не вичерпає всі ходи
                while (unit.currentMovement > 0)
                {
                    Debug.Log($"{civilizationName}: {unit.name} рухається, залишилось ходів: {unit.currentMovement}");
                    yield return StartCoroutine(ControlUnit(unit));

                    if (unit.currentMovement <= 0)
                    {
                        Debug.Log($"{civilizationName}: {unit.name} вичерпав всі ходи");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"{civilizationName}: юніт null");
            }
        }

        // Очищуємо список від мертвих юнітів
        aiUnits.RemoveAll(u => u == null);
        Debug.Log($"{civilizationName}: завершено керування юнітами");
        yield return null;
    }
    
    IEnumerator ControlUnit(Unit unit)
    {
        // Завжди використовуємо BasicUnitControl для гарантованого руху
        yield return StartCoroutine(BasicUnitControl(unit));
    }
    
    IEnumerator BasicUnitControl(Unit unit)
    {
        Debug.Log($"{civilizationName}: BasicUnitControl для {unit.name}, позиція: {unit.gridPosition}");

        // Перевіряємо чи ми у стані війни
        if (CheckIfAtWarWithPlayer())
        {
            Debug.Log($"{civilizationName}: у стані війни, атакуємо гравця");
            yield return StartCoroutine(MoveToAttackPlayer(unit));
        }
        else
        {
            Debug.Log($"{civilizationName}: мирний час, випадковий рух");
            yield return StartCoroutine(RandomMovement(unit));
        }

        yield return null;
    }

    IEnumerator MoveToAttackPlayer(Unit unit)
    {
        // Перевіряємо чи є ворожий юніт поруч для атаки
        Unit adjacentEnemy = FindAdjacentEnemy(unit);
        if (adjacentEnemy != null)
        {
            Debug.Log($"{civilizationName}: {unit.name} атакує {adjacentEnemy.name}");

            // Розвертаємо юніт до ворога перед атакою
            UnitAnimator anim = unit.GetComponent<UnitAnimator>();
            if (anim != null)
            {
                Vector3 enemyPos = mapManager.tilemap.GetCellCenterWorld(adjacentEnemy.gridPosition);
                enemyPos.y -= 1f;
                anim.FaceToward(enemyPos - unit.transform.position);
            }

            yield return StartCoroutine(unit.JumpAttack(adjacentEnemy, mapManager));
            yield break;
        }

        // Знаходимо найближчий юніт або місто гравця
        Unit nearestPlayerUnit = FindNearestPlayerUnit(unit);
        City nearestPlayerCity = FindNearestPlayerCity(unit);

        Vector3Int targetPos;
        if (nearestPlayerUnit != null)
        {
            targetPos = nearestPlayerUnit.gridPosition;
            Debug.Log($"{civilizationName}: ціль - юніт гравця на {targetPos}");
        }
        else if (nearestPlayerCity != null)
        {
            targetPos = nearestPlayerCity.gridPosition;
            Debug.Log($"{civilizationName}: ціль - місто гравця на {targetPos}");
        }
        else
        {
            // Якщо немає цілей, рухаємося до центру карти
            targetPos = new Vector3Int(40, 25, 0);
            Debug.Log($"{civilizationName}: немає цілей, рухаємося до центру");
        }

        // Рухаємося до цілі
        Vector3Int nextStep = GetNextStepTowards(unit.gridPosition, targetPos);
        if (nextStep != unit.gridPosition)
        {
            Debug.Log($"{civilizationName}: рухаємо {unit.name} до {nextStep}");
            yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { nextStep }, mapManager.tilemap, mapManager));
        }
    }

    Unit FindAdjacentEnemy(Unit aiUnit)
    {
        Vector3Int[] adjacentPositions = {
            aiUnit.gridPosition + new Vector3Int(1, 0, 0),
            aiUnit.gridPosition + new Vector3Int(-1, 0, 0),
            aiUnit.gridPosition + new Vector3Int(0, 1, 0),
            aiUnit.gridPosition + new Vector3Int(0, -1, 0)
        };

        foreach (Vector3Int pos in adjacentPositions)
        {
            Unit enemy = mapManager.GetUnitAt(pos);
            if (enemy != null && enemy.isPlayer)
            {
                return enemy;
            }
        }

        return null;
    }

    IEnumerator RandomMovement(Unit unit)
    {
        // Використовуємо оригінальний метод руху з анімацією
        Vector3Int[] directions = {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };

        Vector3Int randomDir = directions[Random.Range(0, directions.Length)];
        Vector3Int targetPos = unit.gridPosition + randomDir;

        if (!mapManager.IsImpassable(targetPos) && mapManager.GetUnitAt(targetPos) == null)
        {
            Debug.Log($"{civilizationName}: рухаємо {unit.name} до {targetPos} з анімацією");
            yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { targetPos }, mapManager.tilemap, mapManager));
        }
    }

    Unit FindNearestPlayerUnit(Unit aiUnit)
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Unit unit in mapManager.allUnits)
        {
            if (unit != null && unit.isPlayer)
            {
                float dist = Vector3Int.Distance(aiUnit.gridPosition, unit.gridPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }
        }

        return nearest;
    }

    City FindNearestPlayerCity(Unit aiUnit)
    {
        City nearest = null;
        float nearestDist = float.MaxValue;

        foreach (City city in mapManager.allCities)
        {
            if (city != null && city.isPlayerCity)
            {
                float dist = Vector3Int.Distance(aiUnit.gridPosition, city.gridPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = city;
                }
            }
        }

        return nearest;
    }

    Vector3Int GetNextStepTowards(Vector3Int from, Vector3Int to)
    {
        Vector3Int direction = to - from;
        direction.x = Mathf.Clamp(direction.x, -1, 1);
        direction.y = Mathf.Clamp(direction.y, -1, 1);

        Vector3Int nextPos = from + direction;

        // Якщо напрямок заблокований, пробуємо інші варіанти
        if (mapManager.IsImpassable(nextPos) || mapManager.GetUnitAt(nextPos) != null)
        {
            Vector3Int[] alternatives = {
                from + new Vector3Int(direction.x, 0, 0),
                from + new Vector3Int(0, direction.y, 0),
                from + new Vector3Int(-direction.y, direction.x, 0),
                from + new Vector3Int(direction.y, -direction.x, 0)
            };

            foreach (Vector3Int alt in alternatives)
            {
                if (!mapManager.IsImpassable(alt) && mapManager.GetUnitAt(alt) == null)
                {
                    return alt;
                }
            }

            return from; // Не можемо рухатися
        }

        return nextPos;
    }

    bool CheckIfAtWarWithPlayer()
    {
        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy != null)
        {
            string playerName = playerCivilization != null ? playerCivilization.currentCivName : "Player";
            return diplomacy.IsAtWar(civilizationName, playerName);
        }
        return isAtWar;
    }

    Unit FindNearestPlayerEnemy(Unit unit)
    {
        Unit nearest = null;
        float minDistance = float.MaxValue;

        foreach (Unit u in mapManager.allUnits)
        {
            // Шукаємо тільки юнітів гравця
            if (u != null && u != unit && u.isPlayer)
            {
                float distance = Vector3Int.Distance(unit.gridPosition, u.gridPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = u;
                }
            }
        }

        return nearest;
    }
    
    IEnumerator MoveToAttack(Unit unit, Unit enemy)
    {
        if (unit == null || enemy == null) yield break;

        List<Vector3Int> path = mapManager.FindPath(unit.gridPosition, enemy.gridPosition);

        if (path != null && path.Count > 0)
        {
            // Рухаємося до ворога
            int maxMove = Mathf.Min(path.Count, unit.currentMovement);
            for (int i = 0; i < maxMove; i++)
            {
                // Перевіряємо чи юніт ще існує перед рухом
                if (unit == null || unit.gameObject == null) yield break;

                yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));

                // Перевіряємо, чи можемо атакувати
                if (unit != null && enemy != null && Vector3Int.Distance(unit.gridPosition, enemy.gridPosition) <= 1)
                {
                    AttackUnit(unit, enemy);
                    break;
                }
            }
        }

        yield return null;
    }
    
    IEnumerator FindCityLocation(Unit settler)
    {
        if (settler == null) yield break;

        // Шукаємо хороше місце для міста
        Vector3Int bestLocation = FindBestCityLocation(settler.gridPosition);

        if (bestLocation != Vector3Int.zero)
        {
            List<Vector3Int> path = mapManager.FindPath(settler.gridPosition, bestLocation);

            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, settler.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    // Перевіряємо чи поселенець ще існує
                    if (settler == null || settler.gameObject == null) yield break;

                    yield return StartCoroutine(settler.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }

                // Якщо дійшли до місця, засновуємо місто
                if (settler != null && settler.gridPosition == bestLocation)
                {
                    CreateAICity(settler);
                }
            }
        }

        yield return null;
    }
    
    IEnumerator Patrol(Unit unit)
    {
        // Випадкове патрулювання
        Vector3Int randomPos = unit.gridPosition + new Vector3Int(Random.Range(-3, 4), Random.Range(-3, 4), 0);
        
        if (!mapManager.IsImpassable(randomPos) && mapManager.GetUnitAt(randomPos) == null)
        {
            List<Vector3Int> path = mapManager.FindPath(unit.gridPosition, randomPos);
            
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, unit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }
            }
        }
        
        yield return null;
    }
    
    IEnumerator ManageExpansion()
    {
        // Засновуємо міста для всіх поселенців
        foreach (Unit unit in aiUnits)
        {
            if (unit != null && unit.name.Contains("Settler"))
            {
                // Поселенці завжди засновують міста
                if (!mapManager.HasCityAt(unit.gridPosition))
                {
                    Debug.Log($"{civilizationName}: поселенець {unit.name} засновує місто");
                    CreateAICity(unit);
                }
            }
        }

        yield return null;
    }
    
    void CreateStartingUnits()
    {
        // Знаходимо хороше місце для старту
        Vector3Int center = new Vector3Int(mapManager.width / 2 + Random.Range(-20, 21), 
                                         mapManager.height / 2 + Random.Range(-20, 21), 0);
        Vector3Int spawnPos = mapManager.FindValidSpawnPosition(center);
        
        if (spawnPos != Vector3Int.zero)
        {
            // Створюємо поселенця
            CreateAIUnit(settlerPrefab, spawnPos);
            
            // Створюємо воїна поруч
            Vector3Int warriorPos = mapManager.FindValidSpawnPosition(spawnPos + new Vector3Int(1, 0, 0));
            if (warriorPos != Vector3Int.zero)
            {
                CreateAIUnit(warriorPrefab, warriorPos);
            }
        }
    }
    
    void CreateAIUnit(GameObject prefab, Vector3Int position)
    {
        if (prefab == null) return;
        
        Vector3 worldPos = mapManager.tilemap.GetCellCenterWorld(position);
        GameObject obj = Instantiate(prefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
        
        Unit unit = obj.GetComponent<Unit>() ?? obj.AddComponent<Unit>();
        unit.gridPosition = position;
        unit.isPlayer = false; // Це AI юніт
        
        // Додаємо AI компонент
        UnitAI ai = obj.GetComponent<UnitAI>() ?? obj.AddComponent<UnitAI>();
        
        // Додаємо компонент здоров'я
        UnitHealth health = obj.GetComponent<UnitHealth>() ?? obj.AddComponent<UnitHealth>();
        CombatSystem.UnitStats stats = GetUnitStatsForType(unit.name);
        health.maxHealth = stats.maxHealth;
        health.currentHealth = stats.maxHealth;
        
        // Налаштовуємо колір
        SetUnitColor(obj);
        
        // Додаємо до списку
        aiUnits.Add(unit);
        mapManager.allUnits.Add(unit);
        
        Debug.Log($"{civilizationName} створив юніта: {unit.name} на позиції {position}");
    }
    
    // Зроблено методи публічними для доступу з інших скриптів
    public void CreateAICity(Unit settler)
    {
        if (settler == null) return;
        
        // Створюємо місто
        GameObject cityObj = Instantiate(mapManager.cityPrefab, 
            mapManager.tilemap.GetCellCenterWorld(settler.gridPosition), 
            Quaternion.identity);
        
        City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
        city.gridPosition = settler.gridPosition;
        
        // Налаштовуємо місто
        SetUnitColor(cityObj);
        // city.isPlayerCity = false; // Закоментовано, оскільки поле не існує
        
        // Додаємо до списків
        aiCities.Add(city);
        mapManager.allCities.Add(city);
        mapManager.RegisterCity(city);
        
        // Видаляємо поселенця
        aiUnits.Remove(settler);
        mapManager.RemoveUnit(settler);
        Destroy(settler.gameObject);
        
        Debug.Log($"{civilizationName} заснував місто на позиції {city.gridPosition}");
    }
    
    void SetUnitColor(GameObject obj)
    {
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
                sr.color = civilizationColor;
                sr.material.SetColor("_Color", civilizationColor);
                sr.material.EnableKeyword("_EMISSION");
                sr.material.SetColor("_EmissionColor", civilizationColor * 0.5f);
            }
        }
    }
    
    Unit FindNearestEnemy(Unit unit)
    {
        Unit nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Unit u in mapManager.allUnits)
        {
            if (u != null && u != unit && u.isPlayer != unit.isPlayer)
            {
                float distance = Vector3Int.Distance(unit.gridPosition, u.gridPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = u;
                }
            }
        }
        
        return nearest;
    }
    
    void AttackUnit(Unit attacker, Unit defender)
    {
        CombatSystem combat = Object.FindAnyObjectByType<CombatSystem>();
        
        if (combat != null)
        {
            // Використовуємо бойову систему
            StartCoroutine(combat.ExecuteAttack(attacker, defender));
        }
        else
        {
            // Резервна проста логіка атаки
            int attackPower = attacker.name.Contains("Warrior") ? 10 : 6;
            int defensePower = defender.name.Contains("Warrior") ? 8 : 4;
            
            int damage = Mathf.Max(1, attackPower - defensePower + Random.Range(-2, 3));
            
            Debug.Log($"{attacker.name} атакує {defender.name} і завдає {damage} пошкоджень!");
            
            // В реальності тут була б система здоров'я
            // Для тесту просто видаляємо захисника
            mapManager.RemoveUnit(defender);
            aiUnits.Remove(defender);
            Destroy(defender.gameObject);
            
            // Скидаємо рух атакуючого
            attacker.currentMovement = 0;
        }
    }
    
    Vector3Int FindValidPositionNearCity(Vector3Int cityPos)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int checkPos = cityPos + new Vector3Int(x, y, 0);
                    if (!mapManager.IsImpassable(checkPos) && mapManager.GetUnitAt(checkPos) == null)
                    {
                        return checkPos;
                    }
                }
            }
        }
        return Vector3Int.zero;
    }
    
    Vector3Int FindBestCityLocation(Vector3Int currentPos)
    {
        Vector3Int bestPos = Vector3Int.zero;
        int bestScore = -1;
        
        for (int x = -5; x <= 5; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                Vector3Int checkPos = currentPos + new Vector3Int(x, y, 0);
                
                if (IsGoodCityLocation(checkPos))
                {
                    int score = EvaluateCityLocation(checkPos);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPos = checkPos;
                    }
                }
            }
        }
        
        return bestPos;
    }
    
    bool IsGoodCityLocation(Vector3Int pos)
    {
        if (mapManager.IsImpassable(pos) || mapManager.GetUnitAt(pos) != null)
            return false;
        
        if (mapManager.HasCityAt(pos))
            return false;
        
        // Перевіряємо, чи є поруч ресурси
        bool hasResources = false;
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                if (!mapManager.IsImpassable(checkPos))
                {
                    hasResources = true;
                    break;
                }
            }
        }
        
        return hasResources;
    }
    
    int EvaluateCityLocation(Vector3Int pos)
    {
        int score = 0;
        
        // Оцінюємо доступ до ресурсів
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                if (!mapManager.IsImpassable(checkPos))
                {
                    score += 1;
                }
            }
        }
        
        // Віддаємо перевагу місцям далеко від інших міст
        foreach (City city in aiCities)
        {
            if (city != null)
            {
                int distance = (int)Vector3Int.Distance(pos, city.gridPosition);
                score += distance * 2;
            }
        }
        
        return score;
    }
    
    CombatSystem.UnitStats GetUnitStatsForType(string unitName)
    {
        if (unitName.Contains("Settler")) return new CombatSystem.UnitStats("Settler", 10, 0, 2, 2, 0);
        if (unitName.Contains("Warrior")) return new CombatSystem.UnitStats("Warrior", 20, 10, 8, 2, 1);
        if (unitName.Contains("Archer")) return new CombatSystem.UnitStats("Archer", 15, 8, 4, 2, 2);
        
        // За замовчуванням - воїн
        return new CombatSystem.UnitStats("Warrior", 20, 10, 8, 2, 1);
    }
    
    public void RemoveUnit(Unit unit)
    {
        if (aiUnits.Contains(unit))
        {
            aiUnits.Remove(unit);
        }
    }
    
    public void RemoveCity(City city)
    {
        if (aiCities.Contains(city))
        {
            aiCities.Remove(city);
        }
    }
}
