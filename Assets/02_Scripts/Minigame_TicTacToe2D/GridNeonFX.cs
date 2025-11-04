using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UILineNeonFX : MonoBehaviour
{
    private Image img;
    private RectTransform rt;
    private Vector2 baseSize;

    [Header("🎨 Colores Neón")]
    public Color colorA = new Color(0f, 1f, 1f, 1f);   // Cian
    public Color colorB = new Color(1f, 0f, 1f, 1f);   // Magenta

    [Header("⚡ Efectos de energía")]
    public float colorShiftSpeed = 2f;
    public float pulseSpeed = 3f;
    public float pulseStrength = 0.5f;
    public float flickerSpeed = 8f;
    public float flickerIntensity = 0.15f;

    [Header("💡 Brillo global")]
    [Range(0.5f, 3f)] public float brightness = 1.5f;

    [Header("🌊 Grosor dinámico (real)")]
    public float thicknessPulseAmount = 0.3f;
    public float thicknessPulseSpeed = 2f;

    private bool isHorizontal;

    void Awake()
    {
        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        baseSize = rt.sizeDelta;
        isHorizontal = rt.rect.width >= rt.rect.height;
    }

    void Update()
    {
        if (!img) return;

        float t = Time.time;

        // 🌈 Mezcla entre azul ↔ magenta
        float lerp = (Mathf.Sin(t * colorShiftSpeed) + 1f) * 0.5f;
        Color glow = Color.Lerp(colorA, colorB, lerp);

        // 💡 Pulso + parpadeo
        float pulse = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f * pulseStrength;
        float flicker = (Mathf.PerlinNoise(t * flickerSpeed, 0f) - 0.5f) * flickerIntensity;

        Color final = glow * (brightness + pulse + flicker);
        final.a = 1f;
        img.color = final;

        // 🧱 Engrosamiento visible (solo eje del grosor)
        Vector2 newSize = baseSize;
        float pulseFactor = 1f + Mathf.Sin(t * thicknessPulseSpeed) * thicknessPulseAmount;

        if (isHorizontal)
            newSize.y = baseSize.y * pulseFactor;
        else
            newSize.x = baseSize.x * pulseFactor;

        rt.sizeDelta = newSize;
    }
}
