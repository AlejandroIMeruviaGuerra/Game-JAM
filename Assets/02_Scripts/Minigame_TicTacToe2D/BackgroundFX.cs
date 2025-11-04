using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFX : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material mat;

    [Header("🎨 Colores principales")]
    public Color colorA = new Color(0.0f, 0.15f, 0.3f, 1f);   // Azul base
    public Color colorB = new Color(0.0f, 1f, 1f, 1f);       // Cian neón
    public Color colorC = new Color(1f, 0.0f, 0.6f, 1f);     // Magenta

    [Header("⚡ Efectos de energía")]
    public float colorSpeed = 2.5f;         // cambio de tono rápido
    public float pulseSpeed = 5f;           // respiración rápida
    public float pulseIntensity = 1.2f;     // brillo fuerte
    public float flickerSpeed = 20f;        // parpadeo tipo electricidad
    public float flickerAmount = 0.35f;     // fuerza del parpadeo

    [Header("🌊 Movimiento de ondas")]
    public float waveSpeed = 2.0f;
    public float waveAmplitude = 0.8f;

    [Header("💡 Brillo global")]
    [Range(1f, 4f)] public float brightness = 2.2f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = new Material(Shader.Find("Sprites/Default"));
        sr.material = mat;
    }

    void Update()
    {
        if (!sr) return;

        float t = Time.time;

        // 🔁 Cambio fuerte entre tres colores
        Color blendAB = Color.Lerp(colorA, colorB, (Mathf.Sin(t * colorSpeed) + 1f) * 0.5f);
        Color blendBC = Color.Lerp(colorB, colorC, (Mathf.Sin(t * colorSpeed * 0.7f + 2f) + 1f) * 0.5f);
        Color finalBlend = Color.Lerp(blendAB, blendBC, (Mathf.Sin(t * colorSpeed * 0.4f + 1f) + 1f) * 0.5f);

        // 💥 Pulso neón que “late”
        float pulse = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f * pulseIntensity;

        // ⚡ Parpadeo eléctrico realista
        float flicker = (Mathf.PerlinNoise(t * flickerSpeed, 0f) - 0.5f) * flickerAmount;

        // 🌊 Movimiento oscilante
        float wave = Mathf.Sin(t * waveSpeed) * waveAmplitude;

        // 💫 Combina todo
        Color dynamic = finalBlend * (brightness + pulse + flicker + wave);
        dynamic.a = 1f;
        sr.color = dynamic;
    }
}
