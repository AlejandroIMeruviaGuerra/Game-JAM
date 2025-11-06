using UnityEngine;
using System.Linq;

public class KetchupOverlay : MonoBehaviour
{
    [Header("Material de efecto")]
    public Material ketchupMat;

    private Renderer rend;
    private Material[] originalMats;

    void Start()
    {
        // obtenemos el renderer del personaje (MeshRenderer o SkinnedMeshRenderer)
        rend = GetComponentInChildren<Renderer>();

        if (rend == null)
        {
            Debug.LogWarning("⚠️ No se encontró Renderer en el personaje.");
            return;
        }

        // guardamos los materiales originales
        originalMats = rend.materials;

        // creamos un nuevo arreglo con el efecto agregado encima
        var newMats = originalMats.ToList();
        newMats.Add(ketchupMat);
        rend.materials = newMats.ToArray();
    }

    void OnDestroy()
    {
        // restauramos los materiales originales al destruir el efecto
        if (rend != null && originalMats != null)
            rend.materials = originalMats;
    }
}
