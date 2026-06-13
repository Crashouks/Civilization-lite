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
    public GameObject scoutPrefab;
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

    public void BindToManager(Program1 manager)
    {
        if (manager == null) return;
        mapManager = manager;
        settlerPrefab = manager.settlerPrefab;
        warriorPrefab = manager.warriorPrefab;
        scoutPrefab = manager.scoutPrefab;
        if (scoutPrefab == null)
            scoutPrefab = Resources.Load<GameObject>("Thief");
    }

    int HexDist(Vector3Int a, Vector3Int b)
    {
        if (mapManager == null) return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return mapManager.GetHexDistance(a, b);
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
        foreach (Unit unit in mapManager.allUnits.ToArray())
        {
            if (unit != null && !unit.isPlayer && unit.GetCivName(mapManager) == civilizationName)
            {
                unit.ownerCivName = civilizationName;
                aiUnits.Add(unit);
            }
        }

        // Додаємо міста цієї цивілізації
        foreach (City city in mapManager.allCities.ToArray())
        {
            if (city != null && city.ownerCivName == civilizationName)
            {
                aiCities.Add(city);
            }
        }
    }

    public void ExecuteAITurn()
    {
        isProcessingTurn = true;
        // Оновлюємо список юнітів та міст перед ходом
        PopulateAIUnits();
        StartCoroutine(ExecuteAITurnCoroutine());
    }

    public bool IsProcessingTurn()
    {
        return isProcessingTurn;
    }

    public void ForceEndProcessing()
    {
        isProcessingTurn = false;
        StopAllCoroutines();
    }

    IEnumerator ExecuteAITurnCoroutine()
    {
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
    }

    IEnumerator ManageDiplomacy()
    {
        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();

        if (diplomacy != null && mapManager != null)
            isAtWar = diplomacy.IsCivAtWar(civilizationName, mapManager);

        yield return null;
    }
    
    IEnumerator ManageUnitProduction()
    {
        yield return null;
    }

    IEnumerator ManageUnits()
    {
        if (aiUnits.Count == 0)
            yield break;

        foreach (Unit unit in aiUnits.ToArray())
        {
            if (unit == null)
                continue;

            int safetySteps = unit.maxMovement + 2;
            while (unit != null && unit.currentMovement > 0 && safetySteps-- > 0)
            {
                int movementBefore = unit.currentMovement;
                yield return StartCoroutine(ControlUnit(unit));

                if (unit == null || unit.currentMovement <= 0)
                    break;

                if (unit.currentMovement >= movementBefore)
                {
                    unit.currentMovement = 0;
                    break;
                }
            }
        }

        aiUnits.RemoveAll(u => u == null);
        yield return null;
    }
    
    IEnumerator ControlUnit(Unit unit)
    {
        yield return StartCoroutine(BasicUnitControl(unit));
    }
    
    IEnumerator BasicUnitControl(Unit unit)
    {
        if (IsCivAtWar())
            yield return StartCoroutine(MoveToAttackEnemies(unit));
        else if (unit.name.Contains("Settler"))
            yield return StartCoroutine(FindCityLocation(unit));
        else
            yield return StartCoroutine(RandomMovement(unit));
    }

    IEnumerator MoveToAttackEnemies(Unit unit)
    {
        Unit adjacentEnemy = FindAdjacentEnemy(unit);
        if (adjacentEnemy != null)
        {
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

        City adjacentEnemyCity = FindAdjacentEnemyCity(unit);
        if (adjacentEnemyCity != null && mapManager.CanAttackCity(unit, adjacentEnemyCity))
        {
            yield return mapManager.StartCoroutine(mapManager.CaptureCityRoutine(unit, adjacentEnemyCity));
            yield break;
        }

        Unit nearestEnemy = FindNearestEnemyUnit(unit);
        City nearestEnemyCity = FindNearestEnemyCity(unit);

        Vector3Int targetPos;
        if (nearestEnemy != null)
            targetPos = nearestEnemy.gridPosition;
        else if (nearestEnemyCity != null)
            targetPos = nearestEnemyCity.gridPosition;
        else
        {
            unit.currentMovement = 0;
            yield break;
        }

        List<Vector3Int> path = mapManager.FindPath(unit.gridPosition, targetPos, unit);
        if (path == null || path.Count == 0)
        {
            unit.currentMovement = 0;
            yield break;
        }

        Vector3Int nextStep = path[0];
        if (nextStep == unit.gridPosition)
        {
            unit.currentMovement = 0;
            yield break;
        }

        yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { nextStep }, mapManager.tilemap, mapManager));
    }

    bool IsCivAtWar()
    {
        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        return diplomacy != null && diplomacy.IsCivAtWar(civilizationName, mapManager);
    }

    IEnumerator MoveToAttackPlayer(Unit unit)
    {
        yield return StartCoroutine(MoveToAttackEnemies(unit));
    }

    Unit FindAdjacentEnemy(Unit aiUnit)
    {
        if (mapManager == null) return null;

        foreach (Vector3Int pos in mapManager.GetNeighborCells(aiUnit.gridPosition))
        {
            Unit enemy = mapManager.GetUnitAt(pos);
            if (enemy != null && mapManager.CanUnitsFight(aiUnit, enemy))
                return enemy;
        }

        return null;
    }

    City FindAdjacentEnemyCity(Unit unit)
    {
        foreach (Vector3Int neighbor in mapManager.GetNeighborCells(unit.gridPosition))
        {
            City city = mapManager.GetCityAt(neighbor);
            if (city != null && mapManager.CanAttackCity(unit, city))
                return city;
        }
        return null;
    }

    Unit FindNearestEnemyUnit(Unit aiUnit)
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Unit unit in mapManager.allUnits.ToArray())
        {
            if (unit == null || unit == aiUnit)
                continue;

            if (!mapManager.CanUnitsFight(aiUnit, unit))
                continue;

            float dist = HexDist(aiUnit.gridPosition, unit.gridPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = unit;
            }
        }

        return nearest;
    }

    City FindNearestEnemyCity(Unit aiUnit)
    {
        City nearest = null;
        float nearestDist = float.MaxValue;

        foreach (City city in mapManager.allCities.ToArray())
        {
            if (city == null || !mapManager.CanAttackCity(aiUnit, city))
                continue;

            float dist = HexDist(aiUnit.gridPosition, city.gridPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = city;
            }
        }

        return nearest;
    }

    IEnumerator RandomMovement(Unit unit)
    {
        List<Vector3Int> neighbors = mapManager.GetNeighborCells(unit.gridPosition);
        var shuffled = new List<Vector3Int>(neighbors);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (Vector3Int targetPos in shuffled)
        {
            if (mapManager.IsImpassable(targetPos) || mapManager.GetUnitAt(targetPos) != null)
                continue;

            yield return StartCoroutine(unit.MoveAlongPath(new List<Vector3Int> { targetPos }, mapManager.tilemap, mapManager));
            yield break;
        }

        unit.currentMovement = 0;
    }

    Unit FindNearestPlayerUnit(Unit aiUnit)
    {
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Unit unit in mapManager.allUnits.ToArray())
        {
            if (unit != null && unit.isPlayer)
            {
                float dist = HexDist(aiUnit.gridPosition, unit.gridPosition);
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

        foreach (City city in mapManager.allCities.ToArray())
        {
            if (city != null && city.isPlayerCity)
            {
                float dist = HexDist(aiUnit.gridPosition, city.gridPosition);
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
        return IsCivAtWar();
    }

    Unit FindNearestPlayerEnemy(Unit unit)
    {
        Unit nearest = null;
        float minDistance = float.MaxValue;

        foreach (Unit u in mapManager.allUnits.ToArray())
        {
            // Шукаємо тільки юнітів гравця
            if (u != null && u != unit && u.isPlayer)
            {
                float distance = HexDist(unit.gridPosition, u.gridPosition);
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
                if (unit != null && enemy != null && mapManager != null && mapManager.AreCellsAdjacent(unit.gridPosition, enemy.gridPosition))
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
        foreach (Unit unit in aiUnits.ToArray())
        {
            if (unit != null && unit.name.Contains("Settler"))
            {
                // Поселенці завжди засновують міста
                if (mapManager.IsValidAiCitySite(civilizationName, unit.gridPosition))
                    CreateAICity(unit);
            }
        }

        yield return null;
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

        if (CombatSystem.Instance != null)
            CombatSystem.Instance.ApplyStatsToUnit(unit);
        else
        {
            CombatSystem.UnitStats stats = GetUnitStatsForType(unit.name);
            unit.maxMovement = stats.movementRange;
            unit.currentMovement = stats.movementRange;
            unit.attackPower = stats.attackPower;
            unit.health = stats.maxHealth;
            unit.hasAttackedThisTurn = false;
        }

        unit.ownerCivName = civilizationName;
        
        // Налаштовуємо колір
        SetUnitColor(obj);
        
        // Додаємо до списку
        aiUnits.Add(unit);
        mapManager.allUnits.Add(unit);
        mapManager.RegisterUnitCell(unit, position);
    }
    
    // Зроблено методи публічними для доступу з інших скриптів
    public void CreateAICity(Unit settler)
    {
        if (settler == null) return;

        if (!mapManager.IsValidAiCitySite(civilizationName, settler.gridPosition))
            return;
        
        // Створюємо місто
        GameObject cityObj = Instantiate(mapManager.cityPrefab, 
            mapManager.tilemap.GetCellCenterWorld(settler.gridPosition), 
            Quaternion.identity);
        
        City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
        city.gridPosition = settler.gridPosition;
        city.ownerCivName = civilizationName;
        city.isPlayerCity = false;
        city.isCapital = mapManager.allCities.Find(c => c != null && c.ownerCivName == civilizationName) == null;
        city.EnsureDisplayName(mapManager);
        city.Init(settler.gridPosition, mapManager.tilemap);
        SetUnitColor(cityObj);
        aiCities.Add(city);
        mapManager.RegisterCity(city);
        
        // Видаляємо поселенця
        aiUnits.Remove(settler);
        mapManager.RemoveUnit(settler);
        Destroy(settler.gameObject);
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
        
        foreach (Unit u in mapManager.allUnits.ToArray())
        {
            if (u != null && u != unit && u.isPlayer != unit.isPlayer)
            {
                float distance = HexDist(unit.gridPosition, u.gridPosition);
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
            if (!attacker.CanAttackThisTurn())
                return;

            int damage = attacker.attackPower;
            defender.TakeDamage(damage, attacker);
            attacker.hasAttackedThisTurn = true;
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
        
        return hasResources && mapManager.IsValidAiCitySite(civilizationName, pos);
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
        foreach (City city in aiCities.ToArray())
        {
            if (city != null)
            {
                int distance = (int)HexDist(pos, city.gridPosition);
                score += distance * 2;
            }
        }
        
        return score;
    }
    
    CombatSystem.UnitStats GetUnitStatsForType(string unitName)
    {
        if (unitName.Contains("Settler")) return new CombatSystem.UnitStats("Settler", 50, 0, 0, 3, 0);
        if (unitName.Contains("Scout")) return new CombatSystem.UnitStats("Scout", 25, 10, 0, 4, 1);
        if (unitName.Contains("Archer")) return new CombatSystem.UnitStats("Archer", 25, 15, 0, 2, 2);
        
        return new CombatSystem.UnitStats("Warrior", 50, 25, 0, 3, 1);
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
