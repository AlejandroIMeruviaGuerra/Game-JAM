using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth;
    private float maxHealthMultiplier = 1f;

    // ▶ evento para la UI
    public event Action<int, int> OnHealthChanged;
    public event Action OnDie;

    // Vida máxima considerando multiplicador
    public int AdjustedMaxHealth => Mathf.RoundToInt(maxHealth * maxHealthMultiplier);

    void Awake()
    {
        currentHealth = AdjustedMaxHealth;
    }

    void Start()
    {
        Debug.Log($"❤️ Vida inicial: {currentHealth}/{AdjustedMaxHealth} (Multiplicador: {maxHealthMultiplier}x)");
        OnHealthChanged?.Invoke(currentHealth, AdjustedMaxHealth);
    }

    public void ApplyDamage(int damage)
    {
        PlayerCookingSystem cookingSystem = GetComponent<PlayerCookingSystem>();
        float damageMultiplier = cookingSystem != null ? cookingSystem.DamageMultiplier : 1f;

        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
        currentHealth -= finalDamage;

        currentHealth = Mathf.Clamp(currentHealth, 0, AdjustedMaxHealth);

        Debug.Log($"💥 Daño recibido: {damage} -> {finalDamage} | Vida: {currentHealth}/{AdjustedMaxHealth}");

        OnHealthChanged?.Invoke(currentHealth, AdjustedMaxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        int adjustedMaxHealth = AdjustedMaxHealth;
        currentHealth = Mathf.Min(adjustedMaxHealth, currentHealth + amount);
        Debug.Log($"💚 Curado: +{amount} | Vida: {currentHealth}/{adjustedMaxHealth}");
        OnHealthChanged?.Invoke(currentHealth, adjustedMaxHealth);
    }

    public void SetMaxHealthMultiplier(float multiplier)
    {
        if (maxHealthMultiplier != multiplier)
        {
            int oldMaxHealth = AdjustedMaxHealth;
            maxHealthMultiplier = multiplier;
            int newMaxHealth = AdjustedMaxHealth;

            float healthPercentage = (float)currentHealth / oldMaxHealth;
            currentHealth = Mathf.RoundToInt(newMaxHealth * healthPercentage);

            Debug.Log($"❤️ Multiplicador de vida cambiado: {multiplier}x | Vida: {currentHealth}/{newMaxHealth}");
            OnHealthChanged?.Invoke(currentHealth, newMaxHealth);
        }
    }

    public void Die()
    {
        Debug.Log("💀 El jugador ha muerto.");
        OnDie?.Invoke();
        // aquí desactivas controles o lo que quieras
    }
}
