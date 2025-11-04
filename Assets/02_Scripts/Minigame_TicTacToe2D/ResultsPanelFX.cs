using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// Añádelo al ResultsPanel (mismo objeto que el Image de fondo).
/// No requiere sprites: genera degradado y viñeta por código y crea 4 líneas de marco.
[DisallowMultipleComponent]
public class ResultsPanelFX : MonoBehaviour
{
    [Header("Orden de capas (todas se crean como hijos)")]
    public int sortingOffset = 0; // por si quieres mover estas capas arriba/abajo entre tus hijos

    [Header("Degradado vertical")]
    public bool enableGradient = true;
    public Color gradTop = new Color(0.02f, 0.06f, 0.12f, 0.75f);
    public Color gradBottom = new Color(0f, 0f, 0f, 0.0f); // se funde a negro
    [Range(64, 2048)] public int gradHeight = 512;

    [Header("Viñeta (radial)")]
    public bool enableVignette = true;
    [Range(0f, 1f)] public float vignetteIntensity = 0.65f; // qué tan fuerte oscurece el borde
    [Range(0.2f, 1f)] public float vignetteSoftness = 0.55f; // qué tan gradual
    [Range(64, 2048)] public int vignetteTexSize = 512;

    [Header("Marco neón (4 líneas)")]
    public bool enableNeonFrame = true;
    public Color frameColor = new Color(0f, 1f, 1f, 0.25f);
    public float frameThickness = 6f;
    public float frameInset = 10f; // margen interior respecto al borde del panel

    [Header("Pulso del marco")]
    public bool pulseFrame = true;
    public float pulseSpeed = 2.4f;
    [Range(0f, 1f)] public float pulseAmount = 0.25f; // cuánto varía el alfa

    // --- Interno ---
    RectTransform _rt;
    Image _gradientImg, _vignetteImg;
    List<Image> _frame = new List<Image>(4);
    Texture2D _gradTex, _vignTex;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        BuildOrFindLayers();
        RefreshTextures();
        LayoutFrame();
    }

    void OnEnable()
    {
        // asegura que estén visibles al abrir el panel
        ApplyGradient();
        ApplyVignette();
        SetFrameColor(frameColor);
    }

    void OnDestroy()
    {
        if (_gradTex) Destroy(_gradTex);
        if (_vignTex) Destroy(_vignTex);
    }

    void Update()
    {
        if (enableNeonFrame && pulseFrame && _frame.Count == 4)
        {
            float s = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1
            float a = Mathf.Lerp(frameColor.a * (1f - pulseAmount), frameColor.a * (1f + pulseAmount), s);
            var c = frameColor; c.a = a;
            SetFrameColor(c);
        }
    }

    // ========== Construcción de capas ==========
    void BuildOrFindLayers()
    {
        // Asegura jerarquía 0..1 anchors para cubrir todo el panel
        if (_rt)
        {
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.one;
        }

        _gradientImg = FindOrCreateImage("Layer.Gradient", sortingOffset + 0);
        _vignetteImg = FindOrCreateImage("Layer.Vignette", sortingOffset + 1);

        if (_frame.Count == 0)
        {
            _frame.Add(FindOrCreateImage("Frame.Top", sortingOffset + 2));
            _frame.Add(FindOrCreateImage("Frame.Bottom", sortingOffset + 2));
            _frame.Add(FindOrCreateImage("Frame.Left", sortingOffset + 2));
            _frame.Add(FindOrCreateImage("Frame.Right", sortingOffset + 2));
        }

        // Config por defecto (sin sprite ⇒ rect sólido)
        if (_gradientImg) { _gradientImg.raycastTarget = false; }
        if (_vignetteImg) { _vignetteImg.raycastTarget = false; }
        foreach (var f in _frame) if (f) f.raycastTarget = false;
    }

    Image FindOrCreateImage(string name, int siblingAdd)
    {
        var t = transform.Find(name);
        Image img = null;
        if (t) img = t.GetComponent<Image>();
        if (!img)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            img = go.GetComponent<Image>();
        }

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Colocar al final con offset
        img.transform.SetSiblingIndex(transform.childCount - 1 + siblingAdd);
        return img;
    }

    // ========== Texturas procedurales ==========
    void RefreshTextures()
    {
        if (enableGradient)
        {
            if (_gradTex) Destroy(_gradTex);
            _gradTex = GenerateVerticalGradient(gradHeight, gradTop, gradBottom);
            ApplyGradient();
        }
        else if (_gradientImg) _gradientImg.enabled = false;

        if (enableVignette)
        {
            if (_vignTex) Destroy(_vignTex);
            _vignTex = GenerateVignette(vignetteTexSize, vignetteIntensity, vignetteSoftness);
            ApplyVignette();
        }
        else if (_vignetteImg) _vignetteImg.enabled = false;
    }

    void ApplyGradient()
    {
        if (_gradientImg)
        {
            _gradientImg.enabled = enableGradient;
            _gradientImg.sprite = _gradTex ? Sprite.Create(_gradTex, new Rect(0, 0, _gradTex.width, _gradTex.height), new Vector2(0.5f, 0.5f)) : null;
            _gradientImg.color = Color.white; // sin multiplicar
            _gradientImg.type = Image.Type.Sliced; // no importa, solo que se estire
            _gradientImg.preserveAspect = false;
        }
    }

    void ApplyVignette()
    {
        if (_vignetteImg)
        {
            _vignetteImg.enabled = enableVignette;
            _vignetteImg.sprite = _vignTex ? Sprite.Create(_vignTex, new Rect(0, 0, _vignTex.width, _vignTex.height), new Vector2(0.5f, 0.5f)) : null;
            _vignetteImg.color = Color.black; // textura ya tiene alpha
            _vignetteImg.type = Image.Type.Sliced;
            _vignetteImg.preserveAspect = false;
        }
    }

    // Gradiente top→bottom
    Texture2D GenerateVerticalGradient(int height, Color top, Color bottom)
    {
        int w = 4; // chiquito, se estira
        var tex = new Texture2D(w, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            Color c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply(false, true);
        return tex;
    }

    // Viñeta radial (negro con alpha en bordes)
    Texture2D GenerateVignette(int size, float intensity, float softness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxR = cx; // cuadrada

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / maxR;
                float dy = (y - cy) / maxR;
                float r = Mathf.Sqrt(dx * dx + dy * dy); // 0 centro, 1 borde

                // transición suave en borde
                float edge = Mathf.InverseLerp(softness, 1f, r);
                float a = Mathf.Clamp01(edge) * intensity;

                // píxel negro con alpha a
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        tex.Apply(false, true);
        return tex;
    }

    // ========== Marco ==========
    void LayoutFrame()
    {
        if (!enableNeonFrame || _frame.Count != 4) return;

        // Top / Bottom
        SetupFrameRect(_frame[0].rectTransform, new Vector2(frameInset, -frameInset - frameThickness), new Vector2(-frameInset, -frameInset)); // Top
        SetupFrameRect(_frame[1].rectTransform, new Vector2(frameInset, frameInset), new Vector2(-frameInset, frameInset + frameThickness));   // Bottom

        // Left / Right
        var left = _frame[2].rectTransform;
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.offsetMin = new Vector2(frameInset, frameInset + frameThickness);
        left.offsetMax = new Vector2(frameInset + frameThickness, -frameInset - frameThickness);

        var right = _frame[3].rectTransform;
        right.anchorMin = new Vector2(1f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.offsetMin = new Vector2(-frameInset - frameThickness, frameInset + frameThickness);
        right.offsetMax = new Vector2(-frameInset, -frameInset - frameThickness);

        foreach (var f in _frame)
        {
            f.color = frameColor;
            f.sprite = null;           // rect sólido
            f.type = Image.Type.Simple;
        }
    }

    void SetupFrameRect(RectTransform rt, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = offMin;   // x min, y min (desde top)
        rt.offsetMax = offMax;   // x max, y max (desde top)
    }

    void SetFrameColor(Color c)
    {
        foreach (var f in _frame) if (f) f.color = c;
    }

    // Si cambias parámetros en runtime, puedes llamar a esto:
    public void RebuildNow()
    {
        BuildOrFindLayers();
        RefreshTextures();
        LayoutFrame();
    }
}
