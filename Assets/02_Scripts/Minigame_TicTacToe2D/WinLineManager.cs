using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class WinLineManager : MonoBehaviour
{
    private LineRenderer mainLine, glowLine, shockLine;
    private Camera cam;
    private AudioSource audioSource;

    [Header("⚙️ Parámetros generales")]
    public float depth = 5f;
    public float lineWidth = 0.12f;
    public float glowWidth = 0.35f;
    public float shockWidth = 0.6f;

    [Header("🎨 Colores de energía")]
    public Color baseColor = new(0.0f, 0.8f, 1f, 1f); // Cian
    public Color glowColor = new(0.8f, 0.0f, 1f, 1f); // Magenta

    [Header("💫 Animación")]
    public float drawTime = 0.7f;
    public float fadeTime = 1.4f;
    public float pulseSpeed = 4.5f;
    public float shockIntensity = 0.18f;
    public float shockFrequency = 35f;

    [Header("🌌 Efecto de impacto")]
    public GameObject energyBurstPrefab; // círculo de luz
    public float burstScale = 1.8f;

    [Header("🔊 Sonido")]
    public AudioClip sfxDraw;
    public AudioClip sfxImpact;
    public float sfxVolume = 0.7f;

    void Awake()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();

        mainLine = CreateLine("_MainLine", lineWidth, 1f);
        glowLine = CreateLine("_GlowLine", glowWidth, 0.35f);
        shockLine = CreateLine("_ShockLine", shockWidth, 0.05f);

        mainLine.enabled = glowLine.enabled = shockLine.enabled = false;
    }

    LineRenderer CreateLine(string name, float width, float alpha)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform);
        var lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.widthMultiplier = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(1f, 1f, 1f, alpha);
        lr.numCapVertices = 16;
        lr.textureMode = LineTextureMode.Stretch;
        return lr;
    }

    // --- Compatibilidad con tu controlador ---
    public void DrawLine(Vector3 uiStart, Vector3 uiEnd)
        => DrawLine(uiStart, uiEnd, baseColor, drawTime);

    public void DrawLine(Vector3 uiStart, Vector3 uiEnd, Color color, float duration = 1.2f)
    {
        Vector3 startWorld = cam.ScreenToWorldPoint(new(uiStart.x, uiStart.y, depth));
        Vector3 endWorld = cam.ScreenToWorldPoint(new(uiEnd.x, uiEnd.y, depth));

        StopAllCoroutines();
        StartCoroutine(LineFX(startWorld, endWorld, color, duration));
    }

    IEnumerator LineFX(Vector3 start, Vector3 end, Color color, float duration)
    {
        mainLine.enabled = glowLine.enabled = shockLine.enabled = true;

        // 🔊 sonido inicial
        if (sfxDraw) audioSource.PlayOneShot(sfxDraw, sfxVolume);

        mainLine.SetPosition(0, start);
        glowLine.SetPosition(0, start);
        shockLine.SetPosition(0, start);

        // ⚡ trazo que crece
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / drawTime;
            Vector3 p = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
            mainLine.SetPosition(1, p);
            glowLine.SetPosition(1, p);
            shockLine.SetPosition(1, p);
            yield return null;
        }

        // 💥 impacto visual
        if (energyBurstPrefab)
        {
            var burst = Instantiate(energyBurstPrefab, end, Quaternion.identity);
            burst.transform.localScale = Vector3.one * burstScale;
            Destroy(burst, 1.5f);
        }

        if (sfxImpact) audioSource.PlayOneShot(sfxImpact, sfxVolume);

        // 🌈 pulso y desvanecimiento
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float fade = 1f - (timer / fadeTime);
            Color mixed = Color.Lerp(color, glowColor, pulse);

            mainLine.startColor = mainLine.endColor = mixed * fade;
            glowLine.startColor = glowLine.endColor =
                new Color(mixed.r, mixed.g, mixed.b, 0.7f * fade);

            float shockAlpha =
                Mathf.PerlinNoise(Time.time * shockFrequency, 0f) * shockIntensity * fade;
            shockLine.startColor = shockLine.endColor =
                new Color(1f, 1f, 1f, shockAlpha);

            // cambia grosor levemente para “vibrar”
            float widthPulse = 1f + Mathf.Sin(Time.time * 15f) * 0.1f;
            mainLine.widthMultiplier = lineWidth * widthPulse;
            glowLine.widthMultiplier = glowWidth * widthPulse;

            yield return null;
        }

        mainLine.enabled = glowLine.enabled = shockLine.enabled = false;
    }
}
