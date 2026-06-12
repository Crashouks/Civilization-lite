using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const string SaveFileName = "civilization_save.json";
    public const string LoadOnStartKey = "LoadSaveGame";

    public static bool IsRestoringSave { get; set; }
    public static bool LoadedFromSaveThisSession { get; set; }

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static bool TryReadSave(out GameSaveData data)
    {
        data = null;
        if (!HasSave()) return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Не вдалося прочитати збереження: " + e.Message);
            return false;
        }
    }

    public static void PrepareLoadFromSave()
    {
        if (!TryReadSave(out GameSaveData data)) return;

        if (!string.IsNullOrEmpty(data.playerCiv))
            PlayerPrefs.SetString("SelectedCiv", data.playerCiv);

        PlayerPrefs.SetInt(LoadOnStartKey, 1);
        PlayerPrefs.Save();
    }

    public void SaveGame()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        EconomyManager economy = EconomyManager.Instance;
        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        FogOfWarManager fog = FogOfWarManager.Instance;

        if (manager == null)
        {
            Debug.LogError("Не вдалося зберегти: Program1 не знайдено");
            return;
        }

        GameSaveData data = new GameSaveData
        {
            mapSeed = manager.seed,
            mapWidth = manager.width,
            mapHeight = manager.height,
            currentTurn = turnManager != null ? turnManager.currentTurn : 1,
            playerCiv = manager.currentCivName,
            playerCoins = economy != null ? economy.PlayerCoins : 0,
            isAtWar = diplomacy != null && diplomacy.isAtWar,
            enemyNations = diplomacy != null ? new List<string>(diplomacy.enemyNations) : new List<string>()
        };

        if (fog != null)
            data.exploredFlat = fog.ExportExploredFlat();

        foreach (Unit unit in manager.allUnits)
        {
            if (unit == null) continue;

            data.units.Add(new UnitSaveData
            {
                unitType = UnitTypeHelper.GetTypeName(UnitTypeHelper.GetKind(unit)),
                x = unit.gridPosition.x,
                y = unit.gridPosition.y,
                health = unit.health,
                currentMovement = unit.currentMovement,
                isPlayer = unit.isPlayer,
                civName = unit.isPlayer ? manager.currentCivName : ExtractCivName(unit.name)
            });
        }

        foreach (City city in manager.allCities)
        {
            if (city == null) continue;

            data.cities.Add(new CitySaveData
            {
                x = city.gridPosition.x,
                y = city.gridPosition.y,
                isPlayerCity = city.isPlayerCity,
                ownerCivName = city.ownerCivName,
                cityName = city.cityName
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game saved to JSON: " + SavePath);
    }

    public void LoadGame()
    {
        if (!PrepareLoadAndValidate()) return;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gamescena");
    }

    bool PrepareLoadAndValidate()
    {
        if (!TryReadSave(out GameSaveData data))
        {
            Debug.LogWarning("Файл збереження не знайдено");
            return false;
        }

        if (!string.IsNullOrEmpty(data.playerCiv))
            PlayerPrefs.SetString("SelectedCiv", data.playerCiv);

        PlayerPrefs.SetInt(LoadOnStartKey, 1);
        PlayerPrefs.Save();
        return true;
    }

    public void ApplyLoadedGame(Program1 manager)
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("Немає збереження для завантаження");
            return;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null)
        {
            Debug.LogError("Пошкоджене збереження");
            return;
        }

        IsRestoringSave = true;
        LoadedFromSaveThisSession = true;

        manager.seed = data.mapSeed;
        manager.width = data.mapWidth > 0 ? data.mapWidth : manager.width;
        manager.height = data.mapHeight > 0 ? data.mapHeight : manager.height;
        manager.currentCivName = data.playerCiv;

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.SetCoins(data.playerCoins);

        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
            turnManager.currentTurn = data.currentTurn;

        if (DiplomacyManager.Instance != null)
        {
            DiplomacyManager.Instance.isAtWar = data.isAtWar;
            DiplomacyManager.Instance.enemyNations = data.enemyNations ?? new List<string>();
            manager.isAtWar = data.isAtWar;
        }

        manager.ApplyCivFromName(data.playerCiv);
        PlayerPrefs.SetString("SelectedCiv", data.playerCiv ?? "Rome");
        PlayerPrefs.Save();
        manager.ClearAllGameObjects();
        manager.StartCoroutine(manager.RegenerateAndRestore(data));
    }

    string ExtractCivName(string unitName)
    {
        if (unitName.Contains("Rome")) return "Rome";
        if (unitName.Contains("America")) return "America";
        if (unitName.Contains("Egypt")) return "Egypt";
        if (unitName.Contains("Scythia")) return "Scythia";
        return "Unknown";
    }
}
