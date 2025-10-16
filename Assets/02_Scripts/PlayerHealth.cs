using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;
    private float maxHealthMultiplier = 1f;

    // Propiedad para obtener la vida máxima actualizada
    public int AdjustedMaxHealth => Mathf.RoundToInt(maxHealth * maxHealthMultiplier);

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        Debug.Log($"❤️ Vida inicial: {currentHealth}/{AdjustedMaxHealth} (Multiplicador: {maxHealthMultiplier}x)");
    }

    public void ApplyDamage(int damage)
    {
        // Aplicar multiplicador de daño del sistema de cocción
        PlayerCookingSystem cookingSystem = GetComponent<PlayerCookingSystem>();
        float damageMultiplier = cookingSystem != null ? cookingSystem.DamageMultiplier : 1f;

        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
        currentHealth -= finalDamage;

        Debug.Log($"💥 Daño recibido: {damage} -> {finalDamage} (multiplicador: {damageMultiplier}x) | Vida: {currentHealth}/{AdjustedMaxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetMaxHealthMultiplier(float multiplier)
    {
        if (maxHealthMultiplier != multiplier)
        {
            // Calcular la nueva vida máxima
            int oldMaxHealth = AdjustedMaxHealth;
            maxHealthMultiplier = multiplier;
            int newMaxHealth = AdjustedMaxHealth;

            // Ajustar la vida actual proporcionalmente
            float healthPercentage = (float)currentHealth / oldMaxHealth;
            currentHealth = Mathf.RoundToInt(newMaxHealth * healthPercentage);

            Debug.Log($"❤️ Multiplicador de vida cambiado: {multiplier}x | Vida: {currentHealth}/{newMaxHealth}");
        }
    }

    public void Heal(int amount)
    {
        int adjustedMaxHealth = AdjustedMaxHealth;
        currentHealth = Mathf.Min(adjustedMaxHealth, currentHealth + amount);
        Debug.Log($"💚 Curado: +{amount} | Vida: {currentHealth}/{adjustedMaxHealth}");
    }

    public void Die()
    {
        Debug.Log("💀 El jugador ha muerto.");
        // Aquí puedes desactivar controles, reproducir animación, recargar nivel, etc.
        // For example:
        // GetComponent<PlayerController>().enabled = false;
    }
}