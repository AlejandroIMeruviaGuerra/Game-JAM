using UnityEngine;
using UnityEngine.UI;

public class FancyHealthSliderFX : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth target;
    public Slider slider;
    public Image fillImage;
    public Image glowImage;          // opcional, un hijo Image más grande

    [Header("Animación de valor")]
    public float lerpSpeed = 10f;

    [Header("Pulso general")]
    public float basePulseSpeed = 2f;
    public float basePulseAmount = 0.015f;   // qué tanto escala

    [Header("Flash al recibir daño")]
    public Color hitFlashColor = new Color(1, 1, 1, 0.85f);
    public float hitFlashDuration = 0.15f;

    [Header("Shake por vida baja")]
    public float lowHealthThreshold = 0.25f;
    public float lowHealthShakeAmount = 3f;
    public float lowHealthShakeSpeed = 25f;

    private float targetValue;
    private Vector3 originalScale;
    private Vector3 originalPos;
    private float flashTimer = 0f;

    void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        originalScale = transform.localScale;
        originalPos = ((RectTransform)transform).anchoredPosition;
    }

    void Start()
    {
        if (target == null)
            target = FindObjectOfType<PlayerHealth>();

        if (target != null)
        {
            slider.minValue = 0;
            slider.maxValue = target.AdjustedMaxHealth;
            slider.value = target.currentHealth;
            targetValue = slider.value;

            target.OnHealthChanged += OnHealthChanged;
        }

        if (fillImage == null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    void OnDestroy()
    {
        if (target != null)
            target.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(int current, int max)
    {
        slider.maxValue = max;
        targetValue = current;

        // activar flash
        flashTimer = hitFlashDuration;

        // pequeño punch
        transform.localScale = originalScale * 1.05f;
    }

    void Update()
    {
        // 1) Suavizar valor
        slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * lerpSpeed);

        float pct = slider.maxValue > 0 ? slider.value / slider.maxValue : 0f;

        // 2) Color dinámico
        UpdateFillColor(pct);

        // 3) Pulso base
        ApplyBasePulse();

        // 4) Flash corto al recibir daño
        ApplyHitFlash();

        // 5) Shake si vida baja
        ApplyLowHealthShake(pct);

        // 6) Glow que late
        ApplyGlowPulse(pct);
    }

    void UpdateFillColor(float pct)
    {
        if (fillImage == null) return;

        // rojo -> amarillo -> verde
        Color c;
        if (pct > 0.5f)
        {
            float t = (pct - 0.5f) / 0.5f;
            c = Color.Lerp(new Color(1f, 1f, 0f), new Color(0.2f, 1f, 0.2f), t);
        }
        else
        {
            float t = pct / 0.5f;
            c = Color.Lerp(new Color(1f, 0f, 0f), new Color(1f, 1f, 0f), t);
        }

        fillImage.color = c;
    }

    void ApplyBasePulse()
    {
        // respiración muy leve
        float s = 1f + Mathf.Sin(Time.time * basePulseSpeed) * basePulseAmount;
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale * s, Time.deltaTime * 8f);
    }

    void ApplyHitFlash()
    {
        if (fillImage == null) return;

        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            float t = flashTimer / hitFlashDuration;
            // mezclar flash con color actual
            fillImage.color = Color.Lerp(fillImage.color, hitFlashColor, t);
        }
    }

    void ApplyLowHealthShake(float pct)
    {
        RectTransform rt = (RectTransform)transform;

        if (pct <= lowHealthThreshold)
        {
            float shakeX = Mathf.Sin(Time.time * lowHealthShakeSpeed) * lowHealthShakeAmount;
            rt.anchoredPosition = originalPos + new Vector3(shakeX, 0f, 0f);
        }
        else
        {
            // volver al lugar
            rt.anchoredPosition = Vector3.Lerp(rt.anchoredPosition, originalPos, Time.deltaTime * 6f);
        }
    }

    void ApplyGlowPulse(float pct)
    {
        if (glowImage == null) return;

        // más glow cuando menos vida
        float glowBase = Mathf.Lerp(0.2f, 0.6f, 1f - pct);
        float flicker = Mathf.Sin(Time.time * 6f) * 0.1f;
        Color gc = glowImage.color;
        gc.a = glowBase + flicker;
        glowImage.color = gc;
    }
}
