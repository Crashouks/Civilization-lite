using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityInfoPanel : MonoBehaviour
{
    public static CityInfoPanel Instance;

    [Header("UI Elements")]
    public GameObject panel;
    public TextMeshProUGUI cityNameText;
    public TextMeshProUGUI civNameText;
    public TextMeshProUGUI cityDescriptionText;
    public Button declareWarButton;
    public Button declarePeaceButton;

    private City currentCity;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Якщо UI елементи не призначені, створюємо їх програмно
        if (panel == null)
        {
            CreatePanelProgrammatically();
        }
    }

    void Start()
    {
        HidePanel();

        if (declareWarButton != null)
            declareWarButton.onClick.AddListener(OnDeclareWarClicked);

        if (declarePeaceButton != null)
            declarePeaceButton.onClick.AddListener(OnDeclarePeaceClicked);
    }

    void CreatePanelProgrammatically()
    {
        // Створюємо Canvas
        GameObject canvasObj = new GameObject("CityInfoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Створюємо Panel
        panel = new GameObject("CityInfoPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(300, 200);
        panelRect.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Назва міста
        GameObject cityNameObj = new GameObject("CityName");
        cityNameObj.transform.SetParent(panel.transform, false);
        RectTransform cityNameRect = cityNameObj.AddComponent<RectTransform>();
        cityNameRect.anchorMin = new Vector2(0.1f, 0.7f);
        cityNameRect.anchorMax = new Vector2(0.9f, 0.9f);
        cityNameRect.sizeDelta = Vector2.zero;
        cityNameText = cityNameObj.AddComponent<TextMeshProUGUI>();
        cityNameText.fontSize = 24;
        cityNameText.alignment = TextAlignmentOptions.Center;
        cityNameText.color = Color.white;

        // Назва цивілізації
        GameObject civNameObj = new GameObject("CivName");
        civNameObj.transform.SetParent(panel.transform, false);
        RectTransform civNameRect = civNameObj.AddComponent<RectTransform>();
        civNameRect.anchorMin = new Vector2(0.1f, 0.55f);
        civNameRect.anchorMax = new Vector2(0.9f, 0.7f);
        civNameRect.sizeDelta = Vector2.zero;
        civNameText = civNameObj.AddComponent<TextMeshProUGUI>();
        civNameText.fontSize = 18;
        civNameText.alignment = TextAlignmentOptions.Center;
        civNameText.color = new Color(0.8f, 0.8f, 0.9f, 1f);

        // Опис міста
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(panel.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.1f, 0.25f);
        descRect.anchorMax = new Vector2(0.9f, 0.55f);
        descRect.sizeDelta = Vector2.zero;
        cityDescriptionText = descObj.AddComponent<TextMeshProUGUI>();
        cityDescriptionText.fontSize = 14;
        cityDescriptionText.alignment = TextAlignmentOptions.Center;
        cityDescriptionText.color = new Color(0.7f, 0.7f, 0.8f, 1f);

        // Кнопка оголошення війни
        GameObject warBtnObj = new GameObject("DeclareWarButton");
        warBtnObj.transform.SetParent(panel.transform, false);
        RectTransform warBtnRect = warBtnObj.AddComponent<RectTransform>();
        warBtnRect.anchorMin = new Vector2(0.1f, 0.05f);
        warBtnRect.anchorMax = new Vector2(0.48f, 0.2f);
        warBtnRect.sizeDelta = Vector2.zero;
        declareWarButton = warBtnObj.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image warBtnImage = warBtnObj.AddComponent<UnityEngine.UI.Image>();
        warBtnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        GameObject warBtnTextObj = new GameObject("Text");
        warBtnTextObj.transform.SetParent(warBtnObj.transform, false);
        RectTransform warBtnTextRect = warBtnTextObj.AddComponent<RectTransform>();
        warBtnTextRect.anchorMin = Vector2.zero;
        warBtnTextRect.anchorMax = Vector2.one;
        warBtnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI warBtnText = warBtnTextObj.AddComponent<TextMeshProUGUI>();
        warBtnText.text = "Оголосити війну";
        warBtnText.fontSize = 14;
        warBtnText.alignment = TextAlignmentOptions.Center;
        warBtnText.color = Color.white;

        // Кнопка оголошення миру
        GameObject peaceBtnObj = new GameObject("DeclarePeaceButton");
        peaceBtnObj.transform.SetParent(panel.transform, false);
        RectTransform peaceBtnRect = peaceBtnObj.AddComponent<RectTransform>();
        peaceBtnRect.anchorMin = new Vector2(0.52f, 0.05f);
        peaceBtnRect.anchorMax = new Vector2(0.9f, 0.2f);
        peaceBtnRect.sizeDelta = Vector2.zero;
        declarePeaceButton = peaceBtnObj.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image peaceBtnImage = peaceBtnObj.AddComponent<UnityEngine.UI.Image>();
        peaceBtnImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        GameObject peaceBtnTextObj = new GameObject("Text");
        peaceBtnTextObj.transform.SetParent(peaceBtnObj.transform, false);
        RectTransform peaceBtnTextRect = peaceBtnTextObj.AddComponent<RectTransform>();
        peaceBtnTextRect.anchorMin = Vector2.zero;
        peaceBtnTextRect.anchorMax = Vector2.one;
        peaceBtnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI peaceBtnText = peaceBtnTextObj.AddComponent<TextMeshProUGUI>();
        peaceBtnText.text = "Оголосити мир";
        peaceBtnText.fontSize = 14;
        peaceBtnText.alignment = TextAlignmentOptions.Center;
        peaceBtnText.color = Color.white;
    }

    public void ShowPanel(City city)
    {
        Debug.Log("CityInfoPanel.ShowPanel викликано для: " + city.cityName);

        if (city == null)
        {
            Debug.LogWarning("city is null!");
            return;
        }

        currentCity = city;

        if (cityNameText != null)
            cityNameText.text = city.cityName;
        else
            Debug.LogWarning("cityNameText is null!");

        if (civNameText != null)
            civNameText.text = city.ownerCivName;
        else
            Debug.LogWarning("civNameText is null!");

        if (cityDescriptionText != null)
        {
            cityDescriptionText.text = GenerateCityDescription(city);
        }
        else
        {
            Debug.LogWarning("cityDescriptionText is null!");
        }

        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log("Panel активовано");
        }
        else
        {
            Debug.LogError("panel is null!");
        }
    }

    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
        currentCity = null;
    }

    private string GenerateCityDescription(City city)
    {
        string desc = $"Місто: {city.cityName}\n";
        desc += $"Власник: {city.ownerCivName}\n";
        desc += $"Столиця: {(city.isCapital ? "Так" : "Ні")}\n";
        desc += $"Позиція: {city.gridPosition}";
        return desc;
    }

    private void OnDeclareWarClicked()
    {
        if (currentCity != null)
        {
            Program1 manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null)
            {
                manager.selectedCityForWar = currentCity;
                manager.pendingWarTargetCiv = currentCity.ownerCivName;
                manager.DeclareWarOnSelectedCity();
            }
        }
        HidePanel();
    }

    private void OnDeclarePeaceClicked()
    {
        if (currentCity != null)
        {
            Program1 manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null)
            {
                // TODO: Implement peace declaration logic
                Debug.Log("Оголошено мир з " + currentCity.ownerCivName);
            }
        }
        HidePanel();
    }
}
