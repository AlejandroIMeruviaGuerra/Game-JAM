using UnityEngine;
using UnityEngine.UI;

public class BackgroundFX : MonoBehaviour
{
    Image img;
    [ColorUsage(true, true)] public Color baseColor = new Color(0.1f, 0.2f, 0.4f);
    public float pulseSpeed = 0.8f;
    public float intensity = 1.5f;

    void Start() => img = GetComponent<Image>();

    void Update()
    {
        if (!img) return;
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        img.color = Color.Lerp(baseColor * 0.6f, baseColor * intensity, t);
    }

    public void SetBaseColor(Color c)
    {
        baseColor = c;
    }
}
