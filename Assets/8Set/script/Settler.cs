using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // Додано підключення нової системи вводу

public class Settler : Unit
{
    void Update()
    {
        // Перевіряємо, чи підключена клавіатура, чи юніт вибраний і чи натиснута клавіша B
        if (Keyboard.current != null && isSelected && Keyboard.current.bKey.wasPressedThisFrame)
        {
            FoundCity();
        }
    }

    void FoundCity()
    {
        Program1 game = FindAnyObjectByType<Program1>();
        if (game == null) return;

        if (game.cityPrefab == null) return;

        // --- НОВИЙ БЛОК: ОЧИЩЕННЯ ЛІСУ ---
        // Коли ставимо місто, змінюємо поточний тайл на траву
        game.tilemap.SetTile(gridPosition, game.grassTile);
        // ---------------------------------

        if (game.HasCityAt(gridPosition))
        {
            Debug.Log("Тут вже є місто!");
            return;
        }

        GameObject cityObj = Instantiate(game.cityPrefab);
        City city = cityObj.GetComponent<City>();

        if (city == null) city = cityObj.AddComponent<City>();

        city.Init(gridPosition, game.tilemap);

        game.RegisterCity(city);
        game.RemoveUnit(this);

        Destroy(gameObject);
    }
}
