using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Панелі")]
    public GameObject mainMenuPanel;
    public GameObject civSelectionPanel;

    public void OpenCivSelection()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (civSelectionPanel != null) civSelectionPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (civSelectionPanel != null) civSelectionPanel.SetActive(false);
    }

    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.LogWarning("Немає збереженої гри");
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

        Debug.Log("Вибрано: " + civName + ". Завантаження сцени...");
        SceneManager.LoadScene("Gamescena");
    }

    public void ExitGame()
    {
        Debug.Log("Вихід з гри...");
        Application.Quit();
    }
}
