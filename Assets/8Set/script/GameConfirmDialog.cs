using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameConfirmDialog : MonoBehaviour
{
    static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f, 1f);
    static readonly Color BtnBg = new Color(0.12f, 0.08f, 0.05f, 0.92f);
    static readonly Color BtnBorder = new Color(0.85f, 0.68f, 0.25f, 0.85f);

    GameObject root;
    TextMeshProUGUI messageText;
    Action pendingConfirm;

    public bool IsOpen => root != null && root.activeSelf;

    public void Build(Transform canvas)
    {
        root = new GameObject("ConfirmDialog");
        root.transform.SetParent(canvas, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.02f, 0.02f, 0.82f);
        dim.raycastTarget = true;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 0f);

        Image border = panel.AddComponent<Image>();
        border.sprite = GameUI.GetSharedWhiteSprite();
        border.color = BtnBorder;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(panel.transform, false);
        RectTransform innerRect = inner.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        Image bg = inner.AddComponent<Image>();
        bg.sprite = GameUI.GetSharedWhiteSprite();
        bg.color = new Color(0.1f, 0.07f, 0.04f, 1f);

        VerticalLayoutGroup layout = inner.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        ContentSizeFitter innerFitter = inner.AddComponent<ContentSizeFitter>();
        innerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(inner.transform, false);
        LayoutElement msgLe = msgObj.AddComponent<LayoutElement>();
        msgLe.minHeight = 72f;
        msgLe.preferredHeight = 72f;

        messageText = msgObj.AddComponent<TextMeshProUGUI>();
        messageText.text = "";
        messageText.fontSize = 16;
        messageText.color = Cream;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        GameUI.ApplySharedFont(messageText);

        GameObject row = new GameObject("Buttons");
        row.transform.SetParent(inner.transform, false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 42f;
        rowLe.preferredHeight = 42f;

        AddButton(row.transform, "Так", OnYes);
        AddButton(row.transform, "Ні", Hide);

        root.SetActive(false);
    }

    void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnRoot = new GameObject(label + "Button");
        btnRoot.transform.SetParent(parent, false);
        LayoutElement le = btnRoot.AddComponent<LayoutElement>();
        le.minHeight = 42f;
        le.preferredHeight = 42f;

        Image border = btnRoot.AddComponent<Image>();
        border.sprite = GameUI.GetSharedWhiteSprite();
        border.color = BtnBorder;

        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(btnRoot.transform, false);
        RectTransform innerRect = inner.AddComponent<RectTransform>();
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
        GameUI.ApplySharedFont(tmp);
    }

    public void Show(string message, Action onConfirm)
    {
        pendingConfirm = onConfirm;
        if (messageText != null)
            messageText.text = message;
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        pendingConfirm = null;
        if (root != null)
            root.SetActive(false);
    }

    void OnYes()
    {
        Action action = pendingConfirm;
        Hide();
        action?.Invoke();
    }
}
