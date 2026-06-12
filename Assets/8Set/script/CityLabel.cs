using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityLabel : MonoBehaviour
{
    static readonly string[][] CivCityNames =
    {
        new[] { "Rome", "Neapolis", "Mediolanum", "Carthago", "Athenae" },
        new[] { "Washington", "Boston", "Philadelphia", "Chicago", "Denver" },
        new[] { "Memphis", "Thebes", "Alexandria", "Giza", "Luxor" },
        new[] { "Scythopolis", "Panticapaeum", "Olbia", "Tanais", "Chersonesus" }
    };

    static readonly string[] CivDisplayNames = { "Rome", "America", "Egypt", "Scythia" };
    static readonly System.Collections.Generic.Dictionary<string, int> cityCounters = new System.Collections.Generic.Dictionary<string, int>();

    Canvas canvas;
    RectTransform canvasRect;
    Image backgroundImage;
    Image accentImage;
    TextMeshProUGUI cityNameText;
    TextMeshProUGUI civNameText;

    public static string GenerateCityName(string civName, bool isCapital)
    {
        int civIndex = System.Array.IndexOf(CivDisplayNames, civName);
        if (civIndex < 0) civIndex = 0;

        if (isCapital)
            return CivCityNames[civIndex][0];

        if (!cityCounters.ContainsKey(civName))
            cityCounters[civName] = 1;

        int index = cityCounters[civName]++;
        string[] pool = CivCityNames[civIndex];
        return pool[Mathf.Min(index, pool.Length - 1)];
    }

    public void Setup(string cityName, string civName, Color civColor, bool isCapital)
    {
        if (canvas == null)
            BuildBanner();

        if (cityNameText != null)
            cityNameText.text = isCapital ? cityName + " *" : cityName;

        if (civNameText != null)
            civNameText.text = civName;

        if (accentImage != null)
            accentImage.color = civColor;
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null)
            canvas.gameObject.SetActive(true); // Завжди показуємо мітку міста
    }

    void BuildBanner()
    {
        // Створюємо Canvas для World Space UI
        GameObject canvasObj = new GameObject("CityLabelCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 999999; // Найвищий пріоритет для рендеру поверх усього

        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.localPosition = new Vector3(0f, 1.2f, 0f);
        canvasRect.localScale = Vector3.one * 0.006f; // Ще менший масштаб для меншої панелі
        canvasRect.sizeDelta = new Vector2(350, 80);

        // Додаємо GraphicRaycaster для взаємодії
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Фон
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasRect, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        backgroundImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        backgroundImage.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        // Акцент (кольорова смужка)
        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(canvasRect, false);
        RectTransform accentRect = accentObj.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0.1f, 1f);
        accentRect.sizeDelta = Vector2.zero;
        accentImage = accentObj.AddComponent<UnityEngine.UI.Image>();
        accentImage.color = Color.white;

        // Назва цивілізації
        GameObject civObj = new GameObject("CivName");
        civObj.transform.SetParent(canvasRect, false);
        RectTransform civRect = civObj.AddComponent<RectTransform>();
        civRect.anchorMin = new Vector2(0.15f, 0.6f);
        civRect.anchorMax = new Vector2(0.9f, 0.9f);
        civRect.sizeDelta = Vector2.zero;
        civNameText = civObj.AddComponent<TextMeshProUGUI>();
        civNameText.fontSize = 14;
        civNameText.alignment = TextAlignmentOptions.Left;
        civNameText.color = new Color(0.85f, 0.85f, 0.9f, 1f);

        // Назва міста
        GameObject cityObj = new GameObject("CityName");
        cityObj.transform.SetParent(canvasRect, false);
        RectTransform cityRect = cityObj.AddComponent<RectTransform>();
        cityRect.anchorMin = new Vector2(0.15f, 0.2f);
        cityRect.anchorMax = new Vector2(0.9f, 0.5f);
        cityRect.sizeDelta = Vector2.zero;
        cityNameText = cityObj.AddComponent<TextMeshProUGUI>();
        cityNameText.fontSize = 22;
        cityNameText.fontStyle = FontStyles.Bold;
        cityNameText.alignment = TextAlignmentOptions.Left;
        cityNameText.color = Color.white;
    }

    static Sprite CreateRectSprite(Color color)
    {
        Texture2D tex = new Texture2D(8, 8);
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
    }

    void LateUpdate()
    {
        // Обертаємо canvas щоб він завжди був звернен до камери
        if (canvas != null && Camera.main != null)
        {
            canvasRect.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
