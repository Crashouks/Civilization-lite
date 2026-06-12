using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitAI : MonoBehaviour
{
    private Unit selfUnit;
    private Program1 mapManager;
    private CivilizationAI civilizationAI;
    private CombatSystem combatSystem;
    
    [Header("AI Поведінка")]
    public float aggressiveness = 0.5f; // 0 = обережний, 1 = агресивний
    public float strategicThinking = 0.7f; // Наскільки розумно приймає рішення
    
    void Start()
    {
        selfUnit = GetComponent<Unit>();
        mapManager = Object.FindAnyObjectByType<Program1>();
        civilizationAI = Object.FindAnyObjectByType<CivilizationAI>();
        combatSystem = Object.FindAnyObjectByType<CombatSystem>();
    }

    public IEnumerator TakeTurn()
    {
        Debug.Log($"=== AI ХІД ПОЧАВСЯ: {selfUnit.name} ===");
        
        // Аналізуємо ситуацію
        AISituation situation = AnalyzeSituation();
        Debug.Log($"Ситуація: {situation.currentTask}");
        
        // Приймаємо рішення на основі ситуації
        yield return StartCoroutine(ExecuteDecision(situation));
        
        Debug.Log($"=== AI ХІД ЗАВЕРШЕНО: {selfUnit.name} ===");
    }

    Unit FindNearestPlayer()
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Unit u in allUnits)
        {
            if (u != null && u != selfUnit && u.isPlayer != selfUnit.isPlayer)
            {
                float distance = Vector3Int.Distance(selfUnit.gridPosition, u.gridPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = u;
                }
            }
        }
        
        return nearest;
    }
    
    AISituation AnalyzeSituation()
    {
        AISituation situation = new AISituation();

        // Перевіряємо, чи ця конкретна цивілізація воює з гравцем
        bool isAtWar = CheckIfAtWarWithPlayer();
        
        // Знаходимо ворогів поруч
        List<Unit> nearbyEnemies = FindNearbyEnemies(5);
        situation.hasNearbyEnemies = nearbyEnemies.Count > 0;
        situation.nearestEnemy = nearbyEnemies.Count > 0 ? nearbyEnemies[0] : null;
        
        // Знаходимо союзників поруч
        List<Unit> nearbyAllies = FindNearbyAllies(3);
        situation.hasAlliesNearby = nearbyAllies.Count > 0;
        
        // Визначаємо пріоритет завдання
        if (selfUnit.name.Contains("Settler"))
        {
            situation.currentTask = AITask.FindCityLocation;
        }
        else if (isAtWar && situation.hasNearbyEnemies)
        {
            if (CanAttackAnyEnemy(nearbyEnemies))
            {
                situation.currentTask = AITask.Attack;
            }
            else
            {
                situation.currentTask = AITask.ApproachEnemy;
            }
        }
        else if (isAtWar)
        {
            situation.currentTask = AITask.HuntEnemies;
        }
        else if (selfUnit.name.Contains("Warrior"))
        {
            // В мирний час воїн має займатись розвідкою території.
            situation.currentTask = AITask.Explore;
        }
        else if (selfUnit.currentMovement > 1)
        {
            situation.currentTask = AITask.Explore;
        }
        else
        {
            situation.currentTask = AITask.Patrol;
        }
        
        return situation;
    }

    IEnumerator ExecuteDecision(AISituation situation)
    {
        switch (situation.currentTask)
        {
            case AITask.Attack:
                yield return StartCoroutine(ExecuteAttack(situation.nearestEnemy));
                break;
                
            case AITask.ApproachEnemy:
                yield return StartCoroutine(ApproachEnemy(situation.nearestEnemy));
                break;
                
            case AITask.HuntEnemies:
                yield return StartCoroutine(HuntEnemies());
                break;
                
            case AITask.FindCityLocation:
                yield return StartCoroutine(FindCityLocation());
                break;
                
            case AITask.Explore:
                yield return StartCoroutine(Explore());
                break;
                
            case AITask.Patrol:
                yield return StartCoroutine(Patrol());
                break;
        }
    }
    
    IEnumerator ExecuteAttack(Unit target)
    {
        if (target == null || combatSystem == null) yield break;
        
        if (combatSystem.CanAttack(selfUnit, target))
        {
            Debug.Log($"{selfUnit.name} атакує {target.name}!");
            yield return StartCoroutine(combatSystem.ExecuteAttack(selfUnit, target));
        }
        else
        {
            // Якщо не можемо атакувати, наближаємося
            yield return StartCoroutine(ApproachEnemy(target));
        }
    }
    
    IEnumerator ApproachEnemy(Unit enemy)
    {
        if (enemy == null) yield break;
        
        List<Vector3Int> path = mapManager.FindPath(selfUnit.gridPosition, enemy.gridPosition);
        
        if (path != null && path.Count > 0)
        {
            int maxMove = Mathf.Min(path.Count, selfUnit.currentMovement);
            for (int i = 0; i < maxMove; i++)
            {
                yield return StartCoroutine(selfUnit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                
                // Перевіряємо, чи можемо атакувати після кожного кроку
                if (combatSystem != null && combatSystem.CanAttack(selfUnit, enemy))
                {
                    yield return StartCoroutine(combatSystem.ExecuteAttack(selfUnit, enemy));
                    break;
                }
                
                // Мінімальна затримка між кроками AI
                yield return null;
            }
        }
        else
        {
            // Якщо шлях до ворога не будується (або порожній),
            // щоб юніт не "зависав" після оголошення війни — переходимо на розвідку.
            yield return StartCoroutine(Explore());
        }
    }
    
    IEnumerator HuntEnemies()
    {
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            yield return StartCoroutine(ApproachEnemy(nearestEnemy));
        }
        else
        {
            yield return StartCoroutine(Explore());
        }
    }
    
    IEnumerator FindCityLocation()
    {
        // Логіка для поселенця - шукає хороше місце для міста
        Vector3Int bestLocation = FindBestCityLocation();
        
        if (bestLocation != Vector3Int.zero)
        {
            List<Vector3Int> path = mapManager.FindPath(selfUnit.gridPosition, bestLocation);
            
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, selfUnit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(selfUnit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }
                
                // Якщо дійшли до місця і немає руху, засновуємо місто
                if (selfUnit.gridPosition == bestLocation && selfUnit.currentMovement <= 0)
                {
                    if (IsGoodCityLocation(selfUnit.gridPosition))
                    {
                        CreateCity();
                    }
                }
            }
        }
    }
    
    IEnumerator Explore()
    {
        // Розумна розвідка - рухаємося до нерозвіданих територій
        Vector3Int target = FindUnexploredLocation();
        
        if (target != Vector3Int.zero)
        {
            List<Vector3Int> path = mapManager.FindPath(selfUnit.gridPosition, target);
            
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, selfUnit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(selfUnit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }
                yield break;
            }
        }

        // Якщо не знайшли ціль або шлях недосяжний — рухаємось у доступну випадкову точку.
        yield return StartCoroutine(PatrolSmall());
    }
    
    IEnumerator PatrolSmall()
    {
        // Маленьке патрулювання в радіусі 2 клітинок, кілька спроб знайти досяжну ціль.
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3Int randomPos = selfUnit.gridPosition + new Vector3Int(Random.Range(-2, 3), Random.Range(-2, 3), 0);
            if (mapManager.IsImpassable(randomPos) || mapManager.GetUnitAt(randomPos) != null)
                continue;

            List<Vector3Int> path = mapManager.FindPath(selfUnit.gridPosition, randomPos);
            
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, selfUnit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(selfUnit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }
                yield break;
            }
        }
    }
    
    IEnumerator Patrol()
    {
        // Випадкове патрулювання, кілька спроб знайти досяжну ціль.
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3Int randomPos = selfUnit.gridPosition + new Vector3Int(Random.Range(-2, 3), Random.Range(-2, 3), 0);
            if (mapManager.IsImpassable(randomPos) || mapManager.GetUnitAt(randomPos) != null)
                continue;

            List<Vector3Int> path = mapManager.FindPath(selfUnit.gridPosition, randomPos);
            
            if (path != null && path.Count > 0)
            {
                int maxMove = Mathf.Min(path.Count, selfUnit.currentMovement);
                for (int i = 0; i < maxMove; i++)
                {
                    yield return StartCoroutine(selfUnit.MoveAlongPath(new List<Vector3Int> { path[i] }, mapManager.tilemap, mapManager));
                }
                yield break;
            }
        }
    }
    
    // Допоміжні методи
    List<Unit> FindNearbyEnemies(int range)
    {
        List<Unit> enemies = new List<Unit>();
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit u in allUnits)
        {
            if (u != null && u != selfUnit && u.isPlayer != selfUnit.isPlayer)
            {
                int distance = (int)Vector3Int.Distance(selfUnit.gridPosition, u.gridPosition);
                if (distance <= range)
                {
                    enemies.Add(u);
                }
            }
        }
        
        // Сортуємо за відстанню
        enemies.Sort((a, b) => Vector3Int.Distance(selfUnit.gridPosition, a.gridPosition).CompareTo(Vector3Int.Distance(selfUnit.gridPosition, b.gridPosition)));
        return enemies;
    }
    
    List<Unit> FindNearbyAllies(int range)
    {
        List<Unit> allies = new List<Unit>();
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (Unit u in allUnits)
        {
            if (u != null && u != selfUnit && u.isPlayer == selfUnit.isPlayer)
            {
                int distance = (int)Vector3Int.Distance(selfUnit.gridPosition, u.gridPosition);
                if (distance <= range)
                {
                    allies.Add(u);
                }
            }
        }
        
        return allies;
    }
    
    bool CanAttackAnyEnemy(List<Unit> enemies)
    {
        if (combatSystem == null) return false;
        
        foreach (Unit enemy in enemies)
        {
            if (combatSystem.CanAttack(selfUnit, enemy))
            {
                return true;
            }
        }
        return false;
    }
    
    Unit FindNearestEnemy()
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Unit u in allUnits)
        {
            if (u != null && u != selfUnit && u.isPlayer != selfUnit.isPlayer)
            {
                float distance = Vector3Int.Distance(selfUnit.gridPosition, u.gridPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = u;
                }
            }
        }
        
        return nearest;
    }
    
    Vector3Int FindBestCityLocation()
    {
        Vector3Int bestPos = Vector3Int.zero;
        int bestScore = -1;
        
        for (int x = -5; x <= 5; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                Vector3Int checkPos = selfUnit.gridPosition + new Vector3Int(x, y, 0);
                
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
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                if (!mapManager.IsImpassable(checkPos))
                {
                    return true;
                }
            }
        }
        
        return false;
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
        
        return score;
    }
    
    Vector3Int FindUnexploredLocation()
    {
        // Розумний пошук нерозвіданих територій
        List<Vector3Int> candidates = new List<Vector3Int>();
        
        // Шукаємо цікаві точки в радіусі 4-6 клітинок
        int searchRadius = selfUnit.name.Contains("Settler") ? 6 : 4;
        
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector3Int checkPos = selfUnit.gridPosition + new Vector3Int(x, y, 0);
                
                if (!mapManager.IsImpassable(checkPos) && 
                    mapManager.GetUnitAt(checkPos) == null && 
                    !mapManager.HasCityAt(checkPos))
                {
                    // Перевіряємо, чи є там ресурси
                    if (HasResourcesNearby(checkPos))
                    {
                        candidates.Add(checkPos);
                    }
                }
            }
        }
        
        if (candidates.Count > 0)
        {
            // Вибираємо найдалішу точку для кращої розвідки
            candidates.Sort((a, b) => Vector3Int.Distance(selfUnit.gridPosition, a).CompareTo(Vector3Int.Distance(selfUnit.gridPosition, b)));
            return candidates[candidates.Count - 1]; // Найдаліша
        }
        
        return Vector3Int.zero;
    }
    
    bool HasResourcesNearby(Vector3Int pos)
    {
        // Перевіряємо, чи є поруч ресурси (не вода/гори)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                if (!mapManager.IsImpassable(checkPos))
                {
                    return true; // Знайшли придатну територію
                }
            }
        }
        return false;
    }
    
    void CreateCity()
    {
        // Логіка заснування міста
        if (civilizationAI != null)
        {
            // Викликаємо метод з CivilizationAI для створення міста
            civilizationAI.CreateAICity(selfUnit);
        }
    }

    bool CheckIfAtWarWithPlayer()
    {
        if (civilizationAI == null) return false;

        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy != null)
        {
            Program1 playerManager = Object.FindAnyObjectByType<Program1>();
            string playerName = playerManager != null ? playerManager.currentCivName : "Player";
            return diplomacy.IsAtWar(civilizationAI.civilizationName, playerName);
        }

        return civilizationAI.isAtWar;
    }
}

// Допоміжні структури
public class AISituation
{
    public AITask currentTask;
    public bool hasNearbyEnemies;
    public bool hasAlliesNearby;
    public Unit nearestEnemy;
}

public enum AITask
{
    Attack,
    ApproachEnemy,
    HuntEnemies,
    FindCityLocation,
    Explore,
    Patrol
}
