using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Movimiento flotante")]
    public float floatSpeed = 0.5f;  // velocidad del movimiento vertical
    public float floatRange = 0.1f;  // cuánto se mueve arriba y abajo

    [Header("Parpadeo")]
    public float pulseSpeed = 3f;    // velocidad del parpadeo (alpha)

    [Header("Efecto caricaturesco")]
    public float bounceSpeed = 3f;   // velocidad del rebote
    public float bounceHeight = 5f;  // altura del rebote
    public float scalePulseSpeed = 4f; // velocidad del latido en tamaño
    public Color colorA = Color.yellow; // color inicial
    public Color colorB = Color.red;    // color final

    private Vector3 startPos;
    private Vector3 startScale;
    private TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        startPos = transform.localPosition;
        startScale = transform.localScale;
    }

    void Update()
    {
        if (tmp == null) return;

        float time = Time.time;

        // 🌊 Movimiento flotante + rebote cartoon
        float floatY = Mathf.Sin(time * floatSpeed) * floatRange;
        float bounceY = Mathf.Sin(time * bounceSpeed) * (bounceHeight * 0.01f);
        transform.localPosition = startPos + new Vector3(0, floatY + bounceY, 0);

        // 💓 Pulso en tamaño (latido cartoon)
        float scale = 1 + Mathf.Sin(time * scalePulseSpeed) * 0.1f;
        transform.localScale = startScale * scale;

        // ✨ Parpadeo (alpha)
        float alpha = (Mathf.Sin(time * pulseSpeed) + 1f) / 2f;
        Color currentColor = Color.Lerp(colorA, colorB, alpha);
        currentColor.a = alpha;
        tmp.color = currentColor;
    }
}
