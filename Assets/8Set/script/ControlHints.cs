using TMPro;
using UnityEngine;

public static class ControlHints
{
    public const string SectionTitleUk = "Керування";
    public const string SectionTitleEn = "CONTROLS";

    public const float HeaderFontSize = 16f;
    public const float LineFontSize = 15f;
    public const float HeaderRowHeight = 28f;
    public const float LineRowHeight = 26f;

    static readonly Color HeaderColor = new Color(0.95f, 0.82f, 0.42f, 1f);
    static readonly Color LineColor = new Color(0.98f, 0.94f, 0.86f, 1f);

    static TMP_FontAsset readableFont;

    public static readonly string[] Lines =
    {
        "WASD / Стрілки — рух камери",
        "Коліщатко миші — наближення / віддалення",
        "ЛКМ по юніту — вибрати",
        "ЛКМ по гексу — рух",
        "ЛКМ по ворогу — атака",
        "ЛКМ по місту — панель міста",
        "B — заснувати місто (поселенець)",
        "Esc — меню паузи",
        "«Наступний хід» — закінчити хід",
    };

    public static TMP_FontAsset GetFont()
    {
        if (readableFont == null)
            readableFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return readableFont;
    }

    public static void StyleHeader(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        TMP_FontAsset font = GetFont();
        if (font != null)
            tmp.font = font;

        tmp.fontSize = HeaderFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontWeight = FontWeight.Bold;
        tmp.color = HeaderColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = 0f;
        tmp.characterSpacing = 0f;
        tmp.outlineWidth = 0f;
    }

    public static void StyleLine(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        TMP_FontAsset font = GetFont();
        if (font != null)
            tmp.font = font;

        tmp.fontSize = LineFontSize;
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
        tmp.color = LineColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = 4f;
        tmp.characterSpacing = 0.25f;
        tmp.outlineWidth = 0f;
        tmp.margin = new Vector4(10f, 0f, 0f, 0f);
    }
}
