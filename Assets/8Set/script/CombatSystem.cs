using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatSystem : MonoBehaviour
{
    [Header("Налаштування бою")]
    public float combatAnimationDuration = 1.0f;
    public GameObject combatEffectPrefab;
    
    private static CombatSystem instance;
    public static CombatSystem Instance
    {
        get { return instance; }
    }
    
    [System.Serializable]
    public class UnitStats
    {
        public string unitType;
        public int maxHealth;
        public int attackPower;
        public int defensePower;
        public int movementRange;
        public int attackRange;
        
        public UnitStats(string type, int health, int attack, int defense, int movement, int range)
        {
            unitType = type;
            maxHealth = health;
            attackPower = attack;
            defensePower = defense;
            movementRange = movement;
            attackRange = range;
        }
    }
    
    private Dictionary<string, UnitStats> unitStats = new Dictionary<string, UnitStats>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeUnitStats();
        CacheMapManager();
    }
    
    void InitializeUnitStats()
    {
        // Визначаємо статистику для різних типів юнітів
        unitStats["Settler"] = new UnitStats("Settler", 10, 0, 2, 2, 0);
        unitStats["Warrior"] = new UnitStats("Warrior", 20, 6, 5, 2, 1);
        unitStats["Archer"] = new UnitStats("Archer", 15, 5, 3, 2, 2);
        unitStats["Swordsman"] = new UnitStats("Swordsman", 25, 7, 6, 2, 1);
        unitStats["Horseman"] = new UnitStats("Horseman", 18, 8, 4, 4, 1);
        unitStats["Scout"] = new UnitStats("Scout", 12, 4, 2, 4, 1);
    }

    void CacheMapManager()
    {
        if (mapManager == null)
            mapManager = Object.FindAnyObjectByType<Program1>();
    }
    
    public bool CanAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return false;
        if (attacker.isPlayer == defender.isPlayer) return false;
        if (attacker.currentMovement <= 0) return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy != null)
        {
            string defenderCiv = defender.GetCivName();
            if (attacker.isPlayer && !diplomacy.IsAtWarWith(defenderCiv))
                return false;
        }

        UnitStats attackerStats = GetUnitStats(attacker);
        if (attackerStats.attackRange == 0) return false;

        int distance = CalculateDistance(attacker.gridPosition, defender.gridPosition);
        return distance <= attackerStats.attackRange;
    }
    
    public IEnumerator ExecuteAttack(Unit attacker, Unit defender)
    {
        if (!CanAttack(attacker, defender))
        {
            Debug.LogWarning("Атака неможлива!");
            yield break;
        }
        
        UnitStats attackerStats = GetUnitStats(attacker);
        UnitStats defenderStats = GetUnitStats(defender);
        
        Debug.Log($"{attacker.name} атакує {defender.name}!");

        UnitAnimator attackerAnim = attacker.GetComponent<UnitAnimator>();
        Program1 map = Object.FindAnyObjectByType<Program1>();
        if (attackerAnim != null && map != null && map.tilemap != null)
        {
            Vector3 defenderPos = map.tilemap.GetCellCenterWorld(defender.gridPosition);
            defenderPos.y -= 1f;
            attackerAnim.FaceToward(defenderPos - attacker.transform.position);
            yield return StartCoroutine(attackerAnim.PlayAttackRoutine(defender, CalculateDamage(attackerStats, defenderStats)));
        
        CacheMapManager();
        if (combatEffectPrefab != null && mapManager != null && mapManager.tilemap != null)
        {
            GameObject effect = Instantiate(combatEffectPrefab,
                mapManager.tilemap.GetCellCenterWorld(defender.gridPosition),
                Quaternion.identity);
            Destroy(effect, combatAnimationDuration);
        }
        else
        {
            int damage = CalculateDamage(attackerStats, defenderStats);
            bool defenderDestroyed = ApplyDamage(defender, damage);
            if (defenderDestroyed)
                OnUnitDestroyed(attacker, defender);
        }

        attacker.currentMovement = 0;

        if (combatEffectPrefab != null && map != null && map.tilemap != null)
        {
            GameObject effect = Instantiate(combatEffectPrefab,
                map.tilemap.GetCellCenterWorld(defender.gridPosition),
                Quaternion.identity);
            Destroy(effect, combatAnimationDuration);
        }

        CheckCombatEnd(attacker, defender);
    }
    
    int CalculateDamage(UnitStats attackerStats, UnitStats defenderStats)
    {
        // Базова формула: Атака - Захист + Випадковий модифікатор
        int baseDamage = attackerStats.attackPower - defenderStats.defensePower;
        
        // Додаємо випадковий фактор (-3 до +3)
        int randomModifier = Random.Range(-3, 4);
        
        int finalDamage = baseDamage + randomModifier;
        
        // Мінімальне пошкодження - 1
        finalDamage = Mathf.Max(1, finalDamage);
        
        Debug.Log($"Розрахунок пошкодження: {attackerStats.attackPower} (атака) - {defenderStats.defensePower} (захист) + {randomModifier} (випадковість) = {finalDamage}");
        
        return finalDamage;
    }
    
    bool ApplyDamage(Unit unit, int damage)
    {
        // Якщо у юніта немає UnitHealth, використовуємо базове HP в Unit
        // (щоб не було one-shot з одного удару).
        if (unit.GetComponent<UnitHealth>() == null)
        {
            int hpBefore = unit.health;
            unit.TakeDamage(damage);
            bool destroyed = unit == null || hpBefore - damage <= 0;
            return destroyed;
        }
        
        UnitHealth health = unit.GetComponent<UnitHealth>();
        health.TakeDamage(damage);
        
        if (health.currentHealth <= 0)
        {
            Debug.Log($"{unit.name} знищено!");
            return true;
        }
        
        Debug.Log($"{unit.name} отримав {damage} пошкоджень, залишилось {health.currentHealth} здоров'я");
        return false;
    }
    
    void OnUnitDestroyed(Unit attacker, Unit defender)
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AwardKillReward(attacker, defender);

        DiplomacyManager diplomacy = Object.FindAnyObjectByType<DiplomacyManager>();
        if (diplomacy != null)
        {
            string attackerCiv = attacker.isPlayer ? "Player" : "AI";
            string defenderCiv = defender.isPlayer ? "Player" : "AI";
            diplomacy.OnUnitDestroyed(attackerCiv, defenderCiv);
        }
        
        // Видаляємо юніта з гри
        Program1 mapManager = Object.FindAnyObjectByType<Program1>();
        if (mapManager != null)
        {
            mapManager.RemoveUnit(defender);
        }
        
        // Повідомляємо AI системи
        CivilizationAI ai = Object.FindAnyObjectByType<CivilizationAI>();
        if (ai != null)
        {
            ai.RemoveUnit(defender);
        }
        
        // Знищуємо об'єкт
        Destroy(defender.gameObject);
    }
    
    void CheckCombatEnd(Unit attacker, Unit defender)
    {
        // Перевіряємо, чи є ще ворожі юніти поруч
        List<Unit> nearbyEnemies = FindNearbyEnemies(attacker, 3);
        
        if (nearbyEnemies.Count == 0)
        {
            Debug.Log("Бій завершено! Поруч немає ворожих юнітів.");
        }
        else
        {
            Debug.Log($"Поруч є {nearbyEnemies.Count} ворожих юнітів");
        }
    }
    
    List<Unit> FindNearbyEnemies(Unit unit, int range)
    {
        List<Unit> enemies = new List<Unit>();
        Program1 mapManager = Object.FindAnyObjectByType<Program1>();
        
        if (mapManager == null) return enemies;
        
        foreach (Unit other in mapManager.allUnits)
        {
            if (other != null && other != unit && other.isPlayer != unit.isPlayer)
            {
                int distance = CalculateDistance(unit.gridPosition, other.gridPosition);
                if (distance <= range)
                {
                    enemies.Add(other);
                }
            }
        }
        
        return enemies;
    }
    
    int CalculateDistance(Vector3Int pos1, Vector3Int pos2)
    {
        // Для гексагональної сітки використовуємо спеціальну формулу
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        
        // Спрощена відстань для гексів
        return Mathf.Max(dx, dy);
    }
    
    UnitStats GetUnitStats(Unit unit)
    {
        string unitType = unit.name.Contains("Settler") ? "Settler" :
                         unit.name.Contains("Scout") ? "Scout" :
                         unit.name.Contains("Warrior") ? "Warrior" :
                         unit.name.Contains("Archer") ? "Archer" :
                         unit.name.Contains("Swordsman") ? "Swordsman" :
                         unit.name.Contains("Horseman") ? "Horseman" : "Warrior";
        
        if (unitStats.ContainsKey(unitType))
        {
            return unitStats[unitType];
        }
        
        // Повертаємо стандартні stats для воїна
        return unitStats["Warrior"];
    }
    
    public List<Unit> GetPossibleTargets(Unit attacker)
    {
        List<Unit> targets = new List<Unit>();
        Program1 mapManager = Object.FindAnyObjectByType<Program1>();
        
        if (mapManager == null) return targets;
        
        UnitStats attackerStats = GetUnitStats(attacker);
        
        foreach (Unit other in mapManager.allUnits)
        {
            if (other != null && other != attacker && other.isPlayer != attacker.isPlayer)
            {
                int distance = CalculateDistance(attacker.gridPosition, other.gridPosition);
                if (distance <= attackerStats.attackRange)
                {
                    targets.Add(other);
                }
            }
        }
        
        return targets;
    }
    
    public bool IsInRange(Unit unit1, Unit unit2, int range)
    {
        int distance = CalculateDistance(unit1.gridPosition, unit2.gridPosition);
        return distance <= range;
    }
    
    // Метод для отримання інформації про юніта
    public string GetUnitInfo(Unit unit)
    {
        UnitStats stats = GetUnitStats(unit);
        UnitHealth health = unit.GetComponent<UnitHealth>();
        
        string info = $"{unit.name}\n";
        info += $"Атака: {stats.attackPower}\n";
        info += $"Захист: {stats.defensePower}\n";
        info += $"Дальність атаки: {stats.attackRange}\n";
        
        if (health != null)
        {
            info += $"Здоров'я: {health.currentHealth}/{health.maxHealth}\n";
        }
        
        info += $"Рух: {unit.currentMovement}/{stats.movementRange}";
        
        return info;
    }
    
    private Program1 mapManager;
}
