using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CivMainMenuUI : MonoBehaviour
{
    public string gameTitle = "RISE OF EMPIRES";

    private MainMenu controller;
    private GameObject mainPanel;
    private GameObject civPanel;
    private GameObject settingsPanel;
    private MenuButtonAnimator loadButtonAnimator;
    private TMP_FontAsset titleFont;
    private TMP_FontAsset bodyFont;

    private static readonly Color Gold = new Color(0.92f, 0.78f, 0.35f, 1f);
    private static readonly Color GoldDim = new Color(0.72f, 0.58f, 0.22f, 1f);
    private static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f, 1f);
    private static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.1f, 0.94f);
    private static readonly Color BtnBg = new Color(0.07f, 0.1f, 0.16f, 0.88f);
    private static readonly Color BtnBorder = new Color(0.55f, 0.45f, 0.18f, 0.9f);

    public void Build(MainMenu menuController, Canvas canvas)
    {
        controller = menuController;
        FindFonts();

        RectTransform rootRt = gameObject.AddComponent<RectTransform>();
        Stretch(rootRt);

        BuildOverlays(transform);
        BuildTitle(transform);
        BuildMainButtons(transform);
        BuildCivSelectionPanel(transform);
        BuildSettingsPanel(transform);
        BuildVersionLabel(transform);

        mainPanel.SetActive(true);
        civPanel.SetActive(false);
        settingsPanel.SetActive(false);

        RefreshLoadButton();
        StartCoroutine(PlayIntroAnimations());
    }

    void FindFonts()
    {
        TextMeshProUGUI[] allText = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI t in allText)
        {
            if (t.font == null) continue;
            if (titleFont == null && t.font.name.Contains("Outline"))
                titleFont = t.font;
            if (bodyFont == null && t.font.name.Contains("Holos"))
                bodyFont = t.font;
        }
        if (titleFont == null) titleFont = bodyFont;
        if (bodyFont == null) bodyFont = titleFont;
    }

    void BuildOverlays(Transform parent)
    {
        // Full-screen subtle vignette (keeps your painting visible)
        Image vignette = AddImage(CreateUIObject("Vignette", parent), new Color(0, 0, 0, 0.35f));
        Stretch(vignette.rectTransform);

        // Civ 6 style left sidebar gradient
        GameObject sidebar = CreateUIObject("LeftSidebar", parent);
        RectTransform sbRt = sidebar.GetComponent<RectTransform>();
        sbRt.anchorMin = Vector2.zero;
        sbRt.anchorMax = new Vector2(0.42f, 1f);
        sbRt.offsetMin = Vector2.zero;
        sbRt.offsetMax = Vector2.zero;

        Image sbImg = AddImage(sidebar, new Color(0.02f, 0.04f, 0.08f, 0.72f));

        // Gold accent line on sidebar edge
        GameObject accentLine = CreateUIObject("SidebarAccent", sidebar.transform);
        RectTransform alRt = accentLine.GetComponent<RectTransform>();
        alRt.anchorMin = new Vector2(1, 0);
        alRt.anchorMax = new Vector2(1, 1);
        alRt.pivot = new Vector2(1, 0.5f);
        alRt.sizeDelta = new Vector2(2, 0);
        AddImage(accentLine, Gold);

        // Animated top/bottom fade
        GameObject animRoot = CreateUIObject("BgAnimator", parent);
        Stretch(animRoot.GetComponent<RectTransform>());
        Image topFade = AddImage(CreateUIObject("TopFade", animRoot.transform), new Color(0.02f, 0.05f, 0.1f, 0.45f));
        RectTransform topRt = topFade.rectTransform;
        topRt.anchorMin = new Vector2(0, 0.7f);
        topRt.anchorMax = Vector2.one;
        topRt.offsetMin = Vector2.zero;
        topRt.offsetMax = Vector2.zero;

        Image botFade = AddImage(CreateUIObject("BotFade", animRoot.transform), new Color(0.02f, 0.04f, 0.08f, 0.55f));
        RectTransform botRt = botFade.rectTransform;
        botRt.anchorMin = Vector2.zero;
        botRt.anchorMax = new Vector2(1, 0.25f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;

        GameObject orb = CreateUIObject("Glow", animRoot.transform);
        RectTransform orbRt = orb.GetComponent<RectTransform>();
        orbRt.anchorMin = new Vector2(0.15f, 0.5f);
        orbRt.anchorMax = new Vector2(0.15f, 0.5f);
        orbRt.sizeDelta = new Vector2(500, 500);
        AddImage(orb, new Color(0.2f, 0.35f, 0.5f, 0.08f));

        MenuBackgroundAnimator anim = animRoot.AddComponent<MenuBackgroundAnimator>();
        anim.gradientTop = topFade;
        anim.gradientBottom = botFade;
        anim.vignette = vignette;
        anim.glowOrb = orbRt;

        BuildAmbientParticles(parent);
    }

    void BuildAmbientParticles(Transform parent)
    {
        for (int i = 0; i < 24; i++)
        {
            GameObject p = CreateUIObject("Dust", parent);
            RectTransform rt = p.GetComponent<RectTransform>();
            float x = UnityEngine.Random.Range(0f, 0.4f);
            float y = UnityEngine.Random.Range(0f, 1f);
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = rt.anchorMin;
            rt.sizeDelta = new Vector2(UnityEngine.Random.Range(2f, 4f), UnityEngine.Random.Range(2f, 4f));
            AddImage(p, new Color(1f, 0.9f, 0.6f, UnityEngine.Random.Range(0.05f, 0.2f)));
            MenuAmbientParticle drift = p.AddComponent<MenuAmbientParticle>();
            drift.speed = UnityEngine.Random.Range(6f, 16f);
            drift.amplitude = UnityEngine.Random.Range(10f, 30f);
        }
    }

    void BuildTitle(Transform parent)
    {
        GameObject titleRoot = CreateUIObject("TitleGroup", parent);
        RectTransform rt = titleRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(64, -48);
        rt.sizeDelta = new Vector2(700, 160);

        TextMeshProUGUI title = AddText(titleRoot, gameTitle, 62, Gold, FontStyles.Bold, titleFont);
        Stretch(title.rectTransform);
        title.characterSpacing = 6f;
        title.lineSpacing = -8f;
        ApplyTextShadow(title);

        Image line = AddImage(CreateUIObject("TitleLine", titleRoot.transform), Gold);
        RectTransform lineRt = line.rectTransform;
        lineRt.anchorMin = new Vector2(0, 0);
        lineRt.anchorMax = new Vector2(0, 0);
        lineRt.pivot = new Vector2(0, 0);
        lineRt.anchoredPosition = new Vector2(4, 8);
        lineRt.sizeDelta = new Vector2(320, 3);

        CanvasGroup cg = titleRoot.AddComponent<CanvasGroup>();
        StartCoroutine(TitleIntro(cg, rt));
    }

    IEnumerator TitleIntro(CanvasGroup cg, RectTransform rt)
    {
        cg.alpha = 0f;
        Vector2 start = rt.anchoredPosition + new Vector2(-40f, 20f);
        Vector2 end = rt.anchoredPosition;
        rt.anchoredPosition = start;

        float t = 0f;
        while (t < 0.85f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.85f);
            cg.alpha = k;
            rt.anchoredPosition = Vector2.Lerp(start, end, k);
            yield return null;
        }
        cg.alpha = 1f;
        rt.anchoredPosition = end;
    }

    void BuildMainButtons(Transform parent)
    {
        mainPanel = CreateUIObject("MainButtons", parent);
        RectTransform rt = mainPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(64, -40);
        rt.sizeDelta = new Vector2(380, 400);

        float y = 150f;
        float step = -88f;

        CreateMenuButton(mainPanel.transform, "NEW GAME", y, controller.OpenCivSelection);
        y += step;
        loadButtonAnimator = CreateMenuButton(mainPanel.transform, "LOAD GAME", y, controller.LoadGame);
        y += step;
        CreateMenuButton(mainPanel.transform, "SETTINGS", y, controller.OpenSettings);
        y += step;
        CreateMenuButton(mainPanel.transform, "QUIT", y, controller.ExitGame);
    }

    void BuildCivSelectionPanel(Transform parent)
    {
        civPanel = CreateUIObject("CivPanel", parent);
        SetupModalPanel(civPanel, new Vector2(640, 580));

        AddText(civPanel, "CHOOSE CIVILIZATION", 28, Gold, FontStyles.Bold, titleFont,
            new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(560, 44));

        string[] civs = { "Rome", "America", "Egypt", "Scythia" };
        for (int i = 0; i < civs.Length; i++)
        {
            string civ = civs[i];
            CreateMenuButton(civPanel.transform, civ.ToUpper(), -90f - i * 82f, () => controller.SelectCivilization(civ), 560f);
        }
        CreateMenuButton(civPanel.transform, "BACK", -90f - civs.Length * 82f - 16f, controller.BackToMenu, 560f);
    }

    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = CreateUIObject("SettingsPanel", parent);
        SetupModalPanel(settingsPanel, new Vector2(580, 400));

        AddText(settingsPanel, "SETTINGS", 28, Gold, FontStyles.Bold, titleFont,
            new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(480, 44));

        AddText(settingsPanel, "MASTER VOLUME", 18, Cream, FontStyles.Normal, bodyFont,
            new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(400, 32));

        BuildVolumeSlider(settingsPanel.transform);
        CreateMenuButton(settingsPanel.transform, "BACK", -140f, controller.BackToMenu, 500f);
    }

    void BuildVolumeSlider(Transform parent)
    {
        GameObject sliderObj = CreateUIObject("VolumeSlider", parent);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.45f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.45f);
        sliderRt.sizeDelta = new Vector2(460, 36);

        AddImage(CreateUIObject("Bg", sliderObj.transform), new Color(0.08f, 0.12f, 0.18f, 1f));
        Stretch(sliderObj.transform.GetChild(0).GetComponent<RectTransform>());

        GameObject fillArea = CreateUIObject("FillArea", sliderObj.transform);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.2f);
        faRt.anchorMax = new Vector2(1, 0.8f);
        faRt.offsetMin = new Vector2(8, 0);
        faRt.offsetMax = new Vector2(-8, 0);
        Image fill = AddImage(CreateUIObject("Fill", fillArea.transform), Gold);
        Stretch(fill.rectTransform);

        GameObject handleArea = CreateUIObject("HandleArea", sliderObj.transform);
        Stretch(handleArea.GetComponent<RectTransform>());
        Image handle = AddImage(CreateUIObject("Handle", handleArea.transform), Cream);
        handle.rectTransform.sizeDelta = new Vector2(18, 30);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        AudioListener.volume = slider.value;
        slider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat("MasterVolume", v);
            PlayerPrefs.Save();
            AudioListener.volume = v;
        });
    }

    void BuildVersionLabel(Transform parent)
    {
        GameObject ver = CreateUIObject("Version", parent);
        RectTransform rt = ver.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-28, 20);
        rt.sizeDelta = new Vector2(280, 28);
        TextMeshProUGUI t = AddText(ver, "Civilization Lite", 13, Cream, FontStyles.Italic, bodyFont);
        t.alpha = 0.4f;
        t.alignment = TextAlignmentOptions.BottomRight;
        Stretch(t.rectTransform);
    }

    MenuButtonAnimator CreateMenuButton(Transform parent, string label, float y, Action onClick, float width = 360f)
    {
        GameObject btnObj = CreateUIObject("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(width, 68);

        // Border
        Image border = AddImage(btnObj, BtnBorder);
        Stretch(border.rectTransform);

        // Inner background (inset)
        GameObject inner = CreateUIObject("Inner", btnObj.transform);
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2, 2);
        innerRt.offsetMax = new Vector2(-2, -2);
        Image bg = AddImage(inner, BtnBg);

        // Top highlight
        GameObject highlight = CreateUIObject("Highlight", inner.transform);
        RectTransform hlRt = highlight.GetComponent<RectTransform>();
        hlRt.anchorMin = new Vector2(0, 1);
        hlRt.anchorMax = new Vector2(1, 1);
        hlRt.pivot = new Vector2(0.5f, 1);
        hlRt.sizeDelta = new Vector2(0, 1);
        AddImage(highlight, new Color(1f, 1f, 1f, 0.08f));

        // Left gold bar (hidden until hover)
        GameObject accentObj = CreateUIObject("Accent", inner.transform);
        RectTransform accentRt = accentObj.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0, 0);
        accentRt.anchorMax = new Vector2(0, 1);
        accentRt.pivot = new Vector2(0, 0.5f);
        accentRt.sizeDelta = new Vector2(4, -8);
        accentRt.anchoredPosition = new Vector2(6, 0);
        Image accent = AddImage(accentObj, Gold);
        Color ac = accent.color; ac.a = 0f; accent.color = ac;

        // Label
        GameObject textObj = CreateUIObject("Label", inner.transform);
        Stretch(textObj.GetComponent<RectTransform>());
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.offsetMin = new Vector2(22, 0);
        textRt.offsetMax = new Vector2(-16, 0);
        TextMeshProUGUI tmp = AddText(textObj, label, 20, Cream, FontStyles.Bold, bodyFont);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyTextShadow(tmp);

        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick?.Invoke());

        MenuButtonAnimator anim = btnObj.AddComponent<MenuButtonAnimator>();
        anim.normalBg = BtnBg;
        anim.hoverBg = new Color(0.12f, 0.18f, 0.28f, 0.95f);
        anim.hoverOffsetX = 10f;
        anim.Setup(bg, accent, tmp);
        return anim;
    }

    void SetupModalPanel(GameObject panel, Vector2 size)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Image bg = AddImage(panel, PanelBg);
        Stretch(bg.rectTransform);

        Outline o = panel.AddComponent<Outline>();
        o.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
        o.effectDistance = new Vector2(2, -2);

        Shadow s = panel.AddComponent<Shadow>();
        s.effectColor = new Color(0, 0, 0, 0.7f);
        s.effectDistance = new Vector2(4, -4);

        panel.AddComponent<CanvasGroup>();
        panel.AddComponent<MenuPanelAnimator>();
    }

    void ApplyTextShadow(TextMeshProUGUI tmp)
    {
        Shadow shadow = tmp.GetComponent<Shadow>();
        if (shadow == null) shadow = tmp.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    public void ShowMainPanel()
    {
        SetPanelActive(civPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(mainPanel, true, true);
        RefreshLoadButton();
    }

    public void ShowCivPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(civPanel, true, false);
    }

    public void ShowSettingsPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(civPanel, false);
        SetPanelActive(settingsPanel, true, false);
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
        if (loadButtonAnimator == null) return;
        loadButtonAnimator.SetInteractable(controller != null && controller.HasSaveFile());
    }

    IEnumerator PlayIntroAnimations()
    {
        yield return new WaitForSeconds(0.2f);
        foreach (MenuButtonAnimator b in mainPanel.GetComponentsInChildren<MenuButtonAnimator>())
            b.PlayIntro(0.1f * b.transform.GetSiblingIndex());
    }

    // --- helpers ---

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    void Stretch(RectTransform rt)
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
        return img;
    }

    TextMeshProUGUI AddText(GameObject obj, string text, float size, Color color, FontStyles style, TMP_FontAsset font,
        Vector2? anchor = null, Vector2? pos = null, Vector2? sizeDelta = null)
    {
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>() ?? obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        if (font != null) tmp.font = font;
        if (anchor.HasValue)
        {
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = anchor.Value;
            rt.anchorMax = anchor.Value;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos ?? Vector2.zero;
            rt.sizeDelta = sizeDelta ?? new Vector2(400, 40);
        }
        return tmp;
    }

    static Sprite whiteSprite;
    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
