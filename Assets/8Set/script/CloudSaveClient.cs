using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudSaveClient : MonoBehaviour
{
    public static CloudSaveClient Instance { get; private set; }

    public string LastStatus { get; private set; } = "";
    public bool LastOperationSucceeded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsConfigured()
    {
        return DatabaseSettings.CloudSaveEnabled
            && !string.IsNullOrEmpty(DatabaseSettings.ApiBaseUrl)
            && !string.IsNullOrEmpty(DatabaseSettings.PlayerId);
    }

    public void CheckHealth(Action<bool, string> onComplete)
    {
        StartCoroutine(CheckHealthRoutine(onComplete));
    }

    IEnumerator CheckHealthRoutine(Action<bool, string> onComplete)
    {
        string url = DatabaseSettings.ApiBaseUrl + "/health";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 5;
        yield return request.SendWebRequest();

        bool ok = request.result == UnityWebRequest.Result.Success;
        string message = ok ? "Підключено до MySQL API" : request.error;
        LastStatus = message;
        LastOperationSucceeded = ok;
        onComplete?.Invoke(ok, message);
    }

    public void UploadSave(int slot, string saveJson, int turnNumber, string playerCiv, Action<bool, string> onComplete)
    {
        StartCoroutine(UploadSaveRoutine(slot, saveJson, turnNumber, playerCiv, onComplete));
    }

    IEnumerator UploadSaveRoutine(int slot, string saveJson, int turnNumber, string playerCiv, Action<bool, string> onComplete)
    {
        if (!IsConfigured())
        {
            onComplete?.Invoke(false, "Хмарне збереження вимкнено");
            yield break;
        }

        CloudSaveUploadRequest payload = new CloudSaveUploadRequest
        {
            playerId = DatabaseSettings.PlayerId,
            displayName = playerCiv,
            saveName = SaveManager.GetSlotName(slot),
            saveJson = saveJson,
            turnNumber = turnNumber,
            playerCiv = playerCiv
        };

        string body = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

        using UnityWebRequest request = new UnityWebRequest(DatabaseSettings.ApiBaseUrl + "/api/saves", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15;

        yield return request.SendWebRequest();

        bool ok = request.result == UnityWebRequest.Result.Success;
        string message = ok ? "Збережено в MySQL (слот " + slot + ")" : request.error;

        if (ok && !string.IsNullOrEmpty(request.downloadHandler.text))
        {
            CloudSaveSimpleResponse response = JsonUtility.FromJson<CloudSaveSimpleResponse>(request.downloadHandler.text);
            if (response != null && !string.IsNullOrEmpty(response.message))
                message = "Слот " + slot + ": " + response.message;
            if (response != null && !response.success)
            {
                ok = false;
                message = response.message;
            }
        }

        LastStatus = message;
        LastOperationSucceeded = ok;
        onComplete?.Invoke(ok, message);
    }

    public void DownloadSave(int slot, Action<bool, string, GameSaveData> onComplete)
    {
        StartCoroutine(DownloadSaveRoutine(slot, onComplete));
    }

    IEnumerator DownloadSaveRoutine(int slot, Action<bool, string, GameSaveData> onComplete)
    {
        if (!IsConfigured())
        {
            onComplete?.Invoke(false, "Хмарне збереження вимкнено", null);
            yield break;
        }

        string playerId = UnityWebRequest.EscapeURL(DatabaseSettings.PlayerId);
        string slotName = SaveManager.GetSlotName(slot);
        string url = DatabaseSettings.ApiBaseUrl + "/api/saves/" + playerId + "/slot/" + slotName;

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = request.responseCode == 404
                ? "Слот " + slot + " порожній у хмарі"
                : request.error;
            LastStatus = error;
            LastOperationSucceeded = false;
            onComplete?.Invoke(false, error, null);
            yield break;
        }

        CloudSaveDownloadResponse response = JsonUtility.FromJson<CloudSaveDownloadResponse>(request.downloadHandler.text);
        if (response == null || !response.success || string.IsNullOrEmpty(response.saveJson))
        {
            LastStatus = "Пошкоджені дані з сервера";
            LastOperationSucceeded = false;
            onComplete?.Invoke(false, LastStatus, null);
            yield break;
        }

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(response.saveJson);
        if (data == null)
        {
            LastStatus = "Не вдалося розібрати збереження";
            LastOperationSucceeded = false;
            onComplete?.Invoke(false, LastStatus, null);
            yield break;
        }

        LastStatus = "Завантажено слот " + slot + " з MySQL";
        LastOperationSucceeded = true;
        onComplete?.Invoke(true, LastStatus, data);
    }

    public void FetchSlotSummaries(Action<SaveSlotInfo[]> onComplete)
    {
        StartCoroutine(FetchSlotSummariesCoroutine(onComplete));
    }

    public static IEnumerator FetchSlotSummariesCoroutine(Action<SaveSlotInfo[]> onComplete)
    {
        var fallback = new SaveSlotInfo[SaveManager.SlotCount];
        for (int i = 0; i < SaveManager.SlotCount; i++)
            fallback[i] = SaveManager.GetLocalSlotInfo(i + 1);

        if (!SaveManager.IsCloudConfigured())
        {
            onComplete?.Invoke(fallback);
            yield break;
        }

        string url = DatabaseSettings.ApiBaseUrl + "/api/saves/" + UnityWebRequest.EscapeURL(DatabaseSettings.PlayerId) + "/slots";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onComplete?.Invoke(fallback);
            yield break;
        }

        CloudSlotsResponse response = JsonUtility.FromJson<CloudSlotsResponse>(request.downloadHandler.text);
        if (response == null || !response.success || response.slots == null)
        {
            onComplete?.Invoke(fallback);
            yield break;
        }

        var result = new SaveSlotInfo[SaveManager.SlotCount];
        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            result[i] = new SaveSlotInfo { slot = slot };

            foreach (CloudSlotEntry entry in response.slots)
            {
                if (entry == null || entry.saveName != SaveManager.GetSlotName(slot))
                    continue;

                result[i].exists = entry.exists;
                result[i].turnNumber = entry.turnNumber;
                result[i].playerCiv = entry.playerCiv;
                break;
            }

            if (!result[i].exists && fallback[i].exists)
                result[i] = fallback[i];
        }

        onComplete?.Invoke(result);
    }

    IEnumerator FetchSlotSummariesRoutine(Action<SaveSlotInfo[]> onComplete)
    {
        yield return FetchSlotSummariesCoroutine(onComplete);
    }

    [Serializable]
    class CloudSaveUploadRequest
    {
        public string playerId;
        public string displayName;
        public string saveName;
        public string saveJson;
        public int turnNumber;
        public string playerCiv;
    }

    [Serializable]
    class CloudSaveSimpleResponse
    {
        public bool success;
        public string message;
        public int saveId;
    }

    [Serializable]
    class CloudSaveDownloadResponse
    {
        public bool success;
        public string message;
        public int saveId;
        public string saveName;
        public string saveJson;
        public int turnNumber;
        public string playerCiv;
    }
}
