using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Анімація")]
    public float hoverOffsetX = 14f;
    public float hoverScale = 1.02f;
    public float duration = 0.18f;

    [Header("Кольори")]
    public Color normalBg = new Color(0.08f, 0.14f, 0.22f, 0.92f);
    public Color hoverBg = new Color(0.12f, 0.22f, 0.34f, 0.96f);
    public Color disabledBg = new Color(0.06f, 0.08f, 0.12f, 0.55f);
    public Color normalText = new Color(0.94f, 0.9f, 0.82f, 1f);
    public Color hoverText = new Color(1f, 0.92f, 0.55f, 1f);
    public Color disabledText = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private RectTransform rect;
    private Image background;
    private Image accentBar;
    private TextMeshProUGUI label;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Coroutine animRoutine;
    private bool isHovered;
    private Button button;

    public void Setup(Image bg, Image accent, TextMeshProUGUI text)
    {
        background = bg;
        accentBar = accent;
        label = text;
        rect = GetComponent<RectTransform>();
        basePosition = rect.anchoredPosition;
        baseScale = rect.localScale;
        button = GetComponent<Button>();
        ApplyInstant(false, !button.interactable);
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

    public void PlayIntro(float delay)
    {
        StartCoroutine(IntroRoutine(delay));
    }

    IEnumerator IntroRoutine(float delay)
    {
        if (rect == null) yield break;

        Vector2 startPos = basePosition + new Vector2(-80f, 0f);
        rect.anchoredPosition = startPos;
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
            rect.anchoredPosition = Vector2.Lerp(startPos, basePosition, k);
            cg.alpha = k;
            yield return null;
        }

        rect.anchoredPosition = basePosition;
        cg.alpha = 1f;
    }

    void Animate(bool hover)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateRoutine(hover));
    }

    IEnumerator AnimateRoutine(bool hover)
    {
        bool disabled = button != null && !button.interactable;
        Vector2 targetPos = hover && !disabled ? basePosition + new Vector2(hoverOffsetX, 0f) : basePosition;
        Vector3 targetScale = hover && !disabled ? baseScale * hoverScale : baseScale;
        Color targetBg = disabled ? disabledBg : (hover ? hoverBg : normalBg);
        Color targetText = disabled ? disabledText : (hover ? hoverText : normalText);
        float targetAccent = hover && !disabled ? 1f : 0f;

        Vector2 startPos = rect.anchoredPosition;
        Vector3 startScale = rect.localScale;
        Color startBg = background != null ? background.color : normalBg;
        Color startText = label != null ? label.color : normalText;
        float startAccent = accentBar != null ? accentBar.color.a : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, k);
            rect.localScale = Vector3.Lerp(startScale, targetScale, k);
            if (background != null) background.color = Color.Lerp(startBg, targetBg, k);
            if (label != null) label.color = Color.Lerp(startText, targetText, k);
            if (accentBar != null)
            {
                Color c = accentBar.color;
                c.a = Mathf.Lerp(startAccent, targetAccent, k);
                accentBar.color = c;
            }
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        rect.localScale = targetScale;
        if (background != null) background.color = targetBg;
        if (label != null) label.color = targetText;
        if (accentBar != null)
        {
            Color c = accentBar.color;
            c.a = targetAccent;
            accentBar.color = c;
        }
    }

    void ApplyInstant(bool hover, bool disabled)
    {
        if (rect != null)
        {
            rect.anchoredPosition = hover && !disabled ? basePosition + new Vector2(hoverOffsetX, 0f) : basePosition;
            rect.localScale = hover && !disabled ? baseScale * hoverScale : baseScale;
        }
        if (background != null) background.color = disabled ? disabledBg : (hover ? hoverBg : normalBg);
        if (label != null) label.color = disabled ? disabledText : (hover ? hoverText : normalText);
        if (accentBar != null)
        {
            Color c = accentBar.color;
            c.a = hover && !disabled ? 1f : 0f;
            accentBar.color = c;
        }
    }
}
