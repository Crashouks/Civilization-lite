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
