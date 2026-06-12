using System.Collections.Generic;
using UnityEngine;

public static class GameSettings
{
    public const string MasterVolumeKey = "MasterVolume";
    public const string VSyncKey = "VSync";
    public const string ResolutionWidthKey = "ResolutionWidth";
    public const string ResolutionHeightKey = "ResolutionHeight";
    public const string FullscreenKey = "Fullscreen";

    public static void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey, 1);

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (width >= 800 && height >= 600)
            Screen.SetResolution(width, height, fullscreen);
    }

    public static void SetMasterVolume(float volume)
    {
        float v = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, v);
        PlayerPrefs.Save();
        AudioListener.volume = v;
    }

    public static void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool GetVSync()
    {
        return PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
    }

    public static void SetResolution(int width, int height, bool fullscreen = true)
    {
        Screen.SetResolution(width, height, fullscreen);
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static List<Resolution> GetUniqueResolutions()
    {
        Resolution[] all = Screen.resolutions;
        var result = new List<Resolution>();

        if (all == null || all.Length == 0)
        {
            result.Add(new Resolution { width = 1280, height = 720 });
            result.Add(new Resolution { width = 1366, height = 768 });
            result.Add(new Resolution { width = 1600, height = 900 });
            result.Add(new Resolution { width = 1920, height = 1080 });
            result.Add(new Resolution { width = 2560, height = 1440 });
            return result;
        }

        var best = new Dictionary<string, Resolution>();
        foreach (Resolution r in all)
        {
            if (r.width < 800 || r.height < 600) continue;

            string key = r.width + "x" + r.height;
            if (!best.ContainsKey(key) || r.refreshRateRatio.value > best[key].refreshRateRatio.value)
                best[key] = r;
        }

        if (best.Count == 0)
        {
            result.Add(new Resolution { width = 1280, height = 720 });
            result.Add(new Resolution { width = 1920, height = 1080 });
            return result;
        }

        result.AddRange(best.Values);
        result.Sort((a, b) =>
        {
            int cmp = a.width.CompareTo(b.width);
            return cmp != 0 ? cmp : a.height.CompareTo(b.height);
        });
        return result;
    }

    public static int FindResolutionIndex(List<Resolution> resolutions)
    {
        if (resolutions == null || resolutions.Count == 0) return 0;

        int savedW = PlayerPrefs.GetInt(ResolutionWidthKey, 0);
        int savedH = PlayerPrefs.GetInt(ResolutionHeightKey, 0);

        if (savedW >= 800 && savedH >= 600)
        {
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].width == savedW && resolutions[i].height == savedH)
                    return i;
            }
        }

        int currentW = Screen.width;
        int currentH = Screen.height;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == currentW && resolutions[i].height == currentH)
                return i;
        }

        for (int i = resolutions.Count - 1; i >= 0; i--)
        {
            if (resolutions[i].width <= currentW && resolutions[i].height <= currentH)
                return i;
        }

        return resolutions.Count - 1;
    }
}
