using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WinLineManagerUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform canvasRoot;     // Asigna el RectTransform de tu Canvas

    [Header("Style")]
    public float coreThickness = 8f;     // grosor línea central
    public float glowThickness = 16f;    // grosor halo
    public float drawTime = 0.35f;       // tiempo de “crecer” la línea
    public float pulseSeconds = 1.0f;    // tiempo de pulso/brillo luego de dibujar
    public int sortingOrder = 2000;      // orden para dibujar encima del fondo/board

    RectTransform containerRT;
    RectTransform coreRT, glowRT;
    Image coreImg, glowImg;

    /// <summary>
    /// Dibuja una línea UI desde el RectTransform start al end, con color dado.
    /// </summary>
    public void DrawUILine(RectTransform start, RectTransform end, Color color)
    {
        if (!canvasRoot) { Debug.LogWarning("[WinLineUI] canvasRoot no asignado."); return; }
        Clear();

        // Contenedor con sub-canvas para asegurarnos que queda por encima
        var root = new GameObject("WinLine", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        containerRT = root.GetComponent<RectTransform>();
        containerRT.SetParent(canvasRoot, false);

        var sub = root.GetComponent<Canvas>();
        sub.overrideSorting = true;
        sub.sortingOrder = sortingOrder;

        // Piezas: núcleo y halo
        coreRT = CreatePiece(containerRT, out coreImg, coreThickness, color);
        var glowColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.35f));
        glowRT = CreatePiece(containerRT, out glowImg, glowThickness, glowColor);

        // Colocar, rotar y animar
        Vector3 a = start.position;
        Vector3 b = end.position;
        float dist = Vector3.Distance(a, b);
        Vector2 dir = (b - a).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        SetupPiece(coreRT, a, angle, 0f, coreThickness);
        SetupPiece(glowRT, a, angle, 0f, glowThickness);

        StopAllCoroutines();
        StartCoroutine(Animate(dist, color));
    }

    /// <summary>
    /// Borra inmediatamente la línea si existe.
    /// </summary>
    public void Clear()
    {
        if (containerRT)
        {
            Destroy(containerRT.gameObject);
            containerRT = coreRT = glowRT = null;
            coreImg = glowImg = null;
        }
    }

    RectTransform CreatePiece(Transform parent, out Image img, float thickness, Color color)
    {
        var go = new GameObject("Piece", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;                   // que no bloquee clics
        rt.sizeDelta = new Vector2(0f, thickness);   // empieza con ancho 0 (animamos luego)
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        return rt;
    }

    void SetupPiece(RectTransform rt, Vector3 pos, float angle, float width, float height)
    {
        rt.position = pos;
        rt.rotation = Quaternion.Euler(0, 0, angle);
        rt.sizeDelta = new Vector2(width, height);
        rt.localScale = Vector3.one;
    }

    IEnumerator Animate(float finalLen, Color baseColor)
    {
        // Fase 1: crecer
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, drawTime);
            float w = Mathf.SmoothStep(0f, finalLen, t);
            if (coreRT) coreRT.sizeDelta = new Vector2(w, coreThickness);
            if (glowRT) glowRT.sizeDelta = new Vector2(w, glowThickness);
            yield return null;
        }

        // Fase 2: pulso de brillo corto
        float p = 0f;
        while (p < pulseSeconds)
        {
            p += Time.unscaledDeltaTime;
            float glow = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f; // 0..1
            if (coreImg) coreImg.color = Color.Lerp(baseColor * 0.75f, Color.white, glow);
            if (glowImg) glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f + 0.3f * glow);
            yield return null;
        }

        Clear();
    }
}
