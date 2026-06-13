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
    
    private Program1 mapManager;

    void Awake()
    {
        if (instance == null)
            instance = this;
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
        unitStats["Settler"] = new UnitStats("Settler", 50, 0, 0, 3, 0);
        unitStats["Scout"] = new UnitStats("Scout", 25, 10, 0, 4, 1);
        unitStats["Warrior"] = new UnitStats("Warrior", 50, 25, 0, 3, 1);
        unitStats["Swordsman"] = new UnitStats("Swordsman", 50, 25, 0, 3, 1);
        unitStats["Archer"] = new UnitStats("Archer", 25, 15, 0, 2, 2);
        unitStats["Horseman"] = new UnitStats("Horseman", 40, 20, 0, 4, 1);
    }

    void CacheMapManager()
    {
        mapManager = Object.FindAnyObjectByType<Program1>();
    }

    Program1 GetMapManager()
    {
        CacheMapManager();
        return mapManager;
    }
    
    public bool CanAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return false;
        if (attacker.isPlayer == defender.isPlayer) return false;
        if (attacker.currentMovement <= 0) return false;
        if (attacker.hasAttackedThisTurn) return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy != null)
        {
            string defenderCiv = defender.GetCivName();
            if (attacker.isPlayer && !diplomacy.IsAtWarWith(defenderCiv))
                return false;
            if (!attacker.isPlayer && !defender.isPlayer)
            {
                Program1 map = Object.FindAnyObjectByType<Program1>();
                if (map == null) return false;
                if (!diplomacy.AreAtWar(attacker.GetCivName(map), defender.GetCivName(map)))
                    return false;
            }
            if (!attacker.isPlayer && defender.isPlayer && GetMapManager() != null)
            {
                if (!diplomacy.AreAtWar(attacker.GetCivName(mapManager), mapManager.currentCivName))
                    return false;
            }
        }

        UnitStats attackerStats = GetUnitStats(attacker);
        if (attackerStats.attackPower <= 0 || attackerStats.attackRange <= 0) return false;

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
        int damage = attackerStats.attackPower;

        Debug.Log($"{attacker.name} атакує {defender.name}!");

        UnitAnimator attackerAnim = attacker.GetComponent<UnitAnimator>();
        Program1 map = GetMapManager();
        bool defenderDestroyed = false;

        if (attackerAnim != null && map != null && map.tilemap != null)
        {
            Vector3 defenderPos = map.GetUnitPositionForCell(defender.gridPosition);
            attackerAnim.FaceToward(defenderPos - attacker.transform.position);
            defender.lastAttacker = attacker;
            yield return StartCoroutine(attackerAnim.PlayAttackRoutine(defender, damage));
            defenderDestroyed = defender == null || defender.health <= 0;
        }
        else
        {
            if (combatEffectPrefab != null && map != null && map.tilemap != null)
            {
                GameObject effect = Instantiate(combatEffectPrefab,
                    map.tilemap.GetCellCenterWorld(defender.gridPosition),
                    Quaternion.identity);
                Destroy(effect, combatAnimationDuration);
            }

            yield return new WaitForSeconds(combatAnimationDuration);
            defenderDestroyed = ApplyDamage(defender, damage, attacker);
        }

        attacker.hasAttackedThisTurn = true;
        attacker.currentMovement = 0;

        map?.NotifyTurnStateChanged();

        if (attackerAnim != null && attacker != null)
            attackerAnim.ForceIdle();

        if (defenderDestroyed)
            OnUnitDestroyed(attacker, defender);

        CheckCombatEnd(attacker, defender);
    }
    
    int CalculateDamage(UnitStats attackerStats, UnitStats defenderStats)
    {
        return attackerStats.attackPower;
    }

    bool ApplyDamage(Unit unit, int damage, Unit attacker)
    {
        if (unit == null) return true;

        if (unit.GetComponent<UnitHealth>() == null)
        {
            int hpBefore = unit.health;
            unit.TakeDamage(damage, attacker);
            return unit == null || hpBefore - damage <= 0 || unit.IsDead;
        }

        UnitHealth health = unit.GetComponent<UnitHealth>();
        health.TakeDamage(damage);
        unit.health = health.currentHealth;

        if (health.currentHealth <= 0)
        {
            Debug.Log($"{unit.name} знищено!");
            return true;
        }

        Debug.Log($"{unit.name} отримав {damage} пошкоджень, залишилось {health.currentHealth} здоров'я");
        return false;
    }

    static CivilizationAI FindCivilizationAI(string civName)
    {
        if (string.IsNullOrEmpty(civName)) return null;

        foreach (CivilizationAI ai in Object.FindObjectsByType<CivilizationAI>(FindObjectsSortMode.None))
        {
            if (ai != null && ai.civilizationName == civName)
                return ai;
        }

        return null;
    }

    public UnitStats GetStatsForKind(UnitTypeHelper.UnitKind kind)
    {
        string key = UnitTypeHelper.GetTypeName(kind);
        if (unitStats.ContainsKey(key))
            return unitStats[key];

        return unitStats["Warrior"];
    }

    public void ApplyStatsToUnit(Unit unit)
    {
        if (unit == null)
            return;

        UnitStats stats = GetUnitStats(unit);
        unit.maxMovement = stats.movementRange;
        unit.attackPower = stats.attackPower;
        unit.health = stats.maxHealth;
        unit.currentMovement = unit.maxMovement;
        unit.hasAttackedThisTurn = false;

        UnitHealth health = unit.GetComponent<UnitHealth>();
        if (health != null)
        {
            health.maxHealth = stats.maxHealth;
            health.currentHealth = stats.maxHealth;
        }
    }

    public int GetAttackDamage(Unit unit)
    {
        return GetUnitStats(unit).attackPower;
    }
    
    public void NotifyUnitDestroyed(Unit attacker, Unit defender)
    {
        OnUnitDestroyed(attacker, defender);
    }

    void OnUnitDestroyed(Unit attacker, Unit defender)
    {
        if (defender == null) return;

        if (EconomyManager.Instance != null && attacker != null)
            EconomyManager.Instance.AwardKillReward(attacker, defender);

        Program1 map = GetMapManager();
        string attackerCiv = attacker != null ? attacker.GetCivName(map) : "Unknown";
        string defenderCiv = defender.GetCivName(map);

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy != null)
            diplomacy.OnUnitDestroyed(attackerCiv, defenderCiv);

        CivilizationAI ai = FindCivilizationAI(defenderCiv);
        if (ai != null)
            ai.RemoveUnit(defender);

        if ((attacker != null && attacker.isPlayer) || (defender != null && defender.isPlayer))
            SaveManager.Instance?.MarkUnsaved();
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
        
        foreach (Unit other in mapManager.allUnits.ToArray())
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
        Program1 map = GetMapManager();
        if (map != null)
            return map.GetHexDistance(pos1, pos2);

        return Mathf.Max(Mathf.Abs(pos1.x - pos2.x), Mathf.Abs(pos1.y - pos2.y));
    }
    
    public UnitStats GetUnitStats(Unit unit)
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
        
        return unitStats["Warrior"];
    }
    
    public List<Unit> GetPossibleTargets(Unit attacker)
    {
        List<Unit> targets = new List<Unit>();
        Program1 mapManager = Object.FindAnyObjectByType<Program1>();
        
        if (mapManager == null) return targets;
        
        UnitStats attackerStats = GetUnitStats(attacker);
        
        foreach (Unit other in mapManager.allUnits.ToArray())
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
        else
        {
            info += $"Здоров'я: {unit.health}/{stats.maxHealth}\n";
        }
        
        info += $"Рух: {unit.currentMovement}/{stats.movementRange}";
        
        return info;
    }
}
