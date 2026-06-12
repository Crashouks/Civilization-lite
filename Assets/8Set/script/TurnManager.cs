using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [Header("Налаштування ходу")]
    public int currentTurn = 1;
    
    public void EndPlayerTurn()
    {
        // Перенаправляємо на Program1.NextTurn() для керування AI
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null)
        {
            Debug.Log("TurnManager: перенаправляємо на Program1.NextTurn()");
            manager.NextTurn();
        }
        else
        {
            Debug.LogError("TurnManager: Program1 не знайдено!");
            Debug.LogError("Program1 не знайдено!");
        }
        
        currentTurn++;
        Debug.Log("=== НАЧАТОК ХОДУ " + currentTurn + " ===");

        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RefreshVisibility();

        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        // Запускаємо хід AI (завжди, а не тільки під час війни)
        if (DiplomacyManager.Instance != null)
        {
            Debug.Log("Запускаємо хід AI...");
            DiplomacyManager.Instance.StartCoroutine(DiplomacyManager.Instance.AITakeTurn());
        }
        else
        {
            Debug.LogWarning("DiplomacyManager не знайдено!");
        }
    }

    // Метод для сумісності з існуючою кнопкою
    public void EndTurn()
    {
        EndPlayerTurn();
    }
    
    // Метод для примусового скидання всіх юнітів (резервний)
    public void ResetAllUnits(List<Unit> units)
    {
        Debug.Log("TurnManager: ResetAllUnits() викликано для " + units.Count + " юнітів");
        foreach (Unit u in units)
        {
            u.ResetTurn();
        }
    }
}
