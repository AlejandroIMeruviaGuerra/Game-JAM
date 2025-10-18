using UnityEngine;

public enum PowerUpKind
{
    Mayonesa, // Salto +
    Ketchup,  // Curación total
    Mostaza,  // Velocidad +
    Pan       // Escudo (reduce daño)
}

[RequireComponent(typeof(Collider))]
public class PowerUpPickup : MonoBehaviour
{
    [Header("Tipo de Power-Up")]
    public PowerUpKind kind;

    [Header("Parámetros")]
    [Tooltip("Duración en segundos (no aplica a Ketchup).")]
    public float duration = 5f;

    [Tooltip("Multiplicador del efecto (no aplica a Ketchup). " +
             "Mayonesa: salto xN | Mostaza: velocidad xN | Pan: daño xN (usar <1 para reducir)")]
    public float multiplier = 1.5f;

    [Header("Feedback (opcional)")]
    public AudioClip pickSound;
    public GameObject pickVfxPrefab;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var sys = other.GetComponent<PlayerCookingSystem>();
        if (!sys) return;

        switch (kind)
        {
            case PowerUpKind.Mayonesa:
                sys.ActivateJump(multiplier, duration);
                break;

            case PowerUpKind.Ketchup:
                sys.HealFull();
                break;

            case PowerUpKind.Mostaza:
                sys.ActivateSpeed(multiplier, duration);
                break;

            case PowerUpKind.Pan:
                sys.ActivateShield(multiplier, duration);
                break;
        }

        if (pickVfxPrefab) Instantiate(pickVfxPrefab, transform.position, Quaternion.identity);
        if (pickSound) AudioSource.PlayClipAtPoint(pickSound, transform.position);

        Destroy(gameObject);
    }
}
