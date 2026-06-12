using UnityEngine;

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
    public int scoutCost = 80;
    public int warriorCost = 100;
    public int settlerCost = 120;

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
