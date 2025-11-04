using UnityEngine;

public class EnergyBackgroundPulse : MonoBehaviour
{
    public Material mat;
    private float baseIntensity = 1f;

    void Start()
    {
        if (mat)
        {
            if (mat.HasProperty("_Intensity"))
                baseIntensity = mat.GetFloat("_Intensity");
        }
    }

    public void SetEnergyColor(Color baseColor, Color pulseColor)
    {
        if (!mat) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_PulseColor"))
            mat.SetColor("_PulseColor", pulseColor);
    }

    public void SetPulseLevel(float level)
    {
        if (!mat) return;
        if (mat.HasProperty("_Intensity"))
            mat.SetFloat("_Intensity", baseIntensity + level * 0.5f);
        if (mat.HasProperty("_Speed"))
            mat.SetFloat("_Speed", 1.5f + level);
    }
}
