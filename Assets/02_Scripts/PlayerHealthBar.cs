using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth target;
    public Slider slider;
    public Image damageOverlay;   // imagen roja encima
    public Image frame;           // opcional, solo decorativo
    public Text textAmount;       // opcional

    [Header("Animación")]
    public float sliderLerpSpeed = 10f;   // velocidad de la barra verde
    public float damageLerpSpeed = 2f;    // velocidad de la barra roja (más lenta)

    // interno
    private float currentSliderValue;
    private float currentDamageValue;
    private float maxWidth;

    void Start()
    {
        if (target == null)
            target = FindObjectOfType<PlayerHealth>();

        if (target != null)
        {
            target.OnHealthChanged += OnHealthChanged;
        }

        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = target != null ? target.AdjustedMaxHealth : 100;
            slider.value = slider.maxValue;
            currentSliderValue = slider.value;
            currentDamageValue = slider.value;
        }

        if (damageOverlay != null)
        {
            // guardamos el ancho original
            maxWidth = ((RectTransform)damageOverlay.transform).sizeDelta.x;
        }

        UpdateText();
    }

    void OnDestroy()
    {
        if (target != null)
            target.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(int current, int max)
    {
        if (slider != null)
        {
            slider.maxValue = max;
        }

        // objetivo nuevo para la barra verde
        currentSliderValue = current;

        // si curamos, la roja se sube al toque
        if (current > currentDamageValue)
        {
            currentDamageValue = current;
            UpdateDamageOverlay();
        }

        UpdateText();
    }

    void Update()
    {
        if (slider != null)
        {
            // barra verde suave
            slider.value = Mathf.Lerp(slider.value, currentSliderValue, Time.deltaTime * sliderLerpSpeed);
        }

        // barra roja se retrasa hacia la verde
        if (currentDamageValue > currentSliderValue)
        {
            currentDamageValue = Mathf.Lerp(currentDamageValue, currentSliderValue, Time.deltaTime * damageLerpSpeed);
            UpdateDamageOverlay();
        }
    }

    void UpdateDamageOverlay()
    {
        if (damageOverlay == null || slider == null) return;

        float pct = slider.maxValue > 0 ? currentDamageValue / slider.maxValue : 0f;
        pct = Mathf.Clamp01(pct);

        RectTransform rt = (RectTransform)damageOverlay.transform;
        rt.sizeDelta = new Vector2(maxWidth * pct, rt.sizeDelta.y);
    }

    void UpdateText()
    {
        if (textAmount != null && target != null)
        {
            textAmount.text = $"{target.currentHealth} / {target.AdjustedMaxHealth}";
        }
    }
}
