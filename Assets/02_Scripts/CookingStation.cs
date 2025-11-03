using UnityEngine;

public class CookingStation : MonoBehaviour
{
    public enum StationType { Pot, Pan }

    [Header("Configuración de Estación")]
    public StationType stationType = StationType.Pot;

    [Header("Efectos Visuales")]
    public ParticleSystem steamParticles;
    public Light cookingLight;

    void Start()
    {
        // Inicializar efectos
        if (steamParticles != null)
            steamParticles.Stop();

        if (cookingLight != null)
            cookingLight.enabled = false;
    }

    public void StartCooking()
    {
        // Activar efectos cuando el jugador entra
        if (steamParticles != null)
            steamParticles.Play();

        if (cookingLight != null)
            cookingLight.enabled = true;
    }

    public void StopCooking()
    {
        // Desactivar efectos cuando el jugador sale
        if (steamParticles != null)
            steamParticles.Stop();

        if (cookingLight != null)
            cookingLight.enabled = false;
    }
}