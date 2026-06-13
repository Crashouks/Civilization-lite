using UnityEngine;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Баланс")]
    public int startingCoins = 100;

    [Header("Нагорода за вбивство")]
    public int settlerKillReward = 10;
    public int scoutKillReward = 15;
    public int warriorKillReward = 25;
    public int archerKillReward = 20;

    [Header("Вартість найму")]
    public int scoutCost = 50;
    public int warriorCost = 100;
    public int settlerCost = 150;

    [Header("Утримання юнітів")]
    public int freeScouts = 1;
    public int freeWarriors = 1;
    public int extraScoutUpkeep = 1;
    public int extraWarriorUpkeep = 2;

    readonly Dictionary<string, int> aiCivCoins = new Dictionary<string, int>();

    public int PlayerCoins { get; private set; }

    public event System.Action<int> OnCoinsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PlayerCoins = startingCoins;
    }

    public void SetCoins(int amount)
    {
        PlayerCoins = Mathf.Max(0, amount);
        OnCoinsChanged?.Invoke(PlayerCoins);
    }

    public void AddCoins(int amount, string reason = "")
    {
        if (amount <= 0) return;
        PlayerCoins += amount;
        if (!string.IsNullOrEmpty(reason))
            Debug.Log("+ " + amount + " монет: " + reason + " (всього: " + PlayerCoins + ")");
        OnCoinsChanged?.Invoke(PlayerCoins);
    }

    public bool TrySpendCoins(int amount, string reason = "")
    {
        if (amount <= 0) return true;
        if (PlayerCoins < amount)
        {
            Debug.Log("Недостатньо монет! Потрібно " + amount + ", є " + PlayerCoins);
            return false;
        }
        PlayerCoins -= amount;
        if (!string.IsNullOrEmpty(reason))
            Debug.Log("- " + amount + " монет: " + reason + " (залишилось: " + PlayerCoins + ")");
        OnCoinsChanged?.Invoke(PlayerCoins);
        return true;
    }

    public int GetAiCivCoins(string civName)
    {
        if (string.IsNullOrEmpty(civName))
            return 0;

        return aiCivCoins.TryGetValue(civName, out int coins) ? coins : 0;
    }

    public void SetAiCivCoins(string civName, int amount)
    {
        if (string.IsNullOrEmpty(civName))
            return;

        aiCivCoins[civName] = Mathf.Max(0, amount);
    }

    public void AddAiCivCoins(string civName, int amount, string reason = "")
    {
        if (amount <= 0 || string.IsNullOrEmpty(civName))
            return;

        int total = GetAiCivCoins(civName) + amount;
        aiCivCoins[civName] = total;
        if (!string.IsNullOrEmpty(reason))
            Debug.Log("+ " + amount + " монет (" + civName + "): " + reason + " (всього: " + total + ")");
    }

    public bool TrySpendAiCivCoins(string civName, int amount, string reason = "")
    {
        if (amount <= 0)
            return true;

        int current = GetAiCivCoins(civName);
        if (current < amount)
            return false;

        aiCivCoins[civName] = current - amount;
        if (!string.IsNullOrEmpty(reason))
            Debug.Log("- " + amount + " монет (" + civName + "): " + reason + " (залишилось: " + aiCivCoins[civName] + ")");
        return true;
    }

    public void ClearAiTreasuries()
    {
        aiCivCoins.Clear();
    }

    public List<CivCoinsSaveData> ExportAiTreasuries()
    {
        var result = new List<CivCoinsSaveData>();
        foreach (KeyValuePair<string, int> entry in aiCivCoins)
        {
            result.Add(new CivCoinsSaveData
            {
                civName = entry.Key,
                coins = entry.Value
            });
        }
        return result;
    }

    public void ImportAiTreasuries(List<CivCoinsSaveData> saved)
    {
        aiCivCoins.Clear();
        if (saved == null)
            return;

        foreach (CivCoinsSaveData entry in saved)
        {
            if (entry == null || string.IsNullOrEmpty(entry.civName))
                continue;

            aiCivCoins[entry.civName] = Mathf.Max(0, entry.coins);
        }
    }

    public void CollectCityIncome(int currentTurn, Program1 manager)
    {
        if (manager == null || currentTurn <= 0)
            return;

        var incomeByCiv = new Dictionary<string, int>();
        foreach (City city in manager.allCities)
        {
            if (city == null || string.IsNullOrEmpty(city.ownerCivName) || city.ownerCivName == "Unknown")
                continue;

            int income = city.GetIncome(currentTurn);
            if (income <= 0)
                continue;

            if (!incomeByCiv.ContainsKey(city.ownerCivName))
                incomeByCiv[city.ownerCivName] = 0;
            incomeByCiv[city.ownerCivName] += income;
        }

        string playerCiv = manager.currentCivName;
        foreach (KeyValuePair<string, int> entry in incomeByCiv)
        {
            if (entry.Value <= 0)
                continue;

            if (entry.Key == playerCiv)
                AddCoins(entry.Value, "доход міст (" + entry.Key + ")");
            else
                AddAiCivCoins(entry.Key, entry.Value, "доход міст");
        }

        CollectPlayerUnitUpkeep(manager);
    }

    public int GetScoutUpkeep(Program1 manager)
    {
        if (manager == null)
            return 0;

        int scouts = manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Scout);
        return Mathf.Max(0, scouts - freeScouts) * extraScoutUpkeep;
    }

    public int GetWarriorUpkeep(Program1 manager)
    {
        if (manager == null)
            return 0;

        int warriors = manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Warrior);
        return Mathf.Max(0, warriors - freeWarriors) * extraWarriorUpkeep;
    }

    public int GetPlayerUnitUpkeep(Program1 manager)
    {
        return GetScoutUpkeep(manager) + GetWarriorUpkeep(manager);
    }

    public void CollectPlayerUnitUpkeep(Program1 manager)
    {
        int upkeep = GetPlayerUnitUpkeep(manager);
        if (upkeep <= 0)
            return;

        TrySpendCoins(upkeep, "утримання армії");
    }

    public void ProcessAiRecruitment(Program1 manager)
    {
        if (manager == null)
            return;

        string playerCiv = manager.currentCivName;
        var aiCivs = new HashSet<string>();
        foreach (City city in manager.allCities)
        {
            if (city == null || string.IsNullOrEmpty(city.ownerCivName))
                continue;
            if (city.ownerCivName == playerCiv)
                continue;

            aiCivs.Add(city.ownerCivName);
        }

        foreach (string civName in aiCivs)
        {
            bool atWar = DiplomacyManager.Instance != null && DiplomacyManager.Instance.IsCivAtWar(civName, manager);
            int recruited = 0;
            const int maxRecruitsPerTurn = 2;

            if (!atWar)
            {
                while (recruited < maxRecruitsPerTurn && GetAiCivCoins(civName) >= settlerCost)
                {
                    City spawnCity = manager.FindCityForRecruitment(civName);
                    if (spawnCity == null || !manager.TrySpawnAiUnitFromCity(spawnCity, civName, UnitTypeHelper.UnitKind.Settler))
                        break;
                    recruited++;
                }
            }
            else
            {
                while (recruited < maxRecruitsPerTurn && GetAiCivCoins(civName) >= warriorCost)
                {
                    City spawnCity = manager.FindCityForRecruitment(civName);
                    if (spawnCity == null || !manager.TrySpawnAiUnitFromCity(spawnCity, civName, UnitTypeHelper.UnitKind.Warrior))
                        break;
                    recruited++;
                }
            }
        }
    }

    public void AwardKillReward(Unit killer, Unit victim)
    {
        if (killer == null || victim == null) return;
        if (!killer.isPlayer) return;

        int reward = GetKillReward(victim);
        if (reward > 0)
            AddCoins(reward, "вбивство " + victim.name);
    }

    public int GetKillReward(Unit victim)
    {
        switch (UnitTypeHelper.GetKind(victim))
        {
            case UnitTypeHelper.UnitKind.Settler: return settlerKillReward;
            case UnitTypeHelper.UnitKind.Scout: return scoutKillReward;
            case UnitTypeHelper.UnitKind.Warrior: return warriorKillReward;
            case UnitTypeHelper.UnitKind.Archer: return archerKillReward;
            default: return warriorKillReward;
        }
    }

    public int GetUnitCost(UnitTypeHelper.UnitKind kind)
    {
        switch (kind)
        {
            case UnitTypeHelper.UnitKind.Scout: return scoutCost;
            case UnitTypeHelper.UnitKind.Warrior: return warriorCost;
            case UnitTypeHelper.UnitKind.Settler: return settlerCost;
            default: return warriorCost;
        }
    }
}
