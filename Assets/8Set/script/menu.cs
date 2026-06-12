using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public const string SaveFileName = "civilization_save.json";
    public const string LoadOnStartKey = "LoadSaveGame";

    [Header("Legacy panels (auto-hidden)")]
    public GameObject mainMenuPanel;
    public GameObject civSelectionPanel;

    private CivMainMenuUI civUI;

    void Awake()
    [Header("UI Ïàíåë³")]
    public GameObject mainMenuPanel;
    public GameObject civSelectionPanel;

    public void OpenCivSelection()
    {
        FixCanvas();
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        HideLegacyUI();
        BuildCivStyleMenu();
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

    void HideLegacyUI()
    public void BackToMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (civSelectionPanel != null) civSelectionPanel.SetActive(false);

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        foreach (Transform child in canvas.transform)
        {
            // Keep background painting + our new menu root
            if (child.name == "Image" || child.name == "CivMenuRoot") continue;
            if (child.name == "MainMenu" || child.name == "CivSelectionPanel")
                child.gameObject.SetActive(false);
        }
    }

    void BuildCivStyleMenu()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found for main menu");
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

    public bool HasSaveFile() => File.Exists(SavePath);

    public void OpenCivSelection() => civUI?.ShowCivPanel();
    public void BackToMenu() => civUI?.ShowMainPanel();
    public void OpenSettings() => civUI?.ShowSettingsPanel();

    public void LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("No save file found");
            return;
        }
        PlayerPrefs.SetInt(LoadOnStartKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Gamescena");
    }

    public void SelectCivilization(string civName)
    {
        PlayerPrefs.SetInt(LoadOnStartKey, 0);
        PlayerPrefs.SetString("SelectedCiv", civName);
        PlayerPrefs.Save();
    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.LogWarning("Íåìàº çáåðåæåíî¿ ãðè");
            return;
        }

        PlayerPrefs.SetInt(SaveManager.LoadOnStartKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Gamescena");
    }

    public bool CanContinue()
    {
        return SaveManager.HasSave();
    }

    public void SelectCivilization(string civName)
    {
        SaveManager.LoadedFromSaveThisSession = false;

        PlayerPrefs.SetString("SelectedCiv", civName);
        PlayerPrefs.Save();

        Debug.Log("Âèáðàíî: " + civName + ". Çàâàíòàæåííÿ ñöåíè...");
        SceneManager.LoadScene("Gamescena");
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
