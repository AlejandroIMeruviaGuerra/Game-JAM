using UnityEngine;

public class EnergyCoreController : MonoBehaviour
{
    [Header("Material Control")]
    public Material coreMat;
    public float basePulse = 0.5f;
    public float swirlIntensity = 1.2f;

    [Header("Rotación visual del núcleo")]
    public float rotationSpeed = 30f;

    private Renderer rend;
    private float charge = 0f;
    private bool charging = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (coreMat == null && rend != null)
            coreMat = rend.material;

        if (coreMat != null)
        {
            coreMat.SetFloat("_Pulse", 0);
            coreMat.SetFloat("_SwirlIntensity", swirlIntensity);
        }
    }

    void Update()
    {
        // Rotación suave del núcleo
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // Carga energética visual
        if (charging && coreMat != null)
        {
            charge = Mathf.MoveTowards(charge, 1f, Time.deltaTime * 0.5f);
            coreMat.SetFloat("_Pulse", basePulse + Mathf.Sin(Time.time * 5f) * 0.5f * charge);
        }
    }

    public void SetEnergyColor(Color baseC, Color glowC)
    {
        if (coreMat == null) return;
        coreMat.SetColor("_BaseColor", baseC);
        coreMat.SetColor("_GlowColor", glowC);
    }

    public void ActivatePulse(float intensity = 1f)
    {
        if (coreMat == null) return;
        StartCoroutine(PulseEffect(intensity));
    }

    public void StartCharge()
    {
        charging = true;
    }

    public void ReleaseCharge()
    {
        charging = false;
        charge = 0;
        if (coreMat != null)
            coreMat.SetFloat("_Pulse", 0);
    }

    private System.Collections.IEnumerator PulseEffect(float intensity)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            float val = Mathf.Lerp(basePulse * intensity, 0, t);
            coreMat.SetFloat("_Pulse", val);
            yield return null;
        }
    }
}
