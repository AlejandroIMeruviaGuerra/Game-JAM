using UnityEngine;

public class EnergyEffectManager : MonoBehaviour
{
    [Header("Materiales por energía")]
    public Material fireMat;
    public Material waterMat;
    public Material natureMat;
    public Material lightMat;
    public Material chaosMat;

    [Header("Render target (la esfera principal)")]
    public Renderer targetRenderer;

    private Material currentMat;

    public void SetEffect(string type)
    {
        switch (type)
        {
            case "Red": ApplyMat(fireMat); break;
            case "Blue": ApplyMat(waterMat); break;
            case "Green": ApplyMat(natureMat); break;
            case "Yellow": ApplyMat(lightMat); break;
            case "Purple": ApplyMat(chaosMat); break;
        }
    }

    public void BlendEffect(string typeA, string typeB, float blendFactor)
    {
        Material matA = GetMat(typeA);
        Material matB = GetMat(typeB);

        if (matA == null || matB == null) return;

        Material blend = new Material(matA);
        blend.Lerp(matA, matB, blendFactor);
        ApplyMat(blend);
    }

    void ApplyMat(Material mat)
    {
        currentMat = mat;
        if (targetRenderer != null)
            targetRenderer.material = mat;
    }

    Material GetMat(string type)
    {
        switch (type)
        {
            case "Red": return fireMat;
            case "Blue": return waterMat;
            case "Green": return natureMat;
            case "Yellow": return lightMat;
            case "Purple": return chaosMat;
        }
        return null;
    }

    public void ResetEffect()
    {
        if (targetRenderer != null && currentMat != null)
            targetRenderer.material = currentMat;
    }
}
