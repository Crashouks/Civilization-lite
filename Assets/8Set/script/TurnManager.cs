using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [Header("Налаштування ходу")]
    public int currentTurn = 1;
    
    public void EndPlayerTurn()
    {
        Debug.Log("=== ЗАВЕРШЕННЯ ХОДУ ===");
        Debug.Log("Хід " + currentTurn + " завершено");
        
        // Знаходимо Program1 для доступу до списку юнітів
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null)
        {
            Debug.Log("Program1 знайдено, скидаємо очки руху для всіх юнітів");
            
            int unitsReset = 0;
            foreach (Unit u in manager.allUnits)
            {
                u.ResetMovement();
                unitsReset++;
            }
            
            Debug.Log("Очки руху скинуто для " + unitsReset + " юнітів");
        }
        else
        {
            Debug.LogError("Program1 не знайдено!");
        }
        
        currentTurn++;
        Debug.Log("=== НАЧАТОК ХОДУ " + currentTurn + " ===");
        
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
