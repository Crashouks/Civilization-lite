using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private TextMeshProUGUI coinsText;
    private TextMeshProUGUI turnText;
    private GameObject spawnPanel;
    private City selectedSpawnCity;
    private TextMeshProUGUI spawnCityText;
    private TextMeshProUGUI spawnScoutCountText;
    private TextMeshProUGUI spawnWarriorCountText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        EnsureManagersExist();
        BuildUI();
        BindEvents();
        RefreshAll();
    }

    void EnsureManagersExist()
    {
        if (EconomyManager.Instance == null)
            new GameObject("EconomyManager").AddComponent<EconomyManager>();
        if (SaveManager.Instance == null)
            new GameObject("SaveManager").AddComponent<SaveManager>();
        if (FogOfWarManager.Instance == null)
            new GameObject("FogOfWarManager").AddComponent<FogOfWarManager>();
    }

    void BuildUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject hud = new GameObject("GameHUD");
        hud.transform.SetParent(canvas.transform, false);
        RectTransform hudRect = hud.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(20, -20);
        hudRect.sizeDelta = new Vector2(420, 120);

        coinsText = CreateLabel(hud.transform, "Монети: 0", new Vector2(0, 0), 22);
        turnText = CreateLabel(hud.transform, "Хід: 1", new Vector2(0, -35), 18);

        CreateButton(hud.transform, "Зберегти", new Vector2(0, -70), new Vector2(110, 32), OnSaveClicked);
        CreateButton(hud.transform, "Завантажити", new Vector2(120, -70), new Vector2(130, 32), OnLoadClicked);

        spawnPanel = CreateSpawnPanel(canvas.transform);
        spawnPanel.SetActive(false);
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 pos, float fontSize)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 30);

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        return label;
    }

    Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(label + "Button");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return button;
    }

    GameObject CreateSpawnPanel(Transform canvas)
    {
        GameObject panel = new GameObject("SpawnPanel");
        panel.transform.SetParent(canvas, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);
        rect.sizeDelta = new Vector2(280, 0);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.94f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLayoutText(panel.transform, "Найм юнітів", 20, FontStyles.Bold, 28);
        spawnCityText = CreateLayoutText(panel.transform, "Місто:", 17, FontStyles.Normal, 24);
        spawnScoutCountText = CreateLayoutText(panel.transform, "Розвідники: 0/4", 15, FontStyles.Normal, 22);
        CreateLayoutButton(panel.transform, "Розвідник (80)", 38, () => TrySpawn(UnitTypeHelper.UnitKind.Scout));
        spawnWarriorCountText = CreateLayoutText(panel.transform, "Воїни: 0/20", 15, FontStyles.Normal, 22);
        CreateLayoutButton(panel.transform, "Воїн (100)", 38, () => TrySpawn(UnitTypeHelper.UnitKind.Warrior));
        CreateLayoutButton(panel.transform, "Закрити", 36, HideSpawnPanel);

        return panel;
    }

    TextMeshProUGUI CreateLayoutText(Transform parent, string text, float fontSize, FontStyles style, float height)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    void CreateLayoutButton(Transform parent, string label, float height, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(label + "Button");
        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;

        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.24f, 0.95f);

        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void BindEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnCoinsChanged += _ => RefreshCoins();
    }

    void RefreshAll()
    {
        RefreshCoins();
        RefreshTurn();
    }

    void RefreshCoins()
    {
        if (coinsText == null) return;
        int coins = EconomyManager.Instance != null ? EconomyManager.Instance.PlayerCoins : 0;
        coinsText.text = "Монети: " + coins;
    }

    public void RefreshTurn()
    {
        if (turnText == null) return;
        TurnManager tm = Object.FindAnyObjectByType<TurnManager>();
        int turn = tm != null ? tm.currentTurn : 1;
        turnText.text = "Хід: " + turn;
    }

    public void ShowSpawnPanel(City city)
    {
        if (city == null) return;

        city.EnsureDisplayName();
        selectedSpawnCity = city;
        if (spawnPanel != null)
        {
            spawnPanel.SetActive(true);
            UpdateSpawnInfo();
        }
    }

    public void HideSpawnPanel()
    {
        selectedSpawnCity = null;
        if (spawnPanel != null)
            spawnPanel.SetActive(false);
    }

    void UpdateSpawnInfo()
    {
        if (selectedSpawnCity == null) return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null) return;

        if (spawnCityText != null)
            spawnCityText.text = "Місто: " + selectedSpawnCity.GetDisplayName();

        if (spawnScoutCountText != null)
            spawnScoutCountText.text = "Розвідники: " + manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Scout) + "/" + manager.maxScouts;

        if (spawnWarriorCountText != null)
            spawnWarriorCountText.text = "Воїни: " + manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Warrior) + "/" + manager.maxWarriors;
    }

    void TrySpawn(UnitTypeHelper.UnitKind kind)
    {
        if (selectedSpawnCity == null) return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null) return;

        bool success = manager.TrySpawnUnitFromCity(selectedSpawnCity, kind);
        if (success)
            UpdateSpawnInfo();
    }

    void OnSaveClicked()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    void OnLoadClicked()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadGame();
    }
}
