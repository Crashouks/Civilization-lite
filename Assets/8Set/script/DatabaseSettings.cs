using UnityEngine;

public static class DatabaseSettings
{
    public const string ApiUrlKey = "CloudSave_ApiUrl";
    public const string PlayerIdKey = "CloudSave_PlayerId";
    public const string CloudEnabledKey = "CloudSave_Enabled";

    public const string DefaultApiUrl = "http://localhost:3000";

    public static bool CloudSaveEnabled
    {
        get => PlayerPrefs.GetInt(CloudEnabledKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(CloudEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static string ApiBaseUrl
    {
        get => PlayerPrefs.GetString(ApiUrlKey, DefaultApiUrl).TrimEnd('/');
        set
        {
            PlayerPrefs.SetString(ApiUrlKey, value.TrimEnd('/'));
            PlayerPrefs.Save();
        }
    }

    public static string PlayerId
    {
        get
        {
            string saved = PlayerPrefs.GetString(PlayerIdKey, "");
            if (string.IsNullOrEmpty(saved))
            {
                saved = SystemInfo.deviceUniqueIdentifier;
                PlayerPrefs.SetString(PlayerIdKey, saved);
                PlayerPrefs.Save();
            }
            return saved;
        }
        set
        {
            PlayerPrefs.SetString(PlayerIdKey, value);
            PlayerPrefs.Save();
        }
    }
}
