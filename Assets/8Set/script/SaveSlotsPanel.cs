using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotsPanel : MonoBehaviour
{
    static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f, 1f);
    static readonly Color Gold = new Color(0.95f, 0.82f, 0.42f, 1f);
    static readonly Color BtnBg = new Color(0.12f, 0.08f, 0.05f, 0.92f);
    static readonly Color BtnBorder = new Color(0.85f, 0.68f, 0.25f, 0.85f);
    static readonly Color EmptyColor = new Color(0.65f, 0.6f, 0.55f, 1f);

    GameObject root;
    TextMeshProUGUI titleText;
    readonly TextMeshProUGUI[] slotDetailTexts = new TextMeshProUGUI[SaveManager.SlotCount];

    bool loadMode;
    Action<int> onSlotSelected;
    Action onCancelled;
    readonly bool[] slotHasSave = new bool[SaveManager.SlotCount];

    public bool IsOpen => root != null && root.activeSelf;

    public void Build(Transform canvas)
    {
        root = new GameObject("SaveSlotsPanel");
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
        panelRect.sizeDelta = new Vector2(440f, 0f);

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
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        ContentSizeFitter innerFitter = inner.AddComponent<ContentSizeFitter>();
        innerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText(inner.transform, "Оберіть слот", 18, true, Gold, 36f);

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            slotDetailTexts[i] = CreateText(inner.transform, "Слот " + slot + ": …", 14, false, Cream, 22f);
            AddSlotButton(inner.transform, slot);
        }

        AddActionButton(inner.transform, "Скасувати", Hide);

        root.SetActive(false);
    }

    TextMeshProUGUI CreateText(Transform parent, string text, float size, bool title, Color color, float height)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = title ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        GameUI.ApplySharedFont(tmp, title);
        return tmp;
    }

    void AddSlotButton(Transform parent, int slot)
    {
        GameObject btnRoot = new GameObject("Slot" + slot + "Button", typeof(RectTransform));
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
        int captured = slot;
        button.onClick.AddListener(() => OnSlotClicked(captured));

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inner.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Слот " + slot;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Cream;
        tmp.fontStyle = FontStyles.Bold;
        GameUI.ApplySharedFont(tmp);
    }

    void AddActionButton(Transform parent, string label, Action onClick)
    {
        GameObject btnRoot = new GameObject(label + "Button", typeof(RectTransform));
        btnRoot.transform.SetParent(parent, false);
        LayoutElement le = btnRoot.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;

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
        button.onClick.AddListener(() => onClick?.Invoke());

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inner.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Cream;
        GameUI.ApplySharedFont(tmp);
    }

    public void ShowForSave(Action<int> onSelected, Action onCancel = null)
    {
        loadMode = false;
        onSlotSelected = onSelected;
        onCancelled = onCancel;
        if (titleText != null)
            titleText.text = "Зберегти гру — оберіть слот";
        RefreshSlotLabels();
        OpenPanel();
    }

    public void ShowForLoad(Action<int> onSelected, Action onCancel = null)
    {
        loadMode = true;
        onSlotSelected = onSelected;
        onCancelled = onCancel;
        if (titleText != null)
            titleText.text = "Завантажити гру — оберіть слот";
        RefreshSlotLabels();
        OpenPanel();
    }

    void OpenPanel()
    {
        if (root == null) return;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        ClosePanel(true);
    }

    void ClosePanel(bool invokeCancel)
    {
        if (root != null)
            root.SetActive(false);

        if (invokeCancel)
            onCancelled?.Invoke();

        onSlotSelected = null;
        onCancelled = null;
    }

    void RefreshSlotLabels()
    {
        SaveSlotInfo[] localSlots = SaveManager.Instance != null
            ? SaveManager.Instance.GetAllLocalSlotInfo()
            : BuildLocalFallback();

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            if (slotDetailTexts[i] == null) continue;
            SaveSlotInfo info = localSlots[i];
            slotHasSave[i] = info.exists;
            slotDetailTexts[i].text = "Слот " + info.slot + ": " + info.GetDisplayLine();
            slotDetailTexts[i].color = info.exists ? Cream : EmptyColor;
        }

        if (CloudSaveClient.Instance != null && CloudSaveClient.Instance.IsConfigured())
        {
            CloudSaveClient.Instance.FetchSlotSummaries(merged =>
            {
                if (!IsOpen || merged == null) return;
                MergeAndRefreshLabels(merged);
            });
        }
    }

    SaveSlotInfo[] BuildLocalFallback()
    {
        var arr = new SaveSlotInfo[SaveManager.SlotCount];
        for (int i = 0; i < SaveManager.SlotCount; i++)
            arr[i] = SaveManager.GetLocalSlotInfo(i + 1);
        return arr;
    }

    void MergeAndRefreshLabels(SaveSlotInfo[] cloudSlots)
    {
        SaveSlotInfo[] local = SaveManager.Instance != null
            ? SaveManager.Instance.GetAllLocalSlotInfo()
            : BuildLocalFallback();

        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            SaveSlotInfo info = local[i];
            if (i < cloudSlots.Length && cloudSlots[i].exists)
            {
                if (!info.exists || cloudSlots[i].turnNumber >= info.turnNumber)
                    info = cloudSlots[i];
            }

            if (slotDetailTexts[i] == null) continue;
            slotHasSave[i] = info.exists;
            slotDetailTexts[i].text = "Слот " + info.slot + ": " + info.GetDisplayLine();
            slotDetailTexts[i].color = info.exists ? Cream : EmptyColor;
        }
    }

    void OnSlotClicked(int slot)
    {
        if (loadMode && !SlotHasSave(slot))
            return;

        Action<int> callback = onSlotSelected;
        ClosePanel(false);
        callback?.Invoke(slot);
    }

    bool SlotHasSave(int slot)
    {
        if (SaveManager.GetLocalSlotInfo(slot).exists)
            return true;

        int index = slot - 1;
        return index >= 0 && index < slotHasSave.Length && slotHasSave[index];
    }
}
