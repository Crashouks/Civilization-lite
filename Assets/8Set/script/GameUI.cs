using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    static readonly Color Gold = new Color(0.95f, 0.82f, 0.42f, 1f);
    static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f, 1f);
    static readonly Color TitleFill = new Color(1f, 0.97f, 0.9f, 1f);
    static readonly Color PanelBg = new Color(0.1f, 0.07f, 0.04f, 1f);
    static readonly Color BtnBg = new Color(0.12f, 0.08f, 0.05f, 0.92f);
    static readonly Color BtnBorder = new Color(0.85f, 0.68f, 0.25f, 0.85f);
    static readonly Color MutedText = new Color(0.82f, 0.76f, 0.66f, 1f);
    static readonly Color SectionBg = new Color(0.14f, 0.1f, 0.06f, 0.95f);

    const float PanelWidth = 280f;
    const float PanelBottomMargin = 18f;
    const float PanelRightMargin = 18f;
    const float MenuButtonHeight = 46f;

    static TMP_FontAsset titleFont;
    static TMP_FontAsset bodyFont;
    static Sprite whiteSprite;

    private TextMeshProUGUI coinsText;
    private TextMeshProUGUI upkeepText;
    private TextMeshProUGUI turnText;
    private GameObject cityPanel;
    private RectTransform cityPanelRect;
    private GameObject pauseMenu;
    private bool pauseMenuOpen;
    private City selectedCity;

    private TextMeshProUGUI cityTitleText;
    private TextMeshProUGUI cityInfoText;
    private TextMeshProUGUI spawnScoutCountText;
    private TextMeshProUGUI spawnWarriorCountText;
    private GameObject recruitmentSection;
    private GameObject diplomacySection;
    private Button declareWarButton;
    private Button declarePeaceButton;
    private GameConfirmDialog confirmDialog;
    private SaveSlotsPanel saveSlotsPanel;
    private InGameSettingsPanel settingsPanel;

    public bool IsPauseMenuOpen => pauseMenuOpen;
    public bool IsCityPanelOpen => cityPanel != null && cityPanel.activeSelf;

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
        FindFonts();
        CleanupLegacyUI();
        EnsureManagersExist();
        BuildUI();
        BindEvents();
        RefreshAll();

        if (!string.IsNullOrEmpty(SaveManager.PendingLoadFailureMessage))
        {
            string msg = SaveManager.PendingLoadFailureMessage;
            SaveManager.PendingLoadFailureMessage = null;
            confirmDialog?.Show(msg, () => { });
        }
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (confirmDialog != null && confirmDialog.IsOpen)
        {
            confirmDialog.Hide();
            return;
        }

        if (saveSlotsPanel != null && saveSlotsPanel.IsOpen)
        {
            saveSlotsPanel.Hide();
            return;
        }

        if (settingsPanel != null && settingsPanel.IsOpen)
        {
            settingsPanel.Hide();
            return;
        }

        TogglePauseMenu();
    }

    void CleanupLegacyUI()
    {
        GameObject legacyCanvas = GameObject.Find("CityInfoCanvas");
        if (legacyCanvas != null)
            Destroy(legacyCanvas);
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

    static void FindFonts()
    {
        if (titleFont != null && bodyFont != null)
            return;

        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in allFonts)
        {
            if (font == null) continue;
            if (font.name.Contains("Holos-Bold") && !font.name.Contains("Outline"))
            {
                titleFont = font;
                bodyFont = font;
                break;
            }
        }

        if (bodyFont == null)
            bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (titleFont == null)
            titleFont = bodyFont;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        return whiteSprite;
    }

    static void ApplyFont(TextMeshProUGUI label, bool title = false)
    {
        if (label == null) return;
        TMP_FontAsset font = title ? titleFont : bodyFont;
        if (font != null)
            label.font = font;
    }

    public static Sprite GetSharedWhiteSprite() => GetWhiteSprite();

    public static void ApplySharedFont(TextMeshProUGUI label, bool title = false) => ApplyFont(label, title);

    static Transform GetPanelContent(Transform panel)
    {
        return panel != null ? panel.Find("Inner/Content") : null;
    }

    void BuildUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        BuildHud(canvas.transform);
        pauseMenu = BuildPauseMenu(canvas.transform);
        pauseMenu.SetActive(false);

        cityPanel = CreateCityPanel(canvas.transform);
        cityPanelRect = cityPanel.GetComponent<RectTransform>();
        PositionCityPanelBottomRight();
        cityPanel.SetActive(false);

        confirmDialog = gameObject.AddComponent<GameConfirmDialog>();
        confirmDialog.Build(canvas.transform);

        saveSlotsPanel = gameObject.AddComponent<SaveSlotsPanel>();
        saveSlotsPanel.Build(canvas.transform);

        settingsPanel = gameObject.AddComponent<InGameSettingsPanel>();
        settingsPanel.Build(pauseMenu.transform);
        settingsPanel.OnClosed = ShowPauseMenuPanel;
    }

    void BuildHud(Transform canvas)
    {
        GameObject hud = new GameObject("GameHUD");
        hud.transform.SetParent(canvas, false);
        RectTransform hudRect = hud.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(18, -18);
        hudRect.sizeDelta = new Vector2(280, 0);

        Image hudBg = hud.AddComponent<Image>();
        hudBg.sprite = GetWhiteSprite();
        hudBg.color = new Color(0.1f, 0.07f, 0.04f, 0.82f);

        Outline hudOutline = hud.AddComponent<Outline>();
        hudOutline.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.35f);
        hudOutline.effectDistance = new Vector2(1f, -1f);

        ContentSizeFitter hudFitter = hud.AddComponent<ContentSizeFitter>();
        hudFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        hudFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup hudLayout = hud.AddComponent<VerticalLayoutGroup>();
        hudLayout.padding = new RectOffset(12, 12, 10, 10);
        hudLayout.spacing = 4;
        hudLayout.childAlignment = TextAnchor.UpperLeft;
        hudLayout.childControlWidth = true;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = true;
        hudLayout.childForceExpandHeight = false;

        coinsText = CreateHudLine(hud.transform, "Монети: 0", 20, Gold);
        upkeepText = CreateHudLine(hud.transform, "Утримання: 0 мон/хід", 15, MutedText);
        upkeepText.gameObject.SetActive(false);
        turnText = CreateHudLine(hud.transform, "Хід: 1", 17, Cream);
    }

    TextMeshProUGUI CreateHudLine(Transform parent, string text, float fontSize, Color color)
    {
        GameObject obj = new GameObject("Line");
        obj.transform.SetParent(parent, false);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 6f;
        le.preferredHeight = fontSize + 6f;

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        ApplyFont(label, true);
        return label;
    }

    GameObject BuildPauseMenu(Transform canvas)
    {
        GameObject root = new GameObject("PauseMenu", typeof(RectTransform));
        root.transform.SetParent(canvas, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);
        dim.raycastTarget = true;

        GameObject panel = CreateStyledPanel(root.transform, "PausePanel", 420f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Transform content = GetPanelContent(panel.transform);
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        if (contentLayout != null)
        {
            contentLayout.padding = new RectOffset(32, 32, 28, 36);
            contentLayout.spacing = 10;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
        }

        CreateLayoutText(content, "Меню", 26, FontStyles.Bold, 34, TitleFill, true);
        CreateGoldDivider(content);
        CreateLayoutButton(content, "Продовжити", MenuButtonHeight, () => SetPauseMenuOpen(false));
        CreateLayoutButton(content, "Зберегти", MenuButtonHeight, OnSaveClicked);
        CreateLayoutButton(content, "Завантажити", MenuButtonHeight, OnLoadClicked);
        CreateLayoutButton(content, "Налаштування", MenuButtonHeight, OnSettingsClicked);
        CreateLayoutButton(content, "Вийти", MenuButtonHeight, OnQuitClicked);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        return root;
    }

    void TogglePauseMenu()
    {
        SetPauseMenuOpen(!pauseMenuOpen);
    }

    void SetPauseMenuOpen(bool open)
    {
        if (open && cityPanel != null && cityPanel.activeSelf)
            HideCityPanel();

        pauseMenuOpen = open;

        if (!open && settingsPanel != null && settingsPanel.IsOpen)
            settingsPanel.Hide();

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(open);
            if (open)
            {
                pauseMenu.transform.SetAsLastSibling();
                ShowPauseMenuPanel();
                Transform panel = pauseMenu.transform.Find("PausePanel");
                if (panel != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
            }
        }

        if (open)
            SetRightButtonsVisible(false);
        else if (!IsCityPanelOpen)
            SetRightButtonsVisible(true);
    }

    void ShowPauseMenuPanel()
    {
        if (!pauseMenuOpen || pauseMenu == null)
            return;

        Transform panel = pauseMenu.transform.Find("PausePanel");
        if (panel != null)
            panel.gameObject.SetActive(true);
    }

    void HidePauseMenuPanel()
    {
        if (pauseMenu == null)
            return;

        Transform panel = pauseMenu.transform.Find("PausePanel");
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    void PositionCityPanelBottomRight()
    {
        if (cityPanelRect == null) return;

        cityPanelRect.anchorMin = new Vector2(1, 0);
        cityPanelRect.anchorMax = new Vector2(1, 0);
        cityPanelRect.pivot = new Vector2(1, 0);
        cityPanelRect.anchoredPosition = new Vector2(-PanelRightMargin, PanelBottomMargin);
        cityPanelRect.sizeDelta = new Vector2(PanelWidth, 0);
    }

    void SetRightButtonsVisible(bool visible)
    {
        GameObject rightButtons = FindRightButtons();
        if (rightButtons != null)
            rightButtons.SetActive(visible);

        TurnManager turnManager = TurnManager.Instance ?? Object.FindAnyObjectByType<TurnManager>();
        turnManager?.RefreshEndTurnButtons();
    }

    static GameObject FindRightButtons()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null)
        {
            GameObject found = manager.FindObjectByNameInScenePublic("RightButtons");
            if (found != null)
                return found;
        }

        return GameObject.Find("RightButtons");
    }

    GameObject CreateStyledPanel(Transform parent, string name, float width)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 0);

        Image border = panel.AddComponent<Image>();
        border.sprite = GetWhiteSprite();
        border.color = BtnBorder;

        LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
        panelLayout.minWidth = width;
        panelLayout.preferredWidth = width;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup panelLayoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        panelLayoutGroup.padding = new RectOffset(2, 2, 2, 2);
        panelLayoutGroup.spacing = 0;
        panelLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        panelLayoutGroup.childControlWidth = true;
        panelLayoutGroup.childControlHeight = true;
        panelLayoutGroup.childForceExpandWidth = true;
        panelLayoutGroup.childForceExpandHeight = false;

        GameObject inner = new GameObject("Inner", typeof(RectTransform));
        inner.transform.SetParent(panel.transform, false);

        Image bg = inner.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.color = PanelBg;

        LayoutElement innerLayout = inner.AddComponent<LayoutElement>();
        innerLayout.minWidth = width - 4f;
        innerLayout.preferredWidth = width - 4f;
        innerLayout.flexibleWidth = 1f;

        VerticalLayoutGroup innerLayoutGroup = inner.AddComponent<VerticalLayoutGroup>();
        innerLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        innerLayoutGroup.spacing = 0;
        innerLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        innerLayoutGroup.childControlWidth = true;
        innerLayoutGroup.childControlHeight = true;
        innerLayoutGroup.childForceExpandWidth = true;
        innerLayoutGroup.childForceExpandHeight = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(inner.transform, false);

        LayoutElement contentLayoutElement = content.AddComponent<LayoutElement>();
        contentLayoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel;
    }

    GameObject CreateCityPanel(Transform canvas)
    {
        GameObject panel = CreateStyledPanel(canvas, "CityPanel", PanelWidth);
        cityPanelRect = panel.GetComponent<RectTransform>();
        PositionCityPanelBottomRight();

        Transform content = GetPanelContent(panel.transform);

        int scoutCost = EconomyManager.Instance != null ? EconomyManager.Instance.scoutCost : 50;
        int warriorCost = EconomyManager.Instance != null ? EconomyManager.Instance.warriorCost : 100;
        int settlerCost = EconomyManager.Instance != null ? EconomyManager.Instance.settlerCost : 150;

        cityTitleText = CreateLayoutText(content, "Місто", 22, FontStyles.Bold, 32, TitleFill, true);
        CreateGoldDivider(content);
        cityInfoText = CreateInfoText(content);
        CreateSectionDivider(content, "Найм юнітів");

        recruitmentSection = CreateSection(content);
        spawnScoutCountText = CreateLayoutText(recruitmentSection.transform, "Розвідники: 0", 14, FontStyles.Normal, 22, Cream, false);
        CreateLayoutButton(recruitmentSection.transform, "Розвідник (" + scoutCost + ")", 38, () => TrySpawn(UnitTypeHelper.UnitKind.Scout));
        spawnWarriorCountText = CreateLayoutText(recruitmentSection.transform, "Воїни: 0", 14, FontStyles.Normal, 22, Cream, false);
        CreateLayoutButton(recruitmentSection.transform, "Воїн (" + warriorCost + ")", 38, () => TrySpawn(UnitTypeHelper.UnitKind.Warrior));
        CreateLayoutButton(recruitmentSection.transform, "Поселенець (" + settlerCost + ")", 38, () => TrySpawn(UnitTypeHelper.UnitKind.Settler));

        diplomacySection = CreateSection(content);
        declareWarButton = CreateLayoutButton(diplomacySection.transform, "Оголосити війну", 38, OnDeclareWarClicked, war: true);
        declarePeaceButton = CreateLayoutButton(diplomacySection.transform, "Оголосити мир", 38, OnDeclarePeaceClicked, peace: true);

        CreateLayoutButton(content, "Закрити", 38, HideCityPanel);

        return panel;
    }

    void CreateGoldDivider(Transform parent)
    {
        GameObject line = new GameObject("Divider");
        line.transform.SetParent(parent, false);
        LayoutElement le = line.AddComponent<LayoutElement>();
        le.minHeight = 2f;
        le.preferredHeight = 2f;
        Image img = line.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
    }

    void CreateSectionDivider(Transform parent, string title)
    {
        GameObject block = new GameObject("SectionHeader");
        block.transform.SetParent(parent, false);

        LayoutElement le = block.AddComponent<LayoutElement>();
        le.minHeight = 30f;
        le.preferredHeight = 30f;

        Image bg = block.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.color = SectionBg;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(block.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = title;
        label.fontSize = 15;
        label.fontStyle = FontStyles.Bold;
        label.color = Gold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyFont(label, true);
    }

    GameObject CreateSection(Transform parent)
    {
        GameObject section = new GameObject("Section");
        section.transform.SetParent(parent, false);

        LayoutElement sectionLayout = section.AddComponent<LayoutElement>();
        sectionLayout.flexibleWidth = 1f;

        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return section;
    }

    TextMeshProUGUI CreateLayoutText(Transform parent, string text, float fontSize, FontStyles style, float height, Color color, bool title)
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
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        ApplyFont(label, title);
        return label;
    }

    TextMeshProUGUI CreateInfoText(Transform parent)
    {
        GameObject obj = new GameObject("InfoText");
        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = 78f;
        layout.preferredHeight = 78f;

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = "";
        label.fontSize = 14;
        label.color = MutedText;
        label.lineSpacing = 2f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        ApplyFont(label);
        return label;
    }

    Button CreateLayoutButton(Transform parent, string label, float height, UnityEngine.Events.UnityAction onClick, bool war = false, bool peace = false)
    {
        GameObject btnRoot = new GameObject(label + "Button");
        btnRoot.transform.SetParent(parent, false);

        LayoutElement layout = btnRoot.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;

        Image border = btnRoot.AddComponent<Image>();
        border.sprite = GetWhiteSprite();
        border.color = war
            ? new Color(0.55f, 0.18f, 0.12f, 0.9f)
            : peace
                ? new Color(0.18f, 0.42f, 0.2f, 0.9f)
                : BtnBorder;

        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(btnRoot.transform, false);
        RectTransform innerRect = inner.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(1.5f, 1.5f);
        innerRect.offsetMax = new Vector2(-1.5f, -1.5f);

        Image bg = inner.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.color = BtnBg;

        Button button = btnRoot.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inner.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Cream;
        tmp.fontStyle = FontStyles.Bold;
        ApplyFont(tmp);

        return button;
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
        int coins = EconomyManager.Instance != null ? EconomyManager.Instance.PlayerCoins : 0;
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        int upkeep = EconomyManager.Instance != null && manager != null
            ? EconomyManager.Instance.GetPlayerUnitUpkeep(manager)
            : 0;

        if (coinsText != null)
            coinsText.text = "Монети: " + coins;

        if (upkeepText != null)
        {
            bool showUpkeep = upkeep > 0;
            upkeepText.gameObject.SetActive(showUpkeep);
            if (showUpkeep)
                upkeepText.text = "Утримання: " + upkeep + " мон/хід";
        }
    }

    public void RefreshTurn()
    {
        if (turnText == null) return;
        TurnManager tm = Object.FindAnyObjectByType<TurnManager>();
        int turn = tm != null ? tm.currentTurn : 1;

        if (tm != null && !tm.IsPlayerTurnActive)
            turnText.text = "Хід ШІ...";
        else
            turnText.text = "Хід: " + turn;
    }

    public void ShowDefeatMessage()
    {
        confirmDialog?.Show("Вашу цивілізацію знищено!", () => { });
    }

    public void ShowWarDeclaredBy(string aggressorCiv)
    {
        if (string.IsNullOrEmpty(aggressorCiv))
            return;

        confirmDialog?.Show(aggressorCiv + " оголосив вам війну!", () => { });
    }

    public void ShowCityPanel(City city)
    {
        if (city == null || cityPanel == null) return;

        city.EnsureDisplayName();
        selectedCity = city;
        SetPauseMenuOpen(false);
        SetRightButtonsVisible(false);
        PositionCityPanelBottomRight();
        cityPanel.SetActive(true);
        cityPanel.transform.SetAsLastSibling();
        UpdateCityPanel();
    }

    public void HideCityPanel()
    {
        selectedCity = null;
        if (cityPanel != null)
            cityPanel.SetActive(false);
        SetRightButtonsVisible(true);
    }

    public bool IsShowingCity(City city) => selectedCity == city;

    public void RefreshCityPanel()
    {
        if (selectedCity != null && cityPanel != null && cityPanel.activeSelf)
            UpdateCityPanel();
    }

    public void ShowSpawnPanel(City city) => ShowCityPanel(city);
    public void HideSpawnPanel() => HideCityPanel();

    void UpdateCityPanel()
    {
        if (selectedCity == null) return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        bool isOwnCity = selectedCity.IsOwnedByPlayer();

        if (cityTitleText != null)
            cityTitleText.text = selectedCity.GetDisplayName();

        if (cityInfoText != null)
        {
            TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
            int currentTurn = turnManager != null ? turnManager.currentTurn : 1;
            int citizens = selectedCity.GetCitizenCount(currentTurn);
            int income = selectedCity.GetIncome(currentTurn);
            selectedCity.RefreshHealth(currentTurn);

            cityInfoText.text =
                "Власник: " + selectedCity.ownerCivName + "\n" +
                "Столиця: " + (selectedCity.isCapital ? "Так" : "Ні") + "\n" +
                "Громадяни: " + citizens + "\n" +
                "Здоров'я: " + selectedCity.currentHealth + "/" + selectedCity.maxHealth + "\n" +
                "Дохід: " + income + " монет/хід\n" +
                "Засновано: хід " + (selectedCity.foundedTurn > 0 ? selectedCity.foundedTurn.ToString() : "?") + "\n" +
                "Позиція: " + selectedCity.gridPosition.x + ", " + selectedCity.gridPosition.y;
        }

        Transform sectionHeader = cityPanel.transform.Find("Inner/Content/SectionHeader");
        if (sectionHeader != null)
            sectionHeader.gameObject.SetActive(isOwnCity);

        if (recruitmentSection != null)
            recruitmentSection.SetActive(isOwnCity);

        if (diplomacySection != null)
            diplomacySection.SetActive(!isOwnCity);

        if (isOwnCity && manager != null)
        {
            EconomyManager economy = EconomyManager.Instance;
            int scouts = manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Scout);
            int warriors = manager.CountPlayerUnitsOfKind(UnitTypeHelper.UnitKind.Warrior);
            int scoutUpkeep = economy != null ? economy.GetScoutUpkeep(manager) : 0;
            int warriorUpkeep = economy != null ? economy.GetWarriorUpkeep(manager) : 0;

            if (spawnScoutCountText != null)
                spawnScoutCountText.text = "Розвідники: " + scouts + (scoutUpkeep > 0 ? " · утрим. " + scoutUpkeep + "/хід" : "");
            if (spawnWarriorCountText != null)
                spawnWarriorCountText.text = "Воїни: " + warriors + (warriorUpkeep > 0 ? " · утрим. " + warriorUpkeep + "/хід" : "");
        }

        if (!isOwnCity)
        {
            bool atWar = DiplomacyManager.Instance != null
                && DiplomacyManager.Instance.IsAtWarWith(selectedCity.ownerCivName);

            if (declareWarButton != null)
                declareWarButton.gameObject.SetActive(!atWar);
            if (declarePeaceButton != null)
                declarePeaceButton.gameObject.SetActive(atWar);
        }
    }

    void TrySpawn(UnitTypeHelper.UnitKind kind)
    {
        if (selectedCity == null) return;

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null || !manager.CanPlayerAct()) return;

        if (manager.TrySpawnUnitFromCity(selectedCity, kind))
        {
            RefreshCoins();
            UpdateCityPanel();
        }
    }

    void OnDeclareWarClicked()
    {
        if (selectedCity == null || selectedCity.IsOwnedByPlayer())
        {
            HideCityPanel();
            return;
        }

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null || !manager.CanPlayerAct())
            return;

        manager.selectedCityForWar = selectedCity;
        manager.pendingWarTargetCiv = selectedCity.ownerCivName;
        manager.DeclareWarOnSelectedCity();

        HideCityPanel();
    }

    void OnDeclarePeaceClicked()
    {
        if (selectedCity == null || selectedCity.IsOwnedByPlayer())
        {
            HideCityPanel();
            return;
        }

        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null || !manager.CanPlayerAct())
        {
            HideCityPanel();
            return;
        }

        string targetCiv = selectedCity.ownerCivName;
        if (DiplomacyManager.Instance != null)
            DiplomacyManager.Instance.MakePeace(targetCiv);

        manager.HideWarButton();
        HideCityPanel();
    }

    void OnSaveClicked()
    {
        saveSlotsPanel?.ShowForSave(slot =>
        {
            if (SaveManager.HasSave(slot))
            {
                confirmDialog?.Show("Перезаписати слот " + slot + "?", () =>
                {
                    SaveManager.Instance?.SaveGame(slot);
                });
                return;
            }

            SaveManager.Instance?.SaveGame(slot);
        });
    }

    void OnLoadClicked()
    {
        saveSlotsPanel?.ShowForLoad(slot =>
        {
            void DoLoad()
            {
                SaveManager.Instance?.LoadGame(slot, success =>
                {
                    if (success)
                    {
                        SetPauseMenuOpen(false);
                        return;
                    }

                    confirmDialog?.Show("Слот " + slot + " порожній або недоступний.", () => { });
                });
            }

            if (SaveManager.Instance != null && SaveManager.Instance.HasUnsavedChanges)
            {
                confirmDialog?.Show("Незбережений прогрес буде втрачено. Завантажити слот " + slot + "?", DoLoad);
                return;
            }

            DoLoad();
        });
    }

    void OnSettingsClicked()
    {
        HidePauseMenuPanel();
        settingsPanel?.Show();
    }

    void OnQuitClicked()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasUnsavedChanges)
        {
            confirmDialog?.Show("Незбережений прогрес буде втрачено. Вийти з гри?", QuitGame);
            return;
        }

        QuitGame();
    }

    static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
