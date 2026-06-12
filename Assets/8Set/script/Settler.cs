using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // ������ ���������� ���� ������� �����

public class Settler : Unit
{
    void Update()
    {
        // ����������, �� ��������� ���������, �� ���� �������� � �� ��������� ������ B
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

        // --- ����� ����: �������� ˲�� ---
        // ���� ������� ����, ������� �������� ���� �� �����
        game.tilemap.SetTile(gridPosition, game.grassTile);
        // ---------------------------------

        if (game.HasCityAt(gridPosition))
        {
            Debug.Log("��� ��� � ����!");
            return;
        }

        GameObject cityObj = Instantiate(game.cityPrefab);
        City city = cityObj.GetComponent<City>();

        if (city == null) city = cityObj.AddComponent<City>();

        city.Init(gridPosition, game.tilemap);
        city.isPlayerCity = true; // Встановлюємо прапорець гравецького міста
        city.ownerCivName = game.currentCivName;
        city.cityName = CityLabel.GenerateCityName(game.currentCivName, true);
        city.isCapital = true;
        city.SetupLabel(game.currentCivName, game.GetCivColor(game.currentCivName));

        game.RegisterCity(city);
        game.RemoveUnit(this);

        Destroy(gameObject);
    }
}
