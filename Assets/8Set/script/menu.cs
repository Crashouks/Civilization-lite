using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Панелі")]
    public GameObject mainMenuPanel;      // Головна панель (Start, Exit)
    public GameObject civSelectionPanel;  // Панель вибору (Rome, Rus, Carthage, Back)

    // Відкриває вікно вибору цивілізації
    public void OpenCivSelection()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (civSelectionPanel != null) civSelectionPanel.SetActive(true);
    }

    // Повертає з вибору цивілізації до головного меню
    public void BackToMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (civSelectionPanel != null) civSelectionPanel.SetActive(false);
    }

    // Викликається кнопками вибору нації
    public void SelectCivilization(string civName)
    {
        // Зберігаємо вибір для подальшого використання у грі (фарбування юнітів)
        PlayerPrefs.SetString("SelectedCiv", civName);
        PlayerPrefs.Save();

        Debug.Log("Вибрано: " + civName + ". Завантаження сцени...");

        // ЗАМНІТЬ "SampleScene" на назву вашої ігрової сцени
        SceneManager.LoadScene("Gamescena");
    }

    // Вихід з гри
    public void ExitGame()
    {
        Debug.Log("Вихід з гри...");
        Application.Quit();
    }
}