using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightPad : MonoBehaviour
{
    [Header("Referencias")]
    public int padIndex = 0;                         // 0,1,2…
    public Light spotLight;                          // child "Spot Light"
    public MeshRenderer beamRenderer;                // child "BeamCone" (tu shader volumétrico)
    public MeshRenderer groundHaloRenderer;          // opcional, el quad circular del suelo

    [Header("Colores e Intensidades")]
    public Color onColor = new Color(1f, 0.92f, 0.5f);
    public Color offColor = new Color(0.20f, 0.20f, 0.20f);
    public float onIntensity = 8f;                   // _Intensity del shader
    public float offIntensity = 0.12f;

    [Header("Audio (opcional)")]
    public AudioSource audioSource;
    public AudioClip clickSfx;

    // IDs de propiedades de tus shaders
    static readonly int BeamColorID = Shader.PropertyToID("_BeamColor");
    static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    static readonly int HaloColorID = Shader.PropertyToID("_HaloColor");

    bool isOn = false;

    void Reset()
    {
        // Intenta autollenar referencias
        if (!spotLight) spotLight = GetComponentInChildren<Light>(true);
        if (!beamRenderer) beamRenderer = transform.GetComponentInChildren<MeshRenderer>(true);
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        ApplyState(false, true); // Arranca apagado sin SFX
    }

    void OnMouseDown()
    {
        // Para interacción directa con Collider en 3D
        var mgr = PuzzleSimonManager.Instance;
        if (mgr && mgr.waitingInput && !mgr.showingSequence && mgr.gameStarted)
        {
            mgr.RegisterPlayerPress(padIndex);
            Pulse(0.18f); // feedback visual
            Play(clickSfx);
        }
    }

    public void SetOn(bool on, bool silent = false)
    {
        isOn = on;
        ApplyState(on, silent);
    }

    public void Pulse(float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(CoPulse(seconds));
    }

    System.Collections.IEnumerator CoPulse(float seconds)
    {
        SetOn(true);
        yield return new WaitForSeconds(seconds);
        SetOn(false);
    }

    void ApplyState(bool on, bool silent)
    {
        // Spot real
        if (spotLight) spotLight.enabled = on;

        // Cono volumétrico
        if (beamRenderer)
        {
            var mat = beamRenderer.material; // en playmode ok
            mat.SetColor(BeamColorID, on ? onColor : offColor);
            mat.SetFloat(IntensityID, on ? onIntensity : offIntensity);
        }

        // Halo del suelo (si lo usas)
        if (groundHaloRenderer)
        {
            var mat = groundHaloRenderer.material;
            var c = on ? onColor : offColor;
            mat.SetColor(HaloColorID, c);
            // Puedes incrementar alpha/contraste cambiando _Alpha en tu shader si lo tienes expuesto
        }

        if (!silent && on) Play(clickSfx);
    }

    void Play(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}
