using UnityEngine;
using UnityEngine.UI;

public class CanvasBackgroundSetter : MonoBehaviour
{
    public Sprite backgroundSprite; // arrástralo aquí
    void Start()
    {
        var canvas = GetComponentInChildren<Canvas>() ?? GetComponent<Canvas>();
        if (!canvas) { Debug.LogError("No hay Canvas en este objeto."); return; }

        // Crear Image si no existe
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(canvas.transform, false);

        var rt = bgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;   // (0,0)
        rt.anchorMax = Vector2.one;    // (1,1)
        rt.offsetMin = Vector2.zero;   // Left/Bottom = 0
        rt.offsetMax = Vector2.zero;   // Right/Top = 0
        rt.SetAsFirstSibling();        // al fondo

        var img = bgGO.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = backgroundSprite;
        img.preserveAspect = false;    // llena toda la pantalla
    }
}
