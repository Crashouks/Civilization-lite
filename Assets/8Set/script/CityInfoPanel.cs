using UnityEngine;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    public void ShowPanel(City city)
    {
        if (GameUI.Instance != null)
            GameUI.Instance.ShowCityPanel(city);
    }

    public void HidePanel()
    {
        if (GameUI.Instance != null)
            GameUI.Instance.HideCityPanel();
    }
}
