using UnityEngine;

public class UnitHealth : MonoBehaviour
{
    [Header("Налаштування здоров'я")]
    public int maxHealth = 20;
    public int currentHealth;
    
    [Header("Візуалізація")]
    public GameObject healthBarPrefab;
    public bool showHealthBar = true;
    
    private GameObject healthBar;
    private UnityEngine.UI.Slider healthSlider;
    private Unit unit;
    
    void Start()
    {
        unit = GetComponent<Unit>();
        currentHealth = maxHealth;
        
        if (showHealthBar && healthBarPrefab != null)
        {
            CreateHealthBar();
        }
    }
    
    void CreateHealthBar()
    {
        // Створюємо health bar як дочірній об'єкт
        healthBar = Instantiate(healthBarPrefab, transform);
        healthBar.transform.localPosition = new Vector3(0, 1f, 0);
        
        healthSlider = healthBar.GetComponent<UnityEngine.UI.Slider>();
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        
        // Ховаємо health bar на початку
        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
        
        // Показуємо health bar коли отримано пошкодження
        if (showHealthBar && healthBar != null)
        {
            healthBar.SetActive(true);
        }
        
        Debug.Log($"{unit.name} отримав {damage} пошкоджень. Залишилось здоров'я: {currentHealth}/{maxHealth}");
    }
    
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
        
        Debug.Log($"{unit.name} вилікувано на {amount}. Поточне здоров'я: {currentHealth}/{maxHealth}");
    }
    
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar);
        }
    }
}
