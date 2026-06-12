using UnityEngine;
using UnityEngine.UI;

public class MenuBackgroundAnimator : MonoBehaviour
{
    public Image gradientTop;
    public Image gradientBottom;
    public Image vignette;
    public Image backgroundImage;
    public RectTransform glowOrb;

    public Color topColorA = new Color(0.04f, 0.1f, 0.18f, 1f);
    public Color topColorB = new Color(0.08f, 0.2f, 0.28f, 1f);
    public Color bottomColorA = new Color(0.02f, 0.05f, 0.1f, 1f);
    public Color bottomColorB = new Color(0.06f, 0.14f, 0.2f, 1f);

    private float time;
    private Vector3 bgBaseScale = Vector3.one;

    void Start()
    {
        if (backgroundImage != null)
            bgBaseScale = backgroundImage.rectTransform.localScale;
    }

    void Update()
    {
        time += Time.deltaTime;

        float pulse = (Mathf.Sin(time * 0.35f) + 1f) * 0.5f;

        if (gradientTop != null)
            gradientTop.color = Color.Lerp(topColorA, topColorB, pulse);

        if (gradientBottom != null)
            gradientBottom.color = Color.Lerp(bottomColorA, bottomColorB, 1f - pulse);

        if (vignette != null)
        {
            Color v = vignette.color;
            v.a = Mathf.Lerp(0.45f, 0.62f, pulse);
            vignette.color = v;
        }

        if (glowOrb != null)
        {
            float x = Mathf.Sin(time * 0.12f) * 120f;
            float y = Mathf.Cos(time * 0.09f) * 60f;
            glowOrb.anchoredPosition = new Vector2(x, y);
            glowOrb.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, pulse);
        }

        if (backgroundImage != null)
        {
            float zoom = 1f + Mathf.Sin(time * 0.05f) * 0.03f;
            backgroundImage.rectTransform.localScale = bgBaseScale * zoom;
        }
    }
}
