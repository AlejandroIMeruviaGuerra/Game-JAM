using UnityEngine;

public class EnergyMomentumManager : MonoBehaviour
{
    [Header("Referencias")]
    public Camera mainCam;
    public AudioSource bgMusic;

    [Header("Configuración")]
    public float basePitch = 1f;
    public float baseIntensity = 0.3f;
    public float comboBoostPitch = 0.1f;
    public float intensityMultiplier = 0.4f;

    private float targetIntensity;
    private float currentIntensity;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        currentIntensity = baseIntensity;
        targetIntensity = baseIntensity;
    }

    public void OnComboChanged(int combo)
    {
        // 🔊 Aumenta la velocidad de la música
        if (bgMusic != null)
            bgMusic.pitch = basePitch + combo * comboBoostPitch;

        // 💡 Intensidad del fondo o del shader (por ahora color)
        targetIntensity = baseIntensity + combo * intensityMultiplier;
        FindObjectOfType<EnergyBackgroundPulse>()?.SetPulseLevel(combo);

    }

    void Update()
    {
        // Suaviza transición
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 2f);

        if (mainCam != null)
            mainCam.backgroundColor = Color.Lerp(Color.black, Color.cyan, currentIntensity);
    }

    public void ResetMomentum()
    {
        targetIntensity = baseIntensity;
        if (bgMusic != null)
            bgMusic.pitch = basePitch;
    }
}
