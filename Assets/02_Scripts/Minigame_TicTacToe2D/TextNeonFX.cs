using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextNeonFX : MonoBehaviour
{
    [Header("🎨 Colores Neón")]
    public Color colorA = new Color(0.0f, 0.8f, 1f, 1f);  // Cian
    public Color colorB = new Color(0.8f, 0.0f, 1f, 1f);  // Magenta

    [Header("⚡ Efectos de luz")]
    public float colorShiftSpeed = 1.2f;
    public float pulseSpeed = 2.5f;
    public float pulseStrength = 0.2f;
    public float flickerSpeed = 8f;
    public float flickerAmount = 0.05f;

    [Header("✨ Movimiento suave")]
    public float scalePulse = 0.05f;  // cuánto “late”
    public float scaleSpeed = 2f;

    private Text uiText;
    private TextMeshProUGUI tmpText;
    private Shadow shadow;
    private Outline outline;
    private Vector3 baseScale;

    void Awake()
    {
        uiText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
        shadow = gameObject.GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
        outline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();

        shadow.effectColor = new Color(0f, 0.1f, 0.3f, 0.6f); // sombra azul suave
        outline.effectColor = new Color(0f, 1f, 1f, 0.6f);
        outline.effectDistance = new Vector2(2, -2);

        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = Time.time;

        // 🌈 cambio entre colores (azul ↔ magenta)
        float lerp = (Mathf.Sin(t * colorShiftSpeed) + 1f) * 0.5f;
        Color main = Color.Lerp(colorA, colorB, lerp);

        // 💡 pulso neón
        float pulse = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f * pulseStrength;
        float flicker = (Mathf.PerlinNoise(t * flickerSpeed, 0f) - 0.5f) * flickerAmount;

        Color glow = main * (1.2f + pulse + flicker);
        glow.a = 1f;

        // 🧩 aplicar color
        if (uiText)
            uiText.color = glow;
        else if (tmpText)
            tmpText.color = glow;

        // 🔮 halo dinámico
        outline.effectColor = Color.Lerp(colorA, colorB, lerp);
        shadow.effectColor = new Color(0f, 0.05f, 0.1f, 0.6f);

        // 💫 respiración (escala)
        float scalePulseValue = 1f + Mathf.Sin(t * scaleSpeed) * scalePulse;
        transform.localScale = baseScale * scalePulseValue;
    }
}
