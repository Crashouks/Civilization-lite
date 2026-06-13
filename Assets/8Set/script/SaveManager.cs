using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const int SlotCount = 3;
    public const string SaveFileName = "civilization_save.json";
    public const string LoadOnStartKey = "LoadSaveGame";
    public const string ActiveSlotKey = "Save_ActiveSlot";
    public const string LoadSlotKey = "Save_LoadSlot";

    public static bool IsRestoringSave { get; set; }
    public static bool LoadedFromSaveThisSession { get; set; }
    public static string PendingLoadFailureMessage { get; set; }
    public bool HasUnsavedChanges { get; private set; } = true;

    public string LastCloudStatus { get; private set; } = "";

    CloudSaveClient cloudClient;

    public static int ActiveSlot
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 1), 1, SlotCount);
        set
        {
            PlayerPrefs.SetInt(ActiveSlotKey, Mathf.Clamp(value, 1, SlotCount));
            PlayerPrefs.Save();
        }
    }

    public static string GetSlotName(int slot) => "slot" + ClampSlot(slot);

    public static string GetSavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, "civilization_save_" + GetSlotName(slot) + ".json");

    public static string LegacySavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static string SavePath => GetSavePath(ActiveSlot);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cloudClient = GetComponent<CloudSaveClient>();
        if (cloudClient == null)
            cloudClient = gameObject.AddComponent<CloudSaveClient>();
    }

    static int ClampSlot(int slot)
    {
        return Mathf.Clamp(slot, 1, SlotCount);
    }

    public static bool HasSave()
    {
        for (int i = 1; i <= SlotCount; i++)
        {
            if (HasSave(i))
                return true;
        }
        return false;
    }

    public static bool HasSave(int slot)
    {
        slot = ClampSlot(slot);
        if (File.Exists(GetSavePath(slot)))
            return true;

        return slot == 1 && File.Exists(LegacySavePath);
    }

    public static SaveSlotInfo GetLocalSlotInfo(int slot)
    {
        slot = ClampSlot(slot);
        SaveSlotInfo info = new SaveSlotInfo { slot = slot };

        if (TryReadSave(slot, out GameSaveData data))
        {
            info.exists = true;
            info.turnNumber = data.currentTurn;
            info.playerCiv = data.playerCiv;
        }

        return info;
    }

    public SaveSlotInfo[] GetAllLocalSlotInfo()
    {
        var slots = new SaveSlotInfo[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            slots[i] = GetLocalSlotInfo(i + 1);
        return slots;
    }

    public static bool TryReadSave(out GameSaveData data) => TryReadSave(ActiveSlot, out data);

    public static bool TryReadSave(int slot, out GameSaveData data)
    {
        data = null;
        slot = ClampSlot(slot);

        string path = GetSavePath(slot);
        if (!File.Exists(path) && slot == 1 && File.Exists(LegacySavePath))
            path = LegacySavePath;

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
                return false;

            data = MigrateSaveData(data);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Не вдалося прочитати збереження слот " + slot + ": " + e.Message);
            return false;
        }
    }

    public static bool HasAnySaveAvailable()
    {
        return HasSave();
    }

    public static int FindFirstOccupiedSlot()
    {
        if (HasSave(ActiveSlot))
            return ActiveSlot;

        for (int i = 1; i <= SlotCount; i++)
        {
            if (HasSave(i))
                return i;
        }

        return -1;
    }

    public static void PrepareLoadFromSlot(int slot)
    {
        slot = ClampSlot(slot);
        ActiveSlot = slot;
        PlayerPrefs.SetInt(LoadOnStartKey, 1);
        PlayerPrefs.SetInt(LoadSlotKey, slot);

        if (TryReadSave(slot, out GameSaveData data) && !string.IsNullOrEmpty(data.playerCiv))
            PlayerPrefs.SetString("SelectedCiv", data.playerCiv);

        PlayerPrefs.Save();
    }

    public static bool IsCloudConfigured()
    {
        return DatabaseSettings.CloudSaveEnabled
            && !string.IsNullOrEmpty(DatabaseSettings.ApiBaseUrl)
            && !string.IsNullOrEmpty(DatabaseSettings.PlayerId);
    }

    public static GameSaveData PickNewerSave(GameSaveData local, GameSaveData cloud)
    {
        if (local == null) return cloud;
        if (cloud == null) return local;
        return cloud.currentTurn > local.currentTurn ? cloud : local;
    }

    public IEnumerator RestoreGameOnStart(Program1 manager, int slot)
    {
        slot = ClampSlot(slot);
        LoadedFromSaveThisSession = false;

        GameSaveData local = null;
        TryReadSave(slot, out local);

        GameSaveData cloud = null;
        if (cloudClient != null && cloudClient.IsConfigured())
        {
            bool done = false;
            cloudClient.DownloadSave(slot, (ok, message, data) =>
            {
                if (ok && data != null)
                    cloud = MigrateSaveData(data);
                done = true;
            });

            while (!done)
                yield return null;
        }

        GameSaveData best = PickNewerSave(local, cloud);
        if (best != null)
        {
            WriteSaveFile(best, slot);
            ApplySaveDataToManager(best, manager, slot);
            yield break;
        }

        Debug.LogWarning("Не вдалося завантажити слот " + slot + ", починаємо нову гру.");
        PendingLoadFailureMessage = "Не вдалося завантажити слот " + slot + ". Починається нова гра.";
        IsRestoringSave = false;
        LoadedFromSaveThisSession = false;
        if (manager.seed == 0)
            manager.seed = Random.Range(0f, 10000f);
        yield return manager.StartCoroutine(manager.GenerateMapRoutine());
    }

    public static void PrepareLoadFromSave()
    {
        int slot = PlayerPrefs.GetInt(LoadSlotKey, ActiveSlot);
        if (HasSave(slot))
            PrepareLoadFromSlot(slot);
        else
        {
            int fallback = FindFirstOccupiedSlot();
            if (fallback > 0)
                PrepareLoadFromSlot(fallback);
        }
    }

    public void MarkUnsaved() => HasUnsavedChanges = true;
    public void MarkSaved() => HasUnsavedChanges = false;

    public GameSaveData BuildSaveData()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        EconomyManager economy = EconomyManager.Instance;
        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        FogOfWarManager fog = FogOfWarManager.Instance;

        if (manager == null)
            return null;

        GameSaveData data = new GameSaveData
        {
            mapSeed = manager.seed,
            mapWidth = manager.width,
            mapHeight = manager.height,
            currentTurn = turnManager != null ? turnManager.currentTurn : 1,
            playerCiv = manager.currentCivName,
            playerCoins = economy != null ? economy.PlayerCoins : 0,
            isAtWar = diplomacy != null && diplomacy.enemyNations.Count > 0,
            enemyNations = diplomacy != null ? new List<string>(diplomacy.enemyNations) : new List<string>()
        };

        if (diplomacy != null)
        {
            diplomacy.ExportWarState(out List<string> warPairs, out List<string> eliminated);
            data.activeWarPairs = warPairs;
            data.eliminatedCivs = eliminated;
        }

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
                hasAttackedThisTurn = unit.hasAttackedThisTurn,
                isPlayer = unit.isPlayer,
                civName = unit.isPlayer
                    ? manager.currentCivName
                    : (!string.IsNullOrEmpty(unit.ownerCivName) ? unit.ownerCivName : ExtractCivName(unit.name))
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
                isCapital = city.isCapital,
                ownerCivName = city.ownerCivName,
                cityName = city.cityName,
                foundedTurn = city.foundedTurn,
                currentHealth = city.currentHealth
            });
        }

        if (economy != null)
            data.civCoins = economy.ExportAiTreasuries();

        return data;
    }

    public void SaveGame(int slot)
    {
        slot = ClampSlot(slot);
        ActiveSlot = slot;

        GameSaveData data = BuildSaveData();
        if (data == null)
        {
            Debug.LogError("Не вдалося зберегти: Program1 не знайдено");
            return;
        }

        WriteSaveFile(data, slot);
        HasUnsavedChanges = false;
        Debug.Log("Game saved locally: slot " + slot + " -> " + GetSavePath(slot));

        if (cloudClient != null && cloudClient.IsConfigured())
        {
            string json = JsonUtility.ToJson(data, true);
            cloudClient.UploadSave(slot, json, data.currentTurn, data.playerCiv, (ok, message) =>
            {
                LastCloudStatus = message;
                if (ok)
                    Debug.Log("Cloud save slot " + slot + ": " + message);
                else
                    Debug.LogWarning("Cloud save failed: " + message);
            });
        }
    }

    public void LoadGame(int slot, System.Action<bool> onComplete = null)
    {
        slot = ClampSlot(slot);
        ActiveSlot = slot;
        PlayerPrefs.SetInt(LoadSlotKey, slot);
        PlayerPrefs.Save();
        StartCoroutine(LoadGameRoutine(slot, onComplete));
    }

    IEnumerator LoadGameRoutine(int slot, System.Action<bool> onComplete)
    {
        GameSaveData local = null;
        TryReadSave(slot, out local);

        GameSaveData cloud = null;
        if (cloudClient != null && cloudClient.IsConfigured())
        {
            bool done = false;
            string message = "";
            cloudClient.DownloadSave(slot, (ok, msg, data) =>
            {
                message = msg;
                LastCloudStatus = msg;
                if (ok && data != null)
                    cloud = MigrateSaveData(data);
                done = true;
            });

            while (!done)
                yield return null;

            if (cloud == null && local == null)
                Debug.LogWarning("Cloud load slot " + slot + " failed: " + message);
        }

        GameSaveData best = PickNewerSave(local, cloud);
        if (best == null)
        {
            Debug.LogWarning("Слот " + slot + " порожній");
            onComplete?.Invoke(false);
            yield break;
        }

        WriteSaveFile(best, slot);
        ApplyLoadedGameFromData(best, slot);
        onComplete?.Invoke(true);
    }

    public void ApplyLoadedGameFromData(GameSaveData data)
    {
        ApplyLoadedGameFromData(data, ActiveSlot);
    }

    public void ApplyLoadedGameFromData(GameSaveData data, int slot)
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null || data == null)
        {
            Debug.LogError("Не вдалося застосувати збереження");
            return;
        }

        ApplySaveDataToManager(MigrateSaveData(data), manager, slot);
    }

    public void ApplyLoadedGame(Program1 manager)
    {
        int slot = PlayerPrefs.GetInt(LoadSlotKey, ActiveSlot);
        if (!TryReadSave(slot, out GameSaveData data))
        {
            Debug.LogWarning("Немає збереження для завантаження");
            return;
        }

        ApplySaveDataToManager(data, manager, slot);
    }

    void ApplySaveDataToManager(GameSaveData data, Program1 manager, int slot)
    {
        slot = ClampSlot(slot);
        ActiveSlot = slot;

        IsRestoringSave = true;
        LoadedFromSaveThisSession = true;

        manager.seed = data.mapSeed;
        manager.width = data.mapWidth > 0 ? data.mapWidth : manager.width;
        manager.height = data.mapHeight > 0 ? data.mapHeight : manager.height;
        manager.currentCivName = data.playerCiv;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetCoins(data.playerCoins);
            EconomyManager.Instance.ImportAiTreasuries(data.civCoins);
        }

        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
            turnManager.currentTurn = data.currentTurn;

        if (DiplomacyManager.Instance != null)
        {
            DiplomacyManager.Instance.ImportWarState(
                data.activeWarPairs,
                data.eliminatedCivs,
                data.enemyNations,
                data.isAtWar,
                manager,
                data.playerCiv);

            if (data.eliminatedCivs != null && !string.IsNullOrEmpty(data.playerCiv)
                && data.eliminatedCivs.Contains(data.playerCiv))
            {
                TurnManager tm = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
                tm?.SetPlayerDefeated(true);
            }
        }

        manager.ApplyCivFromName(data.playerCiv);
        PlayerPrefs.SetString("SelectedCiv", data.playerCiv ?? "Rome");
        PlayerPrefs.SetInt(LoadOnStartKey, 0);
        PlayerPrefs.SetInt(LoadSlotKey, slot);
        PlayerPrefs.Save();

        WriteSaveFile(data, slot);
        HasUnsavedChanges = false;

        manager.ClearAllGameObjects();
        manager.StartCoroutine(manager.RegenerateAndRestore(data));
    }

    void WriteSaveFile(GameSaveData data, int slot)
    {
        if (data == null) return;
        slot = ClampSlot(slot);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(slot), json);
    }

    public static GameSaveData MigrateSaveData(GameSaveData data)
    {
        if (data.enemyNations == null)
            data.enemyNations = new List<string>();
        if (data.activeWarPairs == null)
            data.activeWarPairs = new List<string>();
        if (data.eliminatedCivs == null)
            data.eliminatedCivs = new List<string>();
        if (data.units == null)
            data.units = new List<UnitSaveData>();
        if (data.cities == null)
            data.cities = new List<CitySaveData>();

        if (data.version < 3 && data.activeWarPairs.Count == 0 && data.enemyNations.Count > 0 && !string.IsNullOrEmpty(data.playerCiv))
        {
            foreach (string enemy in data.enemyNations)
            {
                if (string.IsNullOrEmpty(enemy))
                    continue;
                string key = string.CompareOrdinal(data.playerCiv, enemy) < 0
                    ? data.playerCiv + "|" + enemy
                    : enemy + "|" + data.playerCiv;
                if (!data.activeWarPairs.Contains(key))
                    data.activeWarPairs.Add(key);
            }
        }

        if (data.version < 1)
            Debug.LogWarning("Збереження без версії — застосовано міграцію до v3");

        data.version = 3;
        return data;
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
