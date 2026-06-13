using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameSettingsPanel : MonoBehaviour
{
    static readonly Color Gold = new Color(0.95f, 0.82f, 0.42f, 1f);
    static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f, 1f);
    static readonly Color PanelBg = new Color(0.1f, 0.07f, 0.04f, 1f);
    static readonly Color BtnBg = new Color(0.12f, 0.08f, 0.05f, 0.92f);
    static readonly Color BtnBorder = new Color(0.85f, 0.68f, 0.25f, 0.85f);

    public Action OnClosed;

    GameObject root;
    RectTransform panelRect;
    List<Resolution> resolutions;
    int resolutionIndex;
    TextMeshProUGUI resolutionButtonText;
    TextMeshProUGUI windowModeButtonText;
    TextMeshProUGUI vsyncButtonText;
    TextMeshProUGUI cloudSaveButtonText;
    TextMeshProUGUI cloudStatusText;
    Slider volumeSlider;

    public bool IsOpen => root != null && root.activeSelf;

    public void Build(Transform parent)
    {
        root = new GameObject("InGameSettings", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.02f, 0.78f);
        dim.raycastTarget = true;

        Transform content = BuildPanel(root.transform, 460f);
        AddText(content, "Налаштування", 24, true, Gold);
        AddText(content, "Роздільна здатність", 13, false, Gold);
        resolutionButtonText = AddButton(content, "1920 x 1080", CycleResolution);
        AddText(content, "Режим вікна", 13, false, Gold);
        windowModeButtonText = AddButton(content, "Windowed", CycleWindowMode);
        AddText(content, "VSync", 13, false, Gold);
        vsyncButtonText = AddButton(content, "Увімкнено", ToggleVSync);
        AddText(content, "Гучність", 13, false, Gold);
        BuildVolumeSlider(content);
        AddControlHints(content);
        AddText(content, "Хмарне збереження (MySQL)", 13, false, Gold);
        cloudSaveButtonText = AddButton(content, "Вимкнено", ToggleCloudSave);
        cloudStatusText = AddText(content, "API: " + DatabaseSettings.ApiBaseUrl, 12, false, Cream);
        AddButton(content, "Перевірити підключення", TestCloudConnection);
        AddButton(content, "Назад", Hide);

        RefreshLabels();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        root.SetActive(false);
    }

    Transform BuildPanel(Transform parent, float width)
    {
        GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(width, 0f);

        Image border = panel.AddComponent<Image>();
        border.sprite = GameUI.GetSharedWhiteSprite();
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
        bg.sprite = GameUI.GetSharedWhiteSprite();
        bg.color = PanelBg;

        LayoutElement innerLayout = inner.AddComponent<LayoutElement>();
        innerLayout.minWidth = width - 4f;
        innerLayout.preferredWidth = width - 4f;
        innerLayout.flexibleWidth = 1f;

        VerticalLayoutGroup innerLayoutGroup = inner.AddComponent<VerticalLayoutGroup>();
        innerLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        innerLayoutGroup.spacing = 0;
        innerLayoutGroup.childControlWidth = true;
        innerLayoutGroup.childControlHeight = true;
        innerLayoutGroup.childForceExpandWidth = true;
        innerLayoutGroup.childForceExpandHeight = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(inner.transform, false);

        LayoutElement contentLayoutElement = content.AddComponent<LayoutElement>();
        contentLayoutElement.flexibleWidth = 1f;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return content.transform;
    }

    TextMeshProUGUI AddText(Transform parent, string text, float size, bool title, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = size + (title ? 12f : 8f);
        le.preferredHeight = size + (title ? 12f : 8f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        GameUI.ApplySharedFont(tmp, title);
        return tmp;
    }

    TextMeshProUGUI AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnRoot = new GameObject(label + "Button", typeof(RectTransform));
        btnRoot.transform.SetParent(parent, false);
        LayoutElement le = btnRoot.AddComponent<LayoutElement>();
        le.minHeight = 42f;
        le.preferredHeight = 42f;

        Image border = btnRoot.AddComponent<Image>();
        border.sprite = GameUI.GetSharedWhiteSprite();
        border.color = BtnBorder;

        GameObject inner = new GameObject("Inner", typeof(RectTransform));
        inner.transform.SetParent(btnRoot.transform, false);
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(1.5f, 1.5f);
        innerRect.offsetMax = new Vector2(-1.5f, -1.5f);

        Image bg = inner.AddComponent<Image>();
        bg.sprite = GameUI.GetSharedWhiteSprite();
        bg.color = BtnBg;

        Button button = btnRoot.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inner.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
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
        GameUI.ApplySharedFont(tmp);
        return tmp;
    }

    void BuildVolumeSlider(Transform parent)
    {
        GameObject row = new GameObject("VolumeRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 52f;
        rowLe.preferredHeight = 52f;

        GameObject sliderObj = new GameObject("VolumeSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(4f, 8f);
        sliderRt.offsetMax = new Vector2(-4f, -8f);

        GameObject trackObj = new GameObject("Track", typeof(RectTransform));
        trackObj.transform.SetParent(sliderObj.transform, false);
        RectTransform trackRt = trackObj.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0f, 0.5f);
        trackRt.anchorMax = new Vector2(1f, 0.5f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.sizeDelta = new Vector2(0f, 10f);
        Image track = trackObj.AddComponent<Image>();
        track.sprite = GameUI.GetSharedWhiteSprite();
        track.color = new Color(0.1f, 0.07f, 0.04f, 0.95f);
        track.raycastTarget = true;

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRt.sizeDelta = new Vector2(-8f, 10f);
        fillAreaRt.anchoredPosition = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fill = fillObj.AddComponent<Image>();
        fill.sprite = GameUI.GetSharedWhiteSprite();
        fill.color = Gold;

        GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform));
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18f, 18f);
        Image handle = handleObj.AddComponent<Image>();
        handle.sprite = GameUI.GetSharedWhiteSprite();
        handle.color = Cream;
        handle.raycastTarget = true;

        volumeSlider = sliderObj.AddComponent<Slider>();
        volumeSlider.fillRect = fillRt;
        volumeSlider.handleRect = handleRt;
        volumeSlider.targetGraphic = handle;
        volumeSlider.direction = Slider.Direction.LeftToRight;
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = PlayerPrefs.GetFloat(GameSettings.MasterVolumeKey, 0.8f);
        volumeSlider.onValueChanged.AddListener(v => GameSettings.SetMasterVolume(v));
    }

    void AddControlHints(Transform parent)
    {
        TextMeshProUGUI header = AddText(parent, ControlHints.SectionTitleUk, ControlHints.HeaderFontSize, true, Gold);
        ControlHints.StyleHeader(header);
        LayoutElement headerLe = header.GetComponent<LayoutElement>();
        if (headerLe != null)
            headerLe.preferredHeight = ControlHints.HeaderRowHeight;

        foreach (string line in ControlHints.Lines)
        {
            TextMeshProUGUI hint = AddText(parent, line, ControlHints.LineFontSize, false, Cream);
            ControlHints.StyleLine(hint);
            LayoutElement lineLe = hint.GetComponent<LayoutElement>();
            if (lineLe != null)
            {
                lineLe.minHeight = ControlHints.LineRowHeight;
                lineLe.preferredHeight = ControlHints.LineRowHeight;
            }
        }
    }

    public void Show()
    {
        RefreshLabels();
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (panelRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        OnClosed?.Invoke();
    }

    void CycleResolution()
    {
        if (resolutions == null || resolutions.Count == 0)
            resolutions = GameSettings.GetUniqueResolutions();

        resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
        Resolution r = resolutions[resolutionIndex];
        GameSettings.SetResolution(r.width, r.height);
        RefreshLabels();
    }

    void CycleWindowMode()
    {
        GameSettings.CycleWindowMode();
        RefreshLabels();
    }

    void ToggleVSync()
    {
        GameSettings.SetVSync(!GameSettings.GetVSync());
        RefreshLabels();
    }

    void ToggleCloudSave()
    {
        DatabaseSettings.CloudSaveEnabled = !DatabaseSettings.CloudSaveEnabled;
        RefreshLabels();
    }

    void TestCloudConnection()
    {
        CloudSaveClient client = CloudSaveClient.Instance;
        if (client == null)
        {
            if (cloudStatusText != null)
                cloudStatusText.text = "CloudSaveClient не знайдено";
            return;
        }

        if (cloudStatusText != null)
            cloudStatusText.text = "Перевірка...";

        client.CheckHealth((ok, message) =>
        {
            if (cloudStatusText != null)
                cloudStatusText.text = ok ? message : "Помилка: " + message;
        });
    }

    void RefreshLabels()
    {
        if (resolutions == null || resolutions.Count == 0)
            resolutions = GameSettings.GetUniqueResolutions();

        resolutionIndex = GameSettings.FindResolutionIndex(resolutions);
        Resolution r = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];

        if (resolutionButtonText != null)
            resolutionButtonText.text = r.width + " x " + r.height;

        if (windowModeButtonText != null)
            windowModeButtonText.text = GameSettings.GetWindowModeLabel(GameSettings.GetWindowMode());

        if (vsyncButtonText != null)
            vsyncButtonText.text = GameSettings.GetVSync() ? "Увімкнено" : "Вимкнено";

        if (cloudSaveButtonText != null)
            cloudSaveButtonText.text = DatabaseSettings.CloudSaveEnabled ? "Увімкнено" : "Вимкнено";

        if (cloudStatusText != null)
            cloudStatusText.text = "API: " + DatabaseSettings.ApiBaseUrl + " · ID: " + DatabaseSettings.PlayerId.Substring(0, Mathf.Min(8, DatabaseSettings.PlayerId.Length)) + "...";

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(GameSettings.MasterVolumeKey, 0.8f));
    }
}
