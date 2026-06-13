using System.Collections.Generic;
using UnityEngine;

public enum WindowModeType
{
    Windowed = 0,
    Fullscreen = 1,
    Borderless = 2
}

public static class GameSettings
{
    public const string MasterVolumeKey = "MasterVolume";
    public const string VSyncKey = "VSync";
    public const string ResolutionWidthKey = "ResolutionWidth";
    public const string ResolutionHeightKey = "ResolutionHeight";
    public const string FullscreenKey = "Fullscreen";
    public const string WindowModeKey = "WindowMode";

    public static void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey, 1);

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height);

        if (width >= 800 && height >= 600)
            Screen.SetResolution(width, height, ToFullScreenMode(GetWindowMode()));
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

    public static WindowModeType GetWindowMode()
    {
        if (PlayerPrefs.HasKey(WindowModeKey))
            return (WindowModeType)Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey, 0), 0, 2);

        bool legacyFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        return legacyFullscreen ? WindowModeType.Borderless : WindowModeType.Windowed;
    }

    public static void SetWindowMode(WindowModeType mode)
    {
        PlayerPrefs.SetInt(WindowModeKey, (int)mode);
        PlayerPrefs.SetInt(FullscreenKey, mode == WindowModeType.Windowed ? 0 : 1);
        PlayerPrefs.Save();
        ApplyCurrentResolution();
    }

    public static WindowModeType CycleWindowMode()
    {
        WindowModeType next = (WindowModeType)(((int)GetWindowMode() + 1) % 3);
        SetWindowMode(next);
        return next;
    }

    public static string GetWindowModeLabel(WindowModeType mode)
    {
        switch (mode)
        {
            case WindowModeType.Fullscreen: return "Fullscreen";
            case WindowModeType.Borderless: return "Borderless";
            default: return "Windowed";
        }
    }

    public static FullScreenMode ToFullScreenMode(WindowModeType mode)
    {
        switch (mode)
        {
            case WindowModeType.Fullscreen:
                return FullScreenMode.ExclusiveFullScreen;
            case WindowModeType.Borderless:
                return FullScreenMode.FullScreenWindow;
            default:
                return FullScreenMode.Windowed;
        }
    }

    public static void ApplyCurrentResolution()
    {
        int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        if (width >= 800 && height >= 600)
            Screen.SetResolution(width, height, ToFullScreenMode(GetWindowMode()));
    }

    public static void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, ToFullScreenMode(GetWindowMode()));
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.Save();
    }

    public static void SetResolution(int width, int height, bool fullscreen)
    {
        SetWindowMode(fullscreen ? WindowModeType.Borderless : WindowModeType.Windowed);
        SetResolution(width, height);
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
