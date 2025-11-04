using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPulse : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración del efecto")]
    public float pulseSpeed = 4f;     // velocidad del pulso
    public float scaleAmount = 1.1f;  // cuánto crece el botón
    public bool useHoverGlow = true;  // brillo opcional al pasar el mouse

    private Vector3 originalScale;
    private bool isHovering = false;
    private UnityEngine.UI.Image image;
    private Color originalColor;

    void Start()
    {
        originalScale = transform.localScale;
        image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            originalColor = image.color;
    }

    void Update()
    {
        if (isHovering)
        {
            float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * 0.05f; // efecto respiración suave
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * scaleAmount * scale, Time.deltaTime * 5);

            if (useHoverGlow && image != null)
            {
                Color glow = originalColor * 1.3f;
                image.color = Color.Lerp(image.color, glow, Time.deltaTime * 4);
            }
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 5);
            if (useHoverGlow && image != null)
                image.color = Color.Lerp(image.color, originalColor, Time.deltaTime * 5);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}
