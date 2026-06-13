using UnityEngine;
using UnityEngine.Tilemaps;

public class City : MonoBehaviour
{
    public Vector3Int gridPosition;
    public bool isPlayerCity = false;
    public string ownerCivName = "Unknown";
    public string cityName = "City";
    public bool isCapital;
    public int foundedTurn;

    public const int CoinsPerCitizen = 5;
    public const int TurnsPerCitizenGrowth = 10;
    public const int HpPerCitizen = 25;

    public int maxHealth;
    public int currentHealth;
    int lastCitizenCount;

    public void InitializeFoundedTurn(int turn)
    {
        if (foundedTurn <= 0 && turn > 0)
            foundedTurn = turn;
    }

    public int GetTurnsSinceFounded(int currentTurn)
    {
        if (foundedTurn <= 0)
            return 0;

        return Mathf.Max(0, currentTurn - foundedTurn);
    }

    public int GetCitizenCount(int currentTurn)
    {
        return 1 + GetTurnsSinceFounded(currentTurn) / TurnsPerCitizenGrowth;
    }

    public int GetIncome(int currentTurn)
    {
        return GetCitizenCount(currentTurn) * CoinsPerCitizen;
    }

    public int GetMaxHealth(int currentTurn)
    {
        return GetCitizenCount(currentTurn) * HpPerCitizen;
    }

    public void RefreshHealth(int currentTurn)
    {
        int citizens = GetCitizenCount(currentTurn);
        int newMax = citizens * HpPerCitizen;

        if (lastCitizenCount > 0 && citizens > lastCitizenCount)
            currentHealth += (citizens - lastCitizenCount) * HpPerCitizen;

        maxHealth = newMax;
        lastCitizenCount = citizens;

        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    public bool TakeDamage(int damage, int currentTurn)
    {
        RefreshHealth(currentTurn);
        currentHealth = Mathf.Max(0, currentHealth - damage);
        return currentHealth <= 0;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(cityName) && cityName != "City")
            return cityName;

        if (!string.IsNullOrEmpty(ownerCivName) && ownerCivName != "Unknown")
            return ownerCivName + " City";

        return "City";
    }

    public bool IsOwnedByPlayer()
    {
        if (isPlayerCity)
            return true;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        string playerCiv = manager != null
            ? manager.currentCivName
            : PlayerPrefs.GetString("SelectedCiv", "Rome");

        return !string.IsNullOrEmpty(ownerCivName)
            && ownerCivName != "Unknown"
            && ownerCivName == playerCiv;
    }

    public void EnsureDisplayName(Program1 manager = null)
    {
        if (!string.IsNullOrEmpty(cityName) && cityName != "City")
            return;

        if (manager == null)
            manager = Object.FindAnyObjectByType<Program1>();

        string civ = ownerCivName;
        if (string.IsNullOrEmpty(civ) || civ == "Unknown")
            civ = isPlayerCity && manager != null
                ? manager.currentCivName
                : PlayerPrefs.GetString("SelectedCiv", "Rome");

        ownerCivName = civ;
        cityName = CityLabel.GenerateCityName(civ, isCapital);
        gameObject.name = civ + "_" + cityName;
    }
    
    void Start()
    {
        // Автоматично налаштовуємо позицію при старті
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null && manager.tilemap != null)
        {
            Init(gridPosition, manager.tilemap);
        }
        
        // Додаємо колайдер якщо його немає
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one * 0.8f; // Розмір колайдера
            Debug.Log("Додано колайдер до міста " + name);
        }
    }

    public void Init(Vector3Int pos, Tilemap tilemap)
    {
        gridPosition = pos;

        // Отримуємо світову позицію з тайлмапи
        Vector3 worldPos = tilemap.GetCellCenterWorld(pos);

        // Встановлюємо позицію міста (зміщуємо Y для правильного відображення)
        transform.position = new Vector3(worldPos.x, worldPos.y - 0.2f, -0.15f);
        
        Debug.Log("Місто ініціалізовано на позиції: " + pos);
    }
    
    public void SetFogVisibility(bool visible)
    {
        CityLabel label = GetComponent<CityLabel>();
        if (label != null)
            label.SetVisible(visible);

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.GetComponentInParent<CityLabel>() != null) continue;
            sr.enabled = visible;
        }

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col != null)
                col.enabled = visible;
        }
    }

    public void SetupLabel(string civName, Color civColor)
    {
        CityLabel label = GetComponent<CityLabel>() ?? gameObject.AddComponent<CityLabel>();
        label.Setup(cityName, civName, civColor, isCapital);
    }
}
