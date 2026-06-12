using UnityEngine;

public class MenuAmbientParticle : MonoBehaviour
{
    public float speed = 12f;
    public float amplitude = 20f;

    private RectTransform rect;
    private Vector2 baseAnchor;
    private float phase;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseAnchor = rect.anchorMin;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        phase += Time.deltaTime * (speed * 0.05f);
        float ox = Mathf.Sin(phase) * amplitude;
        float oy = Mathf.Cos(phase * 0.7f) * amplitude * 0.5f;
        rect.anchoredPosition = new Vector2(ox, oy);
    }
}
