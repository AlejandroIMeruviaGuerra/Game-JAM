using UnityEngine;
using System.Collections;

public class PlayerCookingSystem : MonoBehaviour
{
    [Header("Estados de Cocción")]
    public PlayerFoodState currentState = PlayerFoodState.Normal;

    [Header("Tiempos de Cocción (segundos)")]
    public float timeToCooked = 10f;
    public float timeToFried = 25f;
    public float timeToBoiled = 25f;
    public float timeToBurned = 35f;
    public float timeToDeath = 60f;

    [Header("Buffs - Cocido")]
    public float cookedHealthMultiplier = 1.3f;
    public float cookedDamageReduction = 0.2f;

    [Header("Buffs - Frito")]
    public float friedDamageReduction = 0.4f;
    public float friedDuration = 30f;

    [Header("Buffs - Hervido")]
    public float boiledSpeedMultiplier = 1.4f;
    public float boiledJumpMultiplier = 1.5f;
    public float boiledDuration = 30f;

    [Header("Debuffs - Quemado")]
    public float burnedSpeedMultiplier = 0.7f;
    public float burnedJumpMultiplier = 0.6f;
    public float burnedDamageMultiplier = 1.3f;

    [Header("Muerte por Cocción")]
    public int burnDamagePerSecond = 5;
    public float deathBurnTime = 10f;

    [Header("Configuración de Posición")]
    public Vector3 potPositionOffset = new Vector3(0, 0.5f, 0);
    public Vector3 panPositionOffset = new Vector3(0, 0.3f, 0);

    [Header("Referencias")]
    public ParticleSystem cookParticles;
    public ParticleSystem burnParticles;
    public AudioClip enterPotSound;
    public AudioClip burnSound;
    public AudioClip deathSound;

    // ================== INTERNOS ==================
    private Player playerMovement;
    private PlayerHealth playerHealth;
    private float currentCookTime = 0f;
    private float burnDeathTimer = 0f;
    private CookingStation currentStation;
    private bool isInCookingStation = false;
    private float stateTimer = 0f;
    private Material originalMaterial;
    private Renderer playerRenderer;
    private bool isBurningToDeath = false;
    private bool showInteractionMessage = false;
    private float hintCooldown = 0f;

    // Guardado de progreso de cocción
    private float savedCookTime = 0f;
    private bool wasInTemporaryState = false;

    // ========= INTEGRACIÓN POWERUPS =========
    // --- BASE (por estado de cocción) ---
    private float baseSpeed = 1f;
    private float baseJump = 1f;
    private float baseDamage = 1f; // 1 = daño normal, <1 reduce daño

    // --- TEMPORALES (por powerups) ---
    private float tempSpeed = 1f; // Mostaza
    private float tempJump = 1f; // Mayonesa
    private float tempDamage = 1f; // Pan (usar <1 para reducir daño)

    // Propiedades combinadas que leerá Player
    public float SpeedMultiplier => baseSpeed * tempSpeed;
    public float JumpMultiplier => baseJump * tempJump;
    public float DamageMultiplier => baseDamage * tempDamage;

    // Coroutines para temporales
    private Coroutine speedCo, jumpCo, shieldCo;

    // Eventos para notificar cambios de estado
    public System.Action<PlayerFoodState> OnStateChanged;

    public enum PlayerFoodState
    {
        Normal, Cooked, Fried, Boiled, Burned
    }

    void Start()
    {
        playerMovement = GetComponent<Player>();
        playerHealth = GetComponent<PlayerHealth>();
        playerRenderer = GetComponentInChildren<Renderer>();

        if (playerRenderer != null)
            originalMaterial = playerRenderer.material;

        UpdateMultipliers(); // Inicializa baseSpeed/baseJump/baseDamage según estado
    }

    void Update()
    {
        // Mensaje de interacción
        if (showInteractionMessage && currentStation != null)
        {
            if (hintCooldown <= 0f)
            {
                string stationName = currentStation.stationType == CookingStation.StationType.Pot ? "Olla" : "Sartén";
                string action = isInCookingStation ? "SALIR de la" : "ENTRAR en la";
                Debug.Log($"📍 Presiona R para {action} {stationName}");
                hintCooldown = 1f; // repetir cada 1s si sigues dentro
            }
            showInteractionMessage = false;
        }
        if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;

        // Interacción
        if (Input.GetKeyDown(KeyCode.R) && currentStation != null)
        {
            if (!isInCookingStation) EnterCookingStation();
            else ExitCookingStation();
        }

        // Proceso de cocción
        if (isInCookingStation)
        {
            ProcessCooking();
        }

        // Muerte por quemado (solo en estación)
        if (isInCookingStation && currentState == PlayerFoodState.Burned)
        {
            HandleBurnDeath();
        }

        // DEBUG rápido de multiplicadores efectivos
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"🏃‍♂️ EFECTIVO - Vel: x{SpeedMultiplier}, Salto: x{JumpMultiplier}, Daño: x{DamageMultiplier}");
        }
    }

    public void EnterCookingStation()
    {
        if (isInCookingStation || currentStation == null) return;

        isInCookingStation = true;
        burnDeathTimer = 0f;
        isBurningToDeath = false;

        // Restaurar progreso
        currentCookTime = savedCookTime;
        Debug.Log($"⏱️ Continuando cocción desde: {currentCookTime:F1}s");

        TeleportToStation();

        if (playerMovement != null) playerMovement.enabled = false;

        if (cookParticles != null) cookParticles.Play();
        if (currentStation != null) currentStation.StartCooking();
        if (enterPotSound != null) AudioSource.PlayClipAtPoint(enterPotSound, transform.position);

        string stationName = currentStation.stationType == CookingStation.StationType.Pot ? "olla" : "sartén";
        Debug.Log($"🍳 Entrando en la {stationName}... Tiempo acumulado: {currentCookTime:F1}s");
    }

    private void TeleportToStation()
    {
        if (currentStation == null) return;
        var col = currentStation.GetComponentInChildren<Collider>();
        Vector3 basePos = currentStation.transform.position;
        if (col != null) basePos.y = col.bounds.max.y;

        Vector3 targetPosition = basePos +
            (currentStation.stationType == CookingStation.StationType.Pot ? potPositionOffset : panPositionOffset);

        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        transform.position = targetPosition;
        if (cc) cc.enabled = true;

        Debug.Log($"🚀 Teletransportado encima de la {currentStation.stationType} en {targetPosition}");
    }

    public void ExitCookingStation()
    {
        if (!isInCookingStation) return;

        savedCookTime = currentCookTime;

        // Estados temporales (frito/hervido) se mantienen fuera, pero no avanzan
        wasInTemporaryState = (currentState == PlayerFoodState.Fried || currentState == PlayerFoodState.Boiled);

        if (playerMovement != null) playerMovement.enabled = true;

        if (currentStation != null)
        {
            Vector3 exitPosition = currentStation.transform.position +
                                   currentStation.transform.forward * 1.5f +
                                   Vector3.up * 0.5f;
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.position = exitPosition;
            if (cc) cc.enabled = true;

            Debug.Log($"🚪 Saliendo de la estación - Tiempo acumulado: {savedCookTime:F1}s - Estado: {currentState}");
        }

        isInCookingStation = false;
        isBurningToDeath = false;

        if (cookParticles != null) cookParticles.Stop();
        if (burnParticles != null) burnParticles.Stop();
        if (currentStation != null) currentStation.StopCooking();

        ApplyStateEffects(); // aplica efectos del estado al salir
    }

    private void ProcessCooking()
    {
        if (currentStation == null) return;

        currentCookTime += Time.deltaTime;

        if (Mathf.FloorToInt(currentCookTime) % 5 == 0 && Time.deltaTime > 0)
        {
            Debug.Log($"⏰ Tiempo de cocción: {currentCookTime:F1}s - Estado: {currentState}");
        }

        switch (currentStation.stationType)
        {
            case CookingStation.StationType.Pot:
                ProcessBoiling();
                break;
            case CookingStation.StationType.Pan:
                ProcessFrying();
                break;
        }

        UpdateCookingEffects();
    }

    private void ProcessBoiling()
    {
        if (currentCookTime >= timeToDeath && currentState == PlayerFoodState.Burned)
        {
            if (!isBurningToDeath) StartBurningToDeath();
        }
        else if (currentCookTime >= timeToBurned && currentState != PlayerFoodState.Burned)
        {
            SetState(PlayerFoodState.Burned);
            Debug.Log("🔥 ¡Te quemaste en la olla! ¡Sal rápido o morirás!");
        }
        else if (currentCookTime >= timeToBoiled && currentState == PlayerFoodState.Cooked)
        {
            SetState(PlayerFoodState.Boiled);
            Debug.Log("💧 ¡Perfectamente hervido! +Velocidad y Salto");
        }
        else if (currentCookTime >= timeToCooked && currentState == PlayerFoodState.Normal)
        {
            SetState(PlayerFoodState.Cooked);
            Debug.Log("🍲 ¡Cocido! +Vida y -Daño recibido");
        }
    }

    private void ProcessFrying()
    {
        if (currentCookTime >= timeToDeath && currentState == PlayerFoodState.Burned)
        {
            if (!isBurningToDeath) StartBurningToDeath();
        }
        else if (currentCookTime >= timeToBurned && currentState != PlayerFoodState.Burned)
        {
            SetState(PlayerFoodState.Burned);
            Debug.Log("🔥 ¡Te quemaste en la sartén! ¡Sal rápido o morirás!");
        }
        else if (currentCookTime >= timeToFried && currentState == PlayerFoodState.Cooked)
        {
            SetState(PlayerFoodState.Fried);
            Debug.Log("🍟 ¡Perfectamente frito! ++Reducción de daño");
        }
        else if (currentCookTime >= timeToCooked && currentState == PlayerFoodState.Normal)
        {
            SetState(PlayerFoodState.Cooked);
            Debug.Log("🍳 ¡Cocido! +Vida y -Daño recibido");
        }
    }

    private void HandleBurnDeath()
    {
        if (!isBurningToDeath) return;

        burnDeathTimer += Time.deltaTime;

        if (burnDeathTimer >= 1f)
        {
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(burnDamagePerSecond);
                Debug.Log($"🔥 ¡Quemándose! Vida: {playerHealth.currentHealth} - ¡SAL RÁPIDO!");
            }
            burnDeathTimer = 0f;
        }

        if (Mathf.FloorToInt(burnDeathTimer * 2f) % 2 == 0 && Time.deltaTime > 0)
        {
            float timeLeft = deathBurnTime - burnDeathTimer;
            if (timeLeft > 0 && timeLeft < 10f)
            {
                Debug.Log($"⚠️ ¡QUEMÁNDOSE! Tiempo restante: {timeLeft:F1}s - ¡SAL CON R!");
            }
        }

        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            DieFromBurning();
        }
    }

    private void StartBurningToDeath()
    {
        isBurningToDeath = true;
        burnDeathTimer = 0f;

        if (burnParticles != null)
        {
            burnParticles.Play();
            var main = burnParticles.main;
            main.startColor = Color.red;
        }

        Debug.Log("💀 ¡CRÍTICO! Comenzando a quemarse hasta la muerte...");
    }

    private void DieFromBurning()
    {
        Debug.Log("💀 ¡HAS MUERTO QUEMADO! Demasiado tiempo en la estación de cocina.");

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);

        if (playerHealth != null)
        {
            playerHealth.Die();
        }

        ExitCookingStation();
    }

    private void SetState(PlayerFoodState newState)
    {
        if (currentState == newState) return;

        var prev = currentState;
        currentState = newState;

        UpdateMultipliers(); // recalcula baseSpeed/baseJump/baseDamage
        UpdateVisuals();
        ApplyStateEffects();

        Debug.Log($"🔄 Cambio de estado: {prev} -> {newState}");

        OnStateChanged?.Invoke(newState);

        if (newState == PlayerFoodState.Burned && isInCookingStation)
        {
            StartBurningToDeath();
        }
    }

    // *** AQUÍ está la clave: Solo fija los MULTIPLICADORES BASE ***
    private void UpdateMultipliers()
    {
        switch (currentState)
        {
            case PlayerFoodState.Normal:
                baseSpeed = 1f;
                baseJump = 1f;
                baseDamage = 1f;
                break;
            case PlayerFoodState.Cooked:
                baseSpeed = 1f;
                baseJump = 1f;
                baseDamage = 1f - cookedDamageReduction;
                break;
            case PlayerFoodState.Fried:
                baseSpeed = 1f;
                baseJump = 1f;
                baseDamage = 1f - friedDamageReduction;
                break;
            case PlayerFoodState.Boiled:
                baseSpeed = boiledSpeedMultiplier;
                baseJump = boiledJumpMultiplier;
                baseDamage = 1f;
                break;
            case PlayerFoodState.Burned:
                baseSpeed = burnedSpeedMultiplier;
                baseJump = burnedJumpMultiplier;
                baseDamage = burnedDamageMultiplier;
                break;
        }

        Debug.Log(
            $"📊 BASE - Vel:{baseSpeed}x Salto:{baseJump}x DañoBase:{baseDamage} | " +
            $"TEMP - Vel:{tempSpeed}x Salto:{tempJump}x DañoTemp:{tempDamage} | " +
            $"EFECTIVO - Vel:{SpeedMultiplier}x Salto:{JumpMultiplier}x Daño:{DamageMultiplier}x"
        );
    }

    private void ApplyStateEffects()
    {
        if (playerHealth != null)
        {
            if (currentState == PlayerFoodState.Cooked)
            {
                playerHealth.SetMaxHealthMultiplier(cookedHealthMultiplier);
                Debug.Log($"❤️ Vida máxima aumentada: {cookedHealthMultiplier}x");
            }
            else
            {
                playerHealth.SetMaxHealthMultiplier(1f);
            }
        }

        if (!isInCookingStation)
        {
            UpdateVisuals();

            if (currentState == PlayerFoodState.Boiled || currentState == PlayerFoodState.Fried)
            {
                if (enterPotSound != null)
                    AudioSource.PlayClipAtPoint(enterPotSound, transform.position);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (playerRenderer == null) return;

        switch (currentState)
        {
            case PlayerFoodState.Normal:
                playerRenderer.material.color = Color.white;
                break;
            case PlayerFoodState.Cooked:
                playerRenderer.material.color = Color.Lerp(Color.white, Color.yellow, 0.3f);
                break;
            case PlayerFoodState.Fried:
                playerRenderer.material.color = Color.Lerp(Color.yellow, new Color(0.5f, 0.3f, 0f), 0.5f);
                break;
            case PlayerFoodState.Boiled:
                playerRenderer.material.color = Color.Lerp(Color.white, Color.blue, 0.3f);
                break;
            case PlayerFoodState.Burned:
                playerRenderer.material.color = Color.Lerp(Color.black, new Color(0.3f, 0.1f, 0f), 0.5f);
                break;
        }

        Debug.Log($"🎨 Color cambiado a: {currentState}");
    }

    private void UpdateCookingEffects()
    {
        if (cookParticles != null)
        {
            var main = cookParticles.main;
            switch (currentState)
            {
                case PlayerFoodState.Cooked:
                    main.startColor = Color.yellow;
                    break;
                case PlayerFoodState.Fried:
                    main.startColor = new Color(0.8f, 0.5f, 0f);
                    break;
                case PlayerFoodState.Boiled:
                    main.startColor = Color.blue;
                    break;
                case PlayerFoodState.Burned:
                    main.startColor = Color.red;
                    break;
                default:
                    main.startColor = Color.white;
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var station = other.GetComponentInParent<CookingStation>();
        if (station != null && !isInCookingStation)
        {
            currentStation = station;
            showInteractionMessage = true;
            Debug.Log($"🟢 Trigger con {station.stationType}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var station = other.GetComponentInParent<CookingStation>();
        if (station != null && station == currentStation && !isInCookingStation)
        {
            currentStation = null;
            showInteractionMessage = false;
            Debug.Log("🔴 Saliste del trigger de la estación");
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var station = hit.collider.GetComponentInParent<CookingStation>();
        if (station != null && !isInCookingStation)
        {
            currentStation = station;
            showInteractionMessage = true;
            Debug.Log($"🧱 Contacto con {station.stationType} (collider sólido)");
        }
    }

    public bool IsBurned() => currentState == PlayerFoodState.Burned;
    public bool IsBurningToDeath() => isBurningToDeath;
    public bool HasBuff() => currentState == PlayerFoodState.Cooked ||
                             currentState == PlayerFoodState.Fried ||
                             currentState == PlayerFoodState.Boiled;

    public void ForceState(PlayerFoodState newState) => SetState(newState);
    public float GetCurrentCookTime() => currentCookTime;
    public float GetSavedCookTime() => savedCookTime;

    public void ResetCookingState()
    {
        SetState(PlayerFoodState.Normal);
        currentCookTime = 0f;
        savedCookTime = 0f;
        stateTimer = 0f;
        isBurningToDeath = false;
        wasInTemporaryState = false;
    }

    // ================== POWERUPS TEMPORALES ==================

    /// <summary>Mostaza: velocidad xN por 'duration' s</summary>
    public void ActivateSpeed(float multiplier, float duration)
    {
        if (speedCo != null) StopCoroutine(speedCo);
        speedCo = StartCoroutine(BuffTimed(
            onStart: () => tempSpeed = multiplier,
            onEnd: () => tempSpeed = 1f,
            duration: duration,
            debug: $"⚡ Velocidad x{multiplier} por {duration:0.##}s"
        ));
    }

    /// <summary>Mayonesa: salto xN por 'duration' s</summary>
    public void ActivateJump(float multiplier, float duration)
    {
        if (jumpCo != null) StopCoroutine(jumpCo);
        jumpCo = StartCoroutine(BuffTimed(
            onStart: () => tempJump = multiplier,
            onEnd: () => tempJump = 1f,
            duration: duration,
            debug: $"🦘 Salto x{multiplier} por {duration:0.##}s"
        ));
    }

    /// <summary>Pan: daño xN (usar &lt;1 para reducir) por 'duration' s</summary>
    public void ActivateShield(float damageMultiplier, float duration)
    {
        if (shieldCo != null) StopCoroutine(shieldCo);
        float clamped = Mathf.Clamp(damageMultiplier, 0.05f, 1f);
        shieldCo = StartCoroutine(BuffTimed(
            onStart: () => tempDamage = clamped,
            onEnd: () => tempDamage = 1f,
            duration: duration,
            debug: $"🛡️ Escudo activo (daño x{clamped}) por {duration:0.##}s"
        ));
    }

    /// <summary>Ketchup: curación instantánea al 100%</summary>
    public void HealFull()
    {
        if (!playerHealth) return;
        int toHeal = playerHealth.AdjustedMaxHealth - playerHealth.currentHealth;
        playerHealth.Heal(toHeal);
        Debug.Log("🧴 Ketchup: Vida restaurada al 100%.");
    }

    private IEnumerator BuffTimed(System.Action onStart, System.Action onEnd, float duration, string debug)
    {
        onStart?.Invoke();
        Debug.Log(debug);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        onEnd?.Invoke();
        Debug.Log("⏲️ Buff temporal finalizado.");
    }
}
