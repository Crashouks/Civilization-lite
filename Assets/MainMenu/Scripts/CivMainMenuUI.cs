using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CivMainMenuUI : MonoBehaviour
{
    public string gameTitle = "RISE OF EMPIRES";

    private MainMenu controller;
    private GameObject menuRoot;
    private GameObject mainPanel;
    private GameObject civPanel;
    private GameObject settingsPanel;
    private GameObject loadPanel;
    private readonly MenuButtonAnimator[] loadSlotButtons = new MenuButtonAnimator[SaveManager.SlotCount];
    private readonly bool[] loadSlotExists = new bool[SaveManager.SlotCount];
    private readonly SaveSlotInfo[] loadSlotCloudInfo = new SaveSlotInfo[SaveManager.SlotCount];
    private bool loadSlotCloudPending;
    private MenuButtonAnimator loadButtonAnimator;
    private TextMeshProUGUI loadButtonLabel;
    private List<Resolution> settingsResolutions;
    private int resolutionIndex;
    private TextMeshProUGUI resolutionButtonLabel;
    private TextMeshProUGUI windowModeButtonLabel;
    private TextMeshProUGUI vsyncButtonLabel;
    private Image vsyncButtonBg;
    private bool vsyncEnabled;
    private TMP_FontAsset titleFont;
    private TMP_FontAsset bodyFont;
    private TMP_FontAsset settingsFont;

    private static readonly Color Gold = new Color(0.95f, 0.82f, 0.42f, 1f);
    private static readonly Color GoldDim = new Color(0.85f, 0.72f, 0.38f, 0.9f);
    private static readonly Color TitleFill = new Color(1f, 0.97f, 0.9f, 1f);
    private static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f, 1f);
    private static readonly Color PanelBg = new Color(0.1f, 0.07f, 0.04f, 0.94f);
    private static readonly Color BtnBg = new Color(0.12f, 0.08f, 0.05f, 0.72f);
    private static readonly Color BtnBorder = new Color(0.85f, 0.68f, 0.25f, 0.55f);

    const float ButtonHeight = 58f;
    const float ButtonWidth = 340f;
    const float TitleWidth = 560f;
    const float TitleHeight = 76f;
    const float TitleFontSize = 54f;
    const float ButtonFontSize = 20f;
    const float ModalButtonHeight = 50f;
    const int ButtonCornerRadius = 10;
    const int PanelCornerRadius = 14;

    public void Build(MainMenu menuController, Canvas canvas)
    {
        controller = menuController;
        FindFonts();

        RectTransform rootRt = gameObject.AddComponent<RectTransform>();
        StretchFull(rootRt);

        BuildReadabilityGradient(transform);
        BuildMenuContent(transform);
        BuildCivSelectionPanel(transform);
        BuildLoadPanel(transform);
        BuildSettingsPanel(transform);
        BuildVersionLabel(transform);

        civPanel.SetActive(false);
        settingsPanel.SetActive(false);
        loadPanel.SetActive(false);

        RefreshLoadButton();
        if (SaveManager.IsCloudConfigured())
            StartCoroutine(RefreshLoadButtonFromCloud());
    }

    void FindFonts()
    {
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset f in allFonts)
        {
            if (f == null) continue;

            if (f.name == "Holos-Bold SDF")
            {
                titleFont = f;
                bodyFont = f;
                continue;
            }

            if (titleFont == null && f.name.Contains("Holos-Bold") && !f.name.Contains("Outline"))
                titleFont = f;

            if (bodyFont == null && f.name.Contains("Holos-Bold") && !f.name.Contains("Outline"))
                bodyFont = f;
        }

        if (bodyFont == null)
            bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (titleFont == null) titleFont = bodyFont;

        settingsFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (settingsFont == null) settingsFont = bodyFont;
    }

    void BuildReadabilityGradient(Transform parent)
    {
        GameObject grad = CreateUIObject("ReadabilityGradient", parent);
        StretchFull(grad.GetComponent<RectTransform>());
        Image img = AddImage(grad, Color.white);
        img.sprite = GetLeftFadeSprite();
        img.color = new Color(0.04f, 0.03f, 0.02f, 1f);
        img.raycastTarget = false;
    }

    void BuildMenuContent(Transform parent)
    {
        menuRoot = CreateUIObject("MenuContent", parent);
        RectTransform menuRt = menuRoot.GetComponent<RectTransform>();
        menuRt.anchorMin = Vector2.zero;
        menuRt.anchorMax = new Vector2(0.52f, 1f);
        menuRt.offsetMin = Vector2.zero;
        menuRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup menuLayout = menuRoot.AddComponent<VerticalLayoutGroup>();
        menuLayout.padding = new RectOffset(64, 32, 80, 64);
        menuLayout.spacing = 0f;
        menuLayout.childAlignment = TextAnchor.UpperLeft;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = true;
        menuLayout.childForceExpandWidth = false;
        menuLayout.childForceExpandHeight = false;

        BuildTitle(menuRoot.transform);
        BuildMainButtons(menuRoot.transform);
    }

    void BuildTitle(Transform parent)
    {
        GameObject titleRoot = CreateUIObject("TitleGroup", parent);
        LayoutElement titleLe = titleRoot.AddComponent<LayoutElement>();
        titleLe.preferredWidth = TitleWidth;
        titleLe.preferredHeight = TitleHeight;

        TextMeshProUGUI title = AddText(titleRoot, gameTitle, TitleFontSize, TitleFill, FontStyles.Bold, titleFont);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0, 1);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(0, TitleHeight);
        title.alignment = TextAlignmentOptions.TopLeft;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Overflow;
        title.characterSpacing = 1f;
        title.lineSpacing = 0f;
        ApplyTitleStyle(title);

        Shadow shadow = titleRoot.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    void ApplyTitleStyle(TextMeshProUGUI title)
    {
        title.fontStyle = FontStyles.Bold;
        title.outlineWidth = 0.28f;
        title.outlineColor = new Color32(18, 10, 2, 255);
    }

    void BuildMainButtons(Transform parent)
    {
        mainPanel = CreateUIObject("MainButtons", parent);
        LayoutElement panelLe = mainPanel.AddComponent<LayoutElement>();
        panelLe.preferredWidth = TitleWidth;

        VerticalLayoutGroup layout = mainPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        int buttonInset = Mathf.RoundToInt((TitleWidth - ButtonWidth) * 0.5f);
        layout.padding = new RectOffset(buttonInset, buttonInset, 88, 0);

        CreateMenuButton(mainPanel.transform, "NEW GAME", controller.OpenCivSelection);
        loadButtonAnimator = CreateMenuButton(mainPanel.transform, "LOAD GAME", controller.OpenLoadPanel);
        loadButtonLabel = loadButtonAnimator.GetComponentInChildren<TextMeshProUGUI>();
        CreateMenuButton(mainPanel.transform, "SETTINGS", controller.OpenSettings);
        CreateMenuButton(mainPanel.transform, "QUIT", controller.ExitGame);
    }

    void BuildCivSelectionPanel(Transform parent)
    {
        civPanel = CreateUIObject("CivPanel", parent);
        SetupModalPanel(civPanel, new Vector2(520, 520), true);
        civPanel.transform.SetAsLastSibling();

        GameObject content = CreateUIObject("Content", civPanel.transform);
        StretchFull(content.GetComponent<RectTransform>());
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLayoutHeader(content.transform, "CHOOSE CIVILIZATION", 28, settingsFont);

        string[] civs = { "Rome", "America", "Egypt", "Scythia" };
        foreach (string civ in civs)
        {
            string c = civ;
            CreateModalButton(content.transform, civ.ToUpper(), () => controller.SelectCivilization(c));
        }

        CreateModalButton(content.transform, "BACK", controller.BackToMenu);
    }

    void BuildLoadPanel(Transform parent)
    {
        loadPanel = CreateUIObject("LoadPanel", parent);
        SetupModalPanel(loadPanel, new Vector2(460, 360), true);
        loadPanel.transform.SetAsLastSibling();

        GameObject content = CreateUIObject("Content", loadPanel.transform);
        StretchFull(content.GetComponent<RectTransform>());
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLayoutHeader(content.transform, "LOAD GAME", 24, settingsFont);

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            loadSlotButtons[i] = CreateModalButton(content.transform, "Slot " + slot, () => controller.LoadGameFromSlot(slot));
        }

        CreateModalButton(content.transform, "BACK", ShowMainPanel);
    }

    public void ShowLoadPanel()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        SetPanelActive(civPanel, false);
        SetPanelActive(settingsPanel, false);
        RefreshLoadSlotLabels();
        SetPanelActive(loadPanel, true, false);
    }

    void RefreshLoadSlotLabels()
    {
        for (int i = 0; i < SaveManager.SlotCount; i++)
            loadSlotExists[i] = SaveManager.GetLocalSlotInfo(i + 1).exists;

        loadSlotCloudPending = SaveManager.IsCloudConfigured();
        ApplyLoadSlotButtonLabels();

        if (!loadSlotCloudPending)
            return;

        StartCoroutine(RefreshLoadSlotLabelsFromCloud());
    }

    void ApplyLoadSlotButtonLabels()
    {
        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            if (loadSlotButtons[i] == null) continue;
            SaveSlotInfo info = SaveManager.GetLocalSlotInfo(i + 1);
            if (loadSlotExists[i])
            {
                SaveSlotInfo cloud = loadSlotCloudInfo[i];
                if (!info.exists && cloud.exists)
                    info = cloud;
                else if (info.exists && cloud.exists && cloud.turnNumber >= info.turnNumber)
                    info = cloud;
            }

            TextMeshProUGUI label = loadSlotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string line = loadSlotExists[i] ? info.GetDisplayLine() : "Empty";
                label.text = "Slot " + (i + 1) + ": " + line;
                label.color = loadSlotExists[i] ? Cream : GoldDim;
            }
            loadSlotButtons[i].SetInteractable(GetLoadSlotInteractable(i));
        }
    }

    bool GetLoadSlotInteractable(int index)
    {
        if (!loadSlotExists[index])
            return false;

        if (loadSlotCloudPending && !SaveManager.HasSave(index + 1))
            return false;

        return true;
    }

    IEnumerator RefreshLoadSlotLabelsFromCloud()
    {
        SaveSlotInfo[] cloudSlots = null;
        yield return CloudSaveClient.FetchSlotSummariesCoroutine(r => cloudSlots = r);

        if (cloudSlots == null)
            yield break;

        for (int i = 0; i < SaveManager.SlotCount && i < cloudSlots.Length; i++)
        {
            loadSlotCloudInfo[i] = cloudSlots[i];
            if (cloudSlots[i].exists)
                loadSlotExists[i] = true;
        }

        if (loadPanel != null && loadPanel.activeSelf)
        {
            loadSlotCloudPending = false;
            ApplyLoadSlotButtonLabels();
        }
    }

    IEnumerator RefreshLoadButtonFromCloud()
    {
        SaveSlotInfo[] cloudSlots = null;
        yield return CloudSaveClient.FetchSlotSummariesCoroutine(r => cloudSlots = r);

        bool anySave = SaveManager.HasSave();
        if (cloudSlots != null)
        {
            foreach (SaveSlotInfo info in cloudSlots)
            {
                if (info.exists)
                {
                    anySave = true;
                    break;
                }
            }
        }

        ApplyLoadButtonState(anySave, cloudSlots);
    }

    void ApplyLoadButtonState(bool anySave, SaveSlotInfo[] cloudSlots = null)
    {
        if (loadButtonAnimator == null) return;
        loadButtonAnimator.SetInteractable(anySave);

        if (loadButtonLabel == null) return;

        SaveSlotInfo best = default;
        best.exists = false;
        best.turnNumber = 0;

        for (int i = 1; i <= SaveManager.SlotCount; i++)
        {
            SaveSlotInfo local = SaveManager.GetLocalSlotInfo(i);
            SaveSlotInfo info = local;
            if (cloudSlots != null && i - 1 < cloudSlots.Length && cloudSlots[i - 1].exists)
            {
                SaveSlotInfo cloud = cloudSlots[i - 1];
                if (!info.exists || cloud.turnNumber >= info.turnNumber)
                    info = cloud;
            }

            if (info.exists && info.turnNumber >= best.turnNumber)
                best = info;
        }

        loadButtonLabel.text = best.exists
            ? $"LOAD GAME  ·  Turn {Mathf.Max(1, best.turnNumber)}"
            : "LOAD GAME";
    }

    MenuButtonAnimator CreateModalButton(Transform parent, string label, Action onClick)
    {
        return CreateMenuButton(parent, label, onClick, true, true, ModalButtonHeight);
    }

    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = CreateUIObject("SettingsPanel", parent);
        SetupModalPanel(settingsPanel, new Vector2(520, 560), true);
        settingsPanel.transform.SetAsLastSibling();

        GameObject content = CreateUIObject("Content", settingsPanel.transform);
        StretchFull(content.GetComponent<RectTransform>());
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLayoutHeader(content.transform, "SETTINGS", 32, settingsFont);
        BuildSettingRowLabel(content.transform, "RESOLUTION");
        resolutionButtonLabel = BuildSettingCycleButton(content.transform, "1920 x 1080", CycleResolution);
        BuildSettingRowLabel(content.transform, "WINDOW MODE");
        windowModeButtonLabel = BuildSettingCycleButton(content.transform, "Windowed", CycleWindowMode);
        BuildSettingRowLabel(content.transform, "VSYNC");
        BuildVSyncToggle(content.transform);
        BuildSettingRowLabel(content.transform, "MASTER VOLUME");
        BuildVolumeSlider(content.transform);
        CreateMenuButton(content.transform, "BACK", controller.BackToMenu, true, true, ModalButtonHeight);
        RefreshSettingsVisuals();
    }

    void BuildSettingRowLabel(Transform parent, string text)
    {
        GameObject row = CreateUIObject("SettingLabel_" + text, parent);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 22f;
        TextMeshProUGUI tmp = AddText(row, text, 14, GoldDim, FontStyles.Bold, settingsFont);
        StretchFull(tmp.rectTransform);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    TextMeshProUGUI BuildSettingCycleButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject row = CreateUIObject("SettingCycleButton", parent);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 46f;

        GameObject border = CreateUIObject("Border", row.transform);
        StretchFull(border.GetComponent<RectTransform>());
        AddRoundedImage(border, BtnBorder).raycastTarget = false;

        GameObject clickArea = CreateUIObject("ClickArea", row.transform);
        RectTransform clickRt = clickArea.GetComponent<RectTransform>();
        StretchFull(clickRt);
        clickRt.offsetMin = new Vector2(1.5f, 1.5f);
        clickRt.offsetMax = new Vector2(-1.5f, -1.5f);
        Image bg = AddRoundedImage(clickArea, BtnBg);
        bg.raycastTarget = true;

        GameObject labelObj = CreateUIObject("Label", clickArea.transform);
        StretchFull(labelObj.GetComponent<RectTransform>());
        TextMeshProUGUI caption = AddText(labelObj, label, 16, Cream, FontStyles.Bold, settingsFont);
        caption.alignment = TextAlignmentOptions.Center;
        caption.raycastTarget = false;

        Button button = clickArea.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        return caption;
    }

    void CycleResolution()
    {
        if (settingsResolutions == null || settingsResolutions.Count == 0)
            settingsResolutions = GameSettings.GetUniqueResolutions();

        resolutionIndex = (resolutionIndex + 1) % settingsResolutions.Count;
        Resolution r = settingsResolutions[resolutionIndex];
        GameSettings.SetResolution(r.width, r.height);
        RefreshSettingsVisuals();
    }

    void CycleWindowMode()
    {
        GameSettings.CycleWindowMode();
        RefreshSettingsVisuals();
    }

    void RefreshSettingsVisuals()
    {
        if (settingsResolutions == null || settingsResolutions.Count == 0)
            settingsResolutions = GameSettings.GetUniqueResolutions();

        resolutionIndex = GameSettings.FindResolutionIndex(settingsResolutions);
        Resolution r = settingsResolutions[Mathf.Clamp(resolutionIndex, 0, settingsResolutions.Count - 1)];

        if (resolutionButtonLabel != null)
            resolutionButtonLabel.text = r.width + " x " + r.height;

        if (windowModeButtonLabel != null)
            windowModeButtonLabel.text = GameSettings.GetWindowModeLabel(GameSettings.GetWindowMode());

        RefreshVSyncVisual();
    }

    void BuildVSyncToggle(Transform parent)
    {
        GameObject row = CreateUIObject("VSyncToggleRow", parent);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 46f;

        GameObject btnObj = CreateUIObject("VSyncToggle", row.transform);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1, 0.5f);
        btnRt.anchorMax = new Vector2(1, 0.5f);
        btnRt.pivot = new Vector2(1, 0.5f);
        btnRt.sizeDelta = new Vector2(120, 40);
        btnRt.anchoredPosition = Vector2.zero;

        vsyncButtonBg = AddRoundedImage(btnObj, BtnBg);
        StretchFull(vsyncButtonBg.rectTransform);
        vsyncButtonBg.raycastTarget = true;

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(BtnBorder.r, BtnBorder.g, BtnBorder.b, 0.35f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        GameObject textObj = CreateUIObject("Label", btnObj.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        vsyncButtonLabel = AddText(textObj, "ON", 16, Cream, FontStyles.Bold, settingsFont);
        vsyncButtonLabel.alignment = TextAlignmentOptions.Center;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = vsyncButtonBg;
        btn.onClick.AddListener(ToggleVSync);

        vsyncEnabled = GameSettings.GetVSync();
        RefreshVSyncVisual();
    }

    void ToggleVSync()
    {
        vsyncEnabled = !vsyncEnabled;
        GameSettings.SetVSync(vsyncEnabled);
        RefreshVSyncVisual();
    }

    void RefreshVSyncVisual()
    {
        if (vsyncButtonLabel == null || vsyncButtonBg == null) return;
        vsyncButtonLabel.text = vsyncEnabled ? "ON" : "OFF";
        vsyncButtonLabel.color = vsyncEnabled ? Gold : Cream;
        vsyncButtonBg.color = vsyncEnabled
            ? new Color(0.2f, 0.14f, 0.06f, 0.92f)
            : BtnBg;
    }

    void AddLayoutHeader(Transform parent, string text, float fontSize, TMP_FontAsset font = null)
    {
        GameObject header = CreateUIObject("Header", parent);
        LayoutElement le = header.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 16f;
        TextMeshProUGUI tmp = AddText(header, text, fontSize, Gold, FontStyles.Bold, font ?? titleFont);
        StretchFull(tmp.rectTransform);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    void BuildVolumeSlider(Transform parent)
    {
        GameObject row = CreateUIObject("VolumeRow", parent);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 52f;

        GameObject sliderObj = CreateUIObject("VolumeSlider", row.transform);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        StretchFull(sliderRt);
        sliderRt.offsetMin = new Vector2(4, 8);
        sliderRt.offsetMax = new Vector2(-4, -8);

        GameObject trackObj = CreateUIObject("Track", sliderObj.transform);
        RectTransform trackRt = trackObj.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0, 0.5f);
        trackRt.anchorMax = new Vector2(1, 0.5f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.sizeDelta = new Vector2(0, 10);
        Image track = AddRoundedImage(trackObj, new Color(0.1f, 0.07f, 0.04f, 0.95f));
        track.raycastTarget = true;

        GameObject fillArea = CreateUIObject("FillArea", sliderObj.transform);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.5f);
        faRt.anchorMax = new Vector2(1, 0.5f);
        faRt.pivot = new Vector2(0.5f, 0.5f);
        faRt.sizeDelta = new Vector2(-8, 10);
        faRt.anchoredPosition = Vector2.zero;

        GameObject fillObj = CreateUIObject("Fill", fillArea.transform);
        StretchFull(fillObj.GetComponent<RectTransform>());
        Image fill = AddRoundedImage(fillObj, Gold);

        GameObject handleArea = CreateUIObject("HandleArea", sliderObj.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handleObj = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18, 18);
        Image handle = AddCircleImage(handleObj, Cream);
        handle.raycastTarget = true;
        Outline handleOutline = handleObj.AddComponent<Outline>();
        handleOutline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.65f);
        handleOutline.effectDistance = new Vector2(1f, -1f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat(GameSettings.MasterVolumeKey, 0.8f);
        slider.onValueChanged.AddListener(v => GameSettings.SetMasterVolume(v));
    }

    void BuildVersionLabel(Transform parent)
    {
        GameObject ver = CreateUIObject("Version", parent);
        RectTransform rt = ver.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-24, 16);
        rt.sizeDelta = new Vector2(240, 24);
        TextMeshProUGUI t = AddText(ver, "Civilization Lite", 12, Cream, FontStyles.Italic, bodyFont);
        StretchFull(t.rectTransform);
        t.alignment = TextAlignmentOptions.BottomRight;
        t.alpha = 0.45f;
        t.raycastTarget = false;
    }

    MenuButtonAnimator CreateMenuButton(Transform parent, string label, Action onClick,
        bool centerText = false, bool stretchWidth = false, float height = -1f)
    {
        float btnHeight = height > 0f ? height : ButtonHeight;

        GameObject btnObj = CreateUIObject("Btn_" + label.Replace(" ", ""), parent);
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = btnHeight;
        le.minHeight = btnHeight;

        if (stretchWidth)
        {
            le.preferredWidth = -1;
            le.minWidth = 0;
            le.flexibleWidth = 1f;
        }
        else
        {
            le.preferredWidth = ButtonWidth;
            le.minWidth = ButtonWidth;
        }

        Image border = AddRoundedImage(btnObj, BtnBorder);
        StretchFull(border.rectTransform);
        border.raycastTarget = false;

        GameObject inner = CreateUIObject("Inner", btnObj.transform);
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(1.5f, 1.5f);
        innerRt.offsetMax = new Vector2(-1.5f, -1.5f);

        Image bg = AddRoundedImage(inner, BtnBg);
        bg.raycastTarget = true;

        GameObject textObj = CreateUIObject("Label", inner.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12, 0);
        textRt.offsetMax = new Vector2(-12, 0);

        TMP_FontAsset font = stretchWidth ? settingsFont : bodyFont;
        TextMeshProUGUI tmp = AddText(textObj, label, stretchWidth ? 18 : ButtonFontSize, Cream, FontStyles.Bold, font);
        tmp.alignment = centerText ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle = FontStyles.Bold;
        if (!stretchWidth && !centerText)
        {
            textRt.offsetMin = new Vector2(22, 0);
            textRt.offsetMax = new Vector2(-14, 0);
            tmp.outlineWidth = 0.12f;
            tmp.outlineColor = new Color32(20, 12, 4, 140);
        }
        tmp.raycastTarget = false;

        Button button = inner.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick?.Invoke());

        MenuButtonAnimator anim = inner.AddComponent<MenuButtonAnimator>();
        anim.normalBg = BtnBg;
        anim.hoverBg = new Color(0.2f, 0.13f, 0.06f, 0.9f);
        anim.hoverOffsetX = centerText ? 0f : 4f;
        anim.Setup(bg, null, tmp);
        anim.ShowInstant();
        return anim;
    }

    void SetupModalPanel(GameObject panel, Vector2 size, bool rounded = false)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Image panelBg = rounded
            ? AddRoundedImage(panel, PanelBg, PanelCornerRadius)
            : AddImage(panel, PanelBg);
        panelBg.raycastTarget = true;

        Outline o = panel.AddComponent<Outline>();
        o.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.45f);
        o.effectDistance = new Vector2(2, -2);

        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        panel.AddComponent<MenuPanelAnimator>();
    }

    public void ShowMainPanel()
    {
        SetPanelActive(civPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(loadPanel, false);
        if (menuRoot != null) menuRoot.SetActive(true);
        RefreshLoadButton();
    }

    public void ShowCivPanel()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(loadPanel, false);
        SetPanelActive(civPanel, true, false);
    }

    public void ShowSettingsPanel()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        SetPanelActive(civPanel, false);
        SetPanelActive(loadPanel, false);
        SetPanelActive(settingsPanel, true, false);
        RefreshSettingsVisuals();
    }

    void SetPanelActive(GameObject panel, bool active, bool instant = false)
    {
        if (panel == null) return;
        if (!active) { panel.SetActive(false); return; }
        panel.SetActive(true);
        panel.GetComponent<MenuPanelAnimator>()?.Show(instant);
    }

    public void RefreshLoadButton()
    {
        ApplyLoadButtonState(SaveManager.HasSave());
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    Image AddImage(GameObject obj, Color color)
    {
        Image img = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Image AddRoundedImage(GameObject obj, Color color, int cornerRadius = -1)
    {
        if (cornerRadius < 0) cornerRadius = ButtonCornerRadius;
        Image img = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
        img.sprite = GetRoundedSprite(cornerRadius);
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Image AddCircleImage(GameObject obj, Color color)
    {
        Image img = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
        img.sprite = GetCircleSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI AddText(GameObject obj, string text, float size, Color color, FontStyles style, TMP_FontAsset font)
    {
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    static Sprite whiteSprite;
    static Sprite leftFadeSprite;
    static Sprite circleSprite;
    static readonly Dictionary<int, Sprite> roundedSprites = new Dictionary<int, Sprite>();

    static Sprite GetRoundedSprite(int cornerRadius = -1)
    {
        if (cornerRadius < 0) cornerRadius = ButtonCornerRadius;
        if (roundedSprites.TryGetValue(cornerRadius, out Sprite cached))
            return cached;

        const int size = 64;
        int radius = cornerRadius;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float qx = Mathf.Abs(px) - half + radius;
                float qy = Mathf.Abs(py) - half + radius;
                Vector2 q = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f));
                float dist = Mathf.Min(Mathf.Max(qx, qy), 0f) + q.magnitude - radius;
                float alpha = dist <= -0.5f ? 1f : dist >= 0.5f ? 0f : 0.5f - dist;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
        roundedSprites[cornerRadius] = sprite;
        return sprite;
    }

    static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f - 0.5f;
        float center = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = dist <= radius - 0.5f ? 1f : dist >= radius + 0.5f ? 0f : radius + 0.5f - dist;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    static Sprite GetLeftFadeSprite()
    {
        if (leftFadeSprite != null) return leftFadeSprite;
        const int w = 128;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
        {
            float t = (float)x / (w - 1);
            float a = 1f - t * t;
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        leftFadeSprite = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0f, 0.5f), 1f);
        return leftFadeSprite;
    }
}
