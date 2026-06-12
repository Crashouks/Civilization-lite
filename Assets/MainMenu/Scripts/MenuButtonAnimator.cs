using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverOffsetX = 6f;
    public float duration = 0.15f;

    public Color normalBg = new Color(0.14f, 0.09f, 0.05f, 0.72f);
    public Color hoverBg = new Color(0.22f, 0.14f, 0.06f, 0.88f);
    public Color disabledBg = new Color(0.08f, 0.06f, 0.05f, 0.45f);
    public Color normalText = new Color(0.96f, 0.91f, 0.82f, 1f);
    public Color hoverText = new Color(1f, 0.88f, 0.45f, 1f);
    public Color disabledText = new Color(0.55f, 0.5f, 0.45f, 0.55f);

    private Image background;
    private Image accentBar;
    private TextMeshProUGUI label;
    private RectTransform labelRect;
    private Vector2 labelBasePos;
    private Coroutine animRoutine;
    private bool isHovered;
    private Button button;

    public void Setup(Image bg, Image accent, TextMeshProUGUI text)
    {
        background = bg;
        accentBar = accent;
        label = text;
        labelRect = text != null ? text.rectTransform : null;
        if (labelRect != null) labelBasePos = labelRect.anchoredPosition;
        button = GetComponent<Button>();
        ApplyInstant(false, button != null && !button.interactable);
    }

    public void SetInteractable(bool value)
    {
        if (button != null) button.interactable = value;
        ApplyInstant(isHovered, !value);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isHovered = true;
        Animate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        Animate(false);
    }

    public void ShowInstant()
    {
        if (background != null) background.color = button != null && !button.interactable ? disabledBg : normalBg;
        if (label != null) label.color = button != null && !button.interactable ? disabledText : normalText;
        if (accentBar != null) { Color c = accentBar.color; c.a = 0f; accentBar.color = c; }
        if (labelRect != null) labelRect.anchoredPosition = labelBasePos;
    }

    void Animate(bool hover)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateRoutine(hover));
    }

    IEnumerator AnimateRoutine(bool hover)
    {
        bool disabled = button != null && !button.interactable;
        Color targetBg = disabled ? disabledBg : (hover ? hoverBg : normalBg);
        Color targetText = disabled ? disabledText : (hover ? hoverText : normalText);
        float targetAccent = hover && !disabled ? 1f : 0f;
        Vector2 targetLabelPos = labelBasePos + new Vector2(hover && !disabled ? hoverOffsetX : 0f, 0f);

        Color startBg = background != null ? background.color : normalBg;
        Color startText = label != null ? label.color : normalText;
        float startAccent = accentBar != null ? accentBar.color.a : 0f;
        Vector2 startLabelPos = labelRect != null ? labelRect.anchoredPosition : targetLabelPos;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            if (background != null) background.color = Color.Lerp(startBg, targetBg, k);
            if (label != null) label.color = Color.Lerp(startText, targetText, k);
            if (labelRect != null) labelRect.anchoredPosition = Vector2.Lerp(startLabelPos, targetLabelPos, k);
            if (accentBar != null)
            {
                Color c = accentBar.color;
                c.a = Mathf.Lerp(startAccent, targetAccent, k);
                accentBar.color = c;
            }
            yield return null;
        }

        ApplyInstant(hover, disabled);
        animRoutine = null;
    }

    void ApplyInstant(bool hover, bool disabled)
    {
        if (background != null) background.color = disabled ? disabledBg : (hover ? hoverBg : normalBg);
        if (label != null) label.color = disabled ? disabledText : (hover ? hoverText : normalText);
        if (labelRect != null) labelRect.anchoredPosition = labelBasePos + new Vector2(hover && !disabled ? hoverOffsetX : 0f, 0f);
        if (accentBar != null)
        {
            Color c = accentBar.color;
            c.a = hover && !disabled ? 1f : 0f;
            accentBar.color = c;
        }
    }
}
