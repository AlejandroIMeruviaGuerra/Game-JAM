using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TTTTheme : MonoBehaviour
{
    public Button[] cellButtons;
    public Text titleText;
    public Text statusText;

    // Colores
    public Color cellBase = new Color(0.05f, 0.12f, 0.18f, 0.90f);
    public Color cellHover = new Color(0.09f, 0.20f, 0.30f, 0.95f);
    public Color cellPress = new Color(0.00f, 0.85f, 1.00f, 1.00f);
    public Color titleColor = new Color(0.70f, 0.95f, 1.00f);
    public Color statColor = new Color(0.87f, 0.92f, 0.98f);

    void Awake()
    {
        EnsureEventSystem();
        EnsureGraphicRaycasterOnCanvas();
        MakeBackgroundAndLinesNonBlocking();  // ⬅️ muy importante

        // Textos
        if (titleText) StyleText(titleText, titleColor, 64);
        if (statusText) StyleText(statusText, statColor, 36);

        // Celdas
        foreach (var b in cellButtons) if (b) StyleCell(b);
    }

    void EnsureEventSystem()
    {
        if (!FindObjectOfType<EventSystem>())
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var newType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", false);
            if (newType != null) go.AddComponent(newType);
            else go.AddComponent<StandaloneInputModule>();
        }
    }

    void EnsureGraphicRaycasterOnCanvas()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas && !canvas.GetComponent<GraphicRaycaster>())
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    // Desactiva raycasts en fondo y líneas para que no tapen los botones
    void MakeBackgroundAndLinesNonBlocking()
    {
        // Intenta ubicarlos por nombre común (ajusta nombres si usas otros)
        var bg = GameObject.Find("Background");
        if (bg)
        {
            var img = bg.GetComponent<Image>();
            if (img) img.raycastTarget = false;
        }

        var uiLine = GameObject.Find("UILine"); // tu contenedor de líneas
        if (uiLine)
        {
            foreach (var img in uiLine.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
        }
    }

    void StyleText(Text t, Color c, int size)
    {
        if (!t) return;

        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        t.enabled = true;
        t.color = new Color(c.r, c.g, c.b, 1f);
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.canvasRenderer.cull = false;
        t.canvasRenderer.SetAlpha(1f);

        var cg = t.GetComponentInParent<CanvasGroup>();
        if (cg) cg.alpha = 1f;

        // Efectos sutiles
        var sh = t.GetComponent<Shadow>() ?? t.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.6f);
        sh.effectDistance = new Vector2(2, -2);

        var ol = t.GetComponent<Outline>() ?? t.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0, 1, 1, 0.15f);
        ol.effectDistance = new Vector2(1, -1);

        // Asegura orden y escala
        t.transform.SetAsLastSibling();
        var rt = t.rectTransform;
        rt.localScale = Vector3.one;
        var pos = rt.anchoredPosition3D; pos.z = 0f; rt.anchoredPosition3D = pos;
    }

    void StyleCell(Button b)
    {
        // Asegura un Image como targetGraphic
        var img = b.targetGraphic as Image;
        if (!img)
        {
            img = b.GetComponent<Image>();
            if (!img) img = b.gameObject.AddComponent<Image>();
            b.targetGraphic = img;
        }

        // Asegura visibilidad del Image
        img.color = cellBase;
        img.material = null;                 // material UI por defecto
        img.raycastTarget = true;            // el botón debe recibir eventos
        img.canvasRenderer.SetAlpha(1f);
        img.type = Image.Type.Simple;        // sin Sliced/SpriteSwap raros

        // Forzar transición a ColorTint
        b.transition = Selectable.Transition.ColorTint;
        var colors = b.colors;
        colors.normalColor = cellBase;
        colors.highlightedColor = cellHover;
        colors.pressedColor = cellPress;
        colors.selectedColor = cellHover;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
        colors.fadeDuration = 0.08f;
        b.colors = colors;

        // Navegación sin “pegues”
        var nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;

        // Sombra y “glow”
        var ol = b.GetComponent<Outline>() ?? b.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0, 1, 1, 0.4f);
        ol.effectDistance = new Vector2(2, -2);

        var sh = b.GetComponent<Shadow>() ?? b.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.9f);
        sh.effectDistance = new Vector2(4, -4);

        if (!b.GetComponent<CellFX>()) b.gameObject.AddComponent<CellFX>();
    }
}

public class CellFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    RectTransform rt; Vector3 baseScale;

    void Awake() { rt = transform as RectTransform; baseScale = rt.localScale; }

    public void OnPointerEnter(PointerEventData e) { StopAllCoroutines(); StartCoroutine(ScaleTo(baseScale * 1.05f, 0.08f)); }
    public void OnPointerExit(PointerEventData e) { StopAllCoroutines(); StartCoroutine(ScaleTo(baseScale, 0.08f)); }
    public void OnPointerDown(PointerEventData e) { StopAllCoroutines(); StartCoroutine(ScaleTo(baseScale * 0.96f, 0.05f)); }
    public void OnPointerUp(PointerEventData e) { StopAllCoroutines(); StartCoroutine(ScaleTo(baseScale * 1.05f, 0.08f)); }

    IEnumerator ScaleTo(Vector3 target, float t)
    {
        Vector3 s = rt.localScale; float a = 0f;
        while (a < 1f) { a += Time.unscaledDeltaTime / t; rt.localScale = Vector3.Lerp(s, target, a); yield return null; }
    }
}
