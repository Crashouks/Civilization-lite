using UnityEngine;

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

    static Sprite whiteSprite;
    static Font labelFont;

    Transform labelRoot;
    TextMesh cityNameMesh;

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

    void Awake()
    {
        CleanupLegacyLabels();
    }

    void Start()
    {
        if (labelRoot != null) return;

        City city = GetComponent<City>();
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (city == null || manager == null) return;

        city.EnsureDisplayName(manager);
        Setup(city.cityName, city.ownerCivName, manager.GetCivColor(city.ownerCivName), city.isCapital);
    }

    public void Setup(string cityName, string civName, Color civColor, bool isCapital)
    {
        CleanupLegacyLabels();
        BuildBanner(civColor);

        if (cityNameMesh != null)
            cityNameMesh.text = cityName;
    }

    public void SetVisible(bool visible)
    {
        if (labelRoot != null)
            labelRoot.gameObject.SetActive(visible);
    }

    void CleanupLegacyLabels()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (IsLabelChild(child))
                Destroy(child.gameObject);
        }

        labelRoot = null;
        cityNameMesh = null;
    }

    static bool IsLabelChild(Transform child)
    {
        if (child == null) return false;
        if (child.name == "CityLabelRoot" || child.name == "CityLabelCanvas")
            return true;
        if (child.GetComponent<Canvas>() != null)
            return true;
        if (child.GetComponent<TextMesh>() != null)
            return true;
        return child.name.Contains("Label");
    }

    static Font GetLabelFont()
    {
        if (labelFont == null)
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return labelFont;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
            whiteSprite = CreateRectSprite(Color.white);
        return whiteSprite;
    }

    void BuildBanner(Color civColor)
    {
        GameObject rootObj = new GameObject("CityLabelRoot");
        labelRoot = rootObj.transform;
        labelRoot.SetParent(transform, false);
        labelRoot.localPosition = new Vector3(0f, 0.72f, -0.05f);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(labelRoot, false);
        SpriteRenderer bg = bgObj.AddComponent<SpriteRenderer>();
        bg.sprite = GetWhiteSprite();
        bg.color = new Color(0.1f, 0.07f, 0.04f, 0.96f);
        bg.sortingOrder = 50;
        bgObj.transform.localScale = new Vector3(1.65f, 0.34f, 1f);

        GameObject accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(labelRoot, false);
        accentObj.transform.localPosition = new Vector3(-0.74f, 0f, -0.001f);
        accentObj.transform.localScale = new Vector3(0.05f, 0.3f, 1f);
        SpriteRenderer accent = accentObj.AddComponent<SpriteRenderer>();
        accent.sprite = GetWhiteSprite();
        accent.color = civColor;
        accent.sortingOrder = 51;

        GameObject textObj = new GameObject("CityName");
        textObj.transform.SetParent(labelRoot, false);
        textObj.transform.localPosition = new Vector3(0.04f, 0f, -0.002f);

        cityNameMesh = textObj.AddComponent<TextMesh>();
        cityNameMesh.font = GetLabelFont();
        cityNameMesh.fontSize = 64;
        cityNameMesh.characterSize = 0.045f;
        cityNameMesh.anchor = TextAnchor.MiddleCenter;
        cityNameMesh.alignment = TextAlignment.Center;
        cityNameMesh.color = new Color(0.98f, 0.94f, 0.86f, 1f);
        cityNameMesh.fontStyle = FontStyle.Bold;

        MeshRenderer textRenderer = textObj.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 52;
            if (cityNameMesh.font != null && cityNameMesh.font.material != null)
                textRenderer.sharedMaterial = cityNameMesh.font.material;
        }
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
        if (labelRoot != null && Camera.main != null)
            labelRoot.rotation = Camera.main.transform.rotation;
    }
}
