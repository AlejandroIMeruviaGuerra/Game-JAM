using UnityEngine;
using System.Collections;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class SpotLightEnhancer : MonoBehaviour
{
    [Header("REFERENCIAS (opcionales)")]
    public Renderer volumetricBeamRenderer;   // asigna el MeshRenderer del cono
    public Renderer groundHaloRenderer;       // asigna el MeshRenderer del halo

    [Header("Light Settings")]
    public Color lightColor = new Color(1f, 0.835f, 0.29f);
    [Min(0)] public float intensity = 25f;
    [Min(0.1f)] public float range = 18f;
    [Range(1f, 179f)] public float spotAngle = 62f;
    public LightShadows shadows = LightShadows.Soft;
    [Range(0f, 2f)] public float shadowBias = 0.02f;
    [Range(0f, 2f)] public float shadowNormalBias = 0.4f;

    [Header("Cookie (sector)")]
    [Range(0f, 180f)] public float arcAngleDeg = 120f;   // ← ahora puede ir hasta 0
    [Range(-180f, 180f)] public float arcOffsetDeg = 0f;
    [Range(0f, 1f)] public float edgeSoftness = 0.45f;
    [Range(0f, 0.95f)] public float innerRadius = 0f;
    [Range(0f, 1f)] public float innerSoftness = 0.25f;
    [Range(128, 2048)] public int cookieResolution = 512;
    public FilterMode cookieFilter = FilterMode.Bilinear;

    [Header("Sincronización con materiales")]
    [Range(0f, 5f)] public float beamIntensityMul = 0.2f;
    [Range(0f, 1f)] public float haloAlpha = 0.85f;

    [Header("Animaciones (opcional)")]
    public bool pulseIntensity = false;
    [Min(0)] public float pulseSpeed = 2f;
    [Range(0f, 2f)] public float pulseAmount = 0.2f;
    public bool hueCycle = false;
    public float hueSpeed = 0.1f;

    private Light _light;
    private Texture2D _cookie;
    private float _baseIntensity;

    void OnEnable()
    {
        _light = GetComponent<Light>();
        _light.type = LightType.Spot;
        _baseIntensity = intensity;

        ApplyLightSettings();
        RebuildCookie();
        SyncMaterials();

        StopAllCoroutines();
        if (pulseIntensity) StartCoroutine(CoPulse());
    }

    void OnDisable()
    {
        if (_cookie)
        {
            DestroyImmediate(_cookie);
            _cookie = null;
        }
        StopAllCoroutines();
    }

    void OnValidate()
    {
        if (!_light) _light = GetComponent<Light>();
        ApplyLightSettings();
        RebuildCookie();
        SyncMaterials();
    }

    void Update()
    {
        if (hueCycle)
        {
            Color.RGBToHSV(lightColor, out float h, out float s, out float v);
            h = (h + hueSpeed * Time.deltaTime) % 1f;
            lightColor = Color.HSVToRGB(h, s, v);
            ApplyLightSettings();
            SyncMaterials();
        }
    }

    IEnumerator CoPulse()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f);
            float mul = Mathf.Lerp(1f - pulseAmount, 1f + pulseAmount, t);
            _light.intensity = _baseIntensity * mul;

            if (volumetricBeamRenderer)
            {
                var m = Application.isPlaying ? volumetricBeamRenderer.material : volumetricBeamRenderer.sharedMaterial;
                if (m != null)
                    m.SetFloat("_Intensity", Mathf.Max(0f, _baseIntensity * beamIntensityMul * mul));
            }

            yield return null;
        }
    }

    void ApplyLightSettings()
    {
        if (_light == null) return;
        _light.color = lightColor;
        _light.intensity = intensity;
        _light.range = range;
        _light.spotAngle = spotAngle;
        _light.shadows = shadows;
        _light.shadowBias = shadowBias;
        _light.shadowNormalBias = shadowNormalBias;
        _light.cookie = _cookie;
        _light.cookieSize = 1f;
    }

    void SyncMaterials()
    {
        // Cono volumétrico
        if (volumetricBeamRenderer)
        {
            var m = Application.isPlaying ? volumetricBeamRenderer.material : volumetricBeamRenderer.sharedMaterial;
            if (m != null)
            {
                m.SetColor("_BeamColor", lightColor);
                m.SetFloat("_Intensity", Mathf.Max(0f, intensity * beamIntensityMul));
            }
        }

        // Halo del suelo
        if (groundHaloRenderer)
        {
            var m2 = Application.isPlaying ? groundHaloRenderer.material : groundHaloRenderer.sharedMaterial;
            if (m2 != null)
            {
                var col = new Color(lightColor.r, lightColor.g, lightColor.b, haloAlpha);
                m2.SetColor("_HaloColor", col);
            }
        }
    }

    void RebuildCookie()
    {
        int W = Mathf.Clamp(cookieResolution, 128, 2048);
        int H = W;
        if (_cookie && (_cookie.width != W || _cookie.height != H))
        {
            DestroyImmediate(_cookie);
            _cookie = null;
        }
        if (_cookie == null)
        {
            _cookie = new Texture2D(W, H, TextureFormat.R8, false, true);
            _cookie.wrapMode = TextureWrapMode.Clamp;
            _cookie.filterMode = cookieFilter;
        }

        float cx = (W - 1) * 0.5f;
        float cy = (H - 1) * 0.5f;
        float radius = Mathf.Min(cx, cy);

        float halfArc = Mathf.Clamp(arcAngleDeg * 0.5f, 0f, 179f) * Mathf.Deg2Rad;
        float arcOffset = arcOffsetDeg * Mathf.Deg2Rad;

        float innerR = Mathf.Clamp01(innerRadius);
        float outerFeather = Mathf.Lerp(0.0f, 0.15f, edgeSoftness);
        float innerFeather = (innerR > 0f) ? Mathf.Lerp(0.0f, 0.12f, innerSoftness) : 0f;

        Color32[] buf = new Color32[W * H];

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = (x - cx);
                float dy = (y - cy);
                float r = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                float ang = Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f + arcOffset;
                ang = Mathf.Atan2(Mathf.Sin(ang), Mathf.Cos(ang));

                float radialOuter = SmoothStepInv(r, 1f - outerFeather, 1f);
                float radialInner = 1f;
                if (innerR > 0f) radialInner = SmoothStep(r, innerR, innerR + innerFeather);
                float radialMask = Mathf.Clamp01(radialOuter * radialInner);

                float a = Mathf.Abs(ang);
                float angMask = SmoothStep(a, halfArc - halfArc * edgeSoftness, halfArc);

                float v = Mathf.Clamp01(radialMask * angMask);
                byte g = (byte)(v * 255);
                buf[y * W + x] = new Color32(g, g, g, 255);
            }
        }

        _cookie.SetPixels32(buf);
        _cookie.Apply(false, false);
        _light.cookie = _cookie;
    }

    static float SmoothStep(float x, float edge0, float edge1)
    {
        float t = Mathf.InverseLerp(edge0, edge1, x);
        return Mathf.Clamp01(t * t * (3f - 2f * t));
    }

    static float SmoothStepInv(float x, float edge0, float edge1)
    {
        return 1f - SmoothStep(x, edge0, edge1);
    }
}
