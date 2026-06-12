using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class MainMenu : MonoBehaviour
{
    public const string SaveFileName = "civilization_save.json";
    public const string LoadOnStartKey = "LoadSaveGame";
    public const string GameSceneName = "Gamescena";

    [Header("Legacy panels (removed on play)")]
    public GameObject mainMenuPanel;
    public GameObject civSelectionPanel;

    private CivMainMenuUI civUI;

    void Awake()
    {
        FixCanvas();
        FixBackgroundImage();
        GameSettings.ApplySavedSettings();
        RemoveLegacyUI();
        BuildCivStyleMenu();
    }

    void FixBackgroundImage()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        foreach (Transform child in canvas.transform)
        {
            if (child.name != "Image") continue;

            child.SetAsFirstSibling();
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    void FixCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        RectTransform rt = canvas.GetComponent<RectTransform>();
        if (rt != null && rt.localScale == Vector3.zero)
            rt.localScale = Vector3.one;
    }

    void RemoveLegacyUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name == "CivMenuRoot") continue;
            if (child.name == "Image" || child.name == "Background") continue;

            if (child.name == "MainMenu" || child.name == "CivSelectionPanel")
            {
                Destroy(child.gameObject);
                continue;
            }

            child.gameObject.SetActive(false);
        }

        mainMenuPanel = null;
        civSelectionPanel = null;
    }

    void BuildCivStyleMenu()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenu: Canvas not found");
            return;
        }

        Transform existing = canvas.transform.Find("CivMenuRoot");
        if (existing != null) Destroy(existing.gameObject);

        GameObject uiHost = new GameObject("CivMenuRoot");
        uiHost.transform.SetParent(canvas.transform, false);
        uiHost.transform.SetAsLastSibling();

        civUI = uiHost.AddComponent<CivMainMenuUI>();
        civUI.Build(this, canvas);
    }

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public bool HasSaveFile() => SaveManager.HasSave();

    public void LoadGame()
    {
        if (!SaveManager.TryReadSave(out GameSaveData data))
        {
            Debug.LogWarning("No save file found");
            return;
        }

        if (!string.IsNullOrEmpty(data.playerCiv))
            PlayerPrefs.SetString("SelectedCiv", data.playerCiv);

        PlayerPrefs.SetInt(LoadOnStartKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneName);
    }

    public void OpenCivSelection() => civUI?.ShowCivPanel();
    public void BackToMenu() => civUI?.ShowMainPanel();
    public void OpenSettings() => civUI?.ShowSettingsPanel();

    public void SelectCivilization(string civName)
    {
        PlayerPrefs.SetInt(LoadOnStartKey, 0);
        PlayerPrefs.SetString("SelectedCiv", civName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
