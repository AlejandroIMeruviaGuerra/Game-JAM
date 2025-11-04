using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
#if TMP_PRESENT
using TMPro;
#endif

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class NeonButtonFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("🎨 Colores")]
    public Color fillColor = new Color(0.02f, 0.1f, 0.18f, 0.9f);   // fondo (azul oscuro)
    public Color glowColor = new Color(0.0f, 0.9f, 1f, 1f);         // cian neón
    public Color textColor = new Color(0.8f, 0.95f, 1f, 1f);

    [Header("✨ Efectos")]
    public float pulseSpeed = 2.0f;   // respiración
    public float hoverScale = 1.05f;  // al pasar
    public float downScale = 0.96f;  // al presionar
    public float glowIntensity = 0.6f;
    public float outlineThickness = 2.5f;

    [Header("🌟 Shine (barra que cruza)")]
    public bool enableShine = true;
    public float shineSpeed = 1.6f;
    public float shineWidth = 0.25f;  // 0..1 del ancho

    // Internos
    Image img;
    Button btn;
    Outline outline;
    Shadow shadow;

    RectTransform rt;
    Vector3 baseScale;

    // Shine
    RectTransform shineRT;
    Image shineImg;
    float hoverBlend;

#if TMP_PRESENT
    TextMeshProUGUI tmpText;
#endif
    Text uiText;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
        rt = transform as RectTransform;

        // Material/UI por defecto
        img.material = null;
        img.raycastTarget = true;
        img.type = Image.Type.Simple;
        img.color = fillColor;

        outline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowIntensity);
        outline.effectDistance = new Vector2(outlineThickness, -outlineThickness);

        shadow = gameObject.GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(3, -3);

#if TMP_PRESENT
        tmpText = GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText) tmpText.color = textColor;
#endif
        uiText = GetComponentInChildren<Text>();
        if (uiText) uiText.color = textColor;

        baseScale = rt.localScale;

        // Shine child
        if (enableShine)
        {
            var go = new GameObject("Shine");
            go.transform.SetParent(transform, false);
            shineRT = go.AddComponent<RectTransform>();
            shineRT.anchorMin = new Vector2(-shineWidth, 0);
            shineRT.anchorMax = new Vector2(0, 1);
            shineRT.offsetMin = shineRT.offsetMax = Vector2.zero;

            shineImg = go.AddComponent<Image>();
            shineImg.raycastTarget = false;
            // degradado vertical simple usando color con alpha
            shineImg.color = new Color(1f, 1f, 1f, 0.0f);
        }
    }

    void Update()
    {
        // Pulso/respiración + leve brillo
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float pulse = Mathf.Lerp(0.5f, 1f, t);
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowIntensity * pulse);

        // Shine anim cuando está en hover
        if (enableShine && shineRT)
        {
            hoverBlend = Mathf.MoveTowards(hoverBlend, isHover ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            float x = Mathf.Repeat(Time.unscaledTime * shineSpeed, 1f) - shineWidth;
            shineRT.anchorMin = new Vector2(x, 0);
            shineRT.anchorMax = new Vector2(x + shineWidth, 1);
            shineImg.color = new Color(1, 1, 1, 0.08f * hoverBlend);
        }
    }

    bool isHover;
    Coroutine scaleCo;

    IEnumerator ScaleTo(Vector3 target, float dur)
    {
        Vector3 s0 = rt.localScale;
        float a = 0f;
        while (a < 1f)
        {
            a += Time.unscaledDeltaTime / dur;
            rt.localScale = Vector3.Lerp(s0, target, a);
            yield return null;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isHover = true;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(baseScale * hoverScale, 0.08f));
    }

    public void OnPointerExit(PointerEventData e)
    {
        isHover = false;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(baseScale, 0.10f));
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!btn.interactable) return;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(baseScale * downScale, 0.05f));
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!btn.interactable) return;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleTo(baseScale * hoverScale, 0.06f));
    }
}
