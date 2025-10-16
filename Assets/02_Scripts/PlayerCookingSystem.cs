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

    // Variables internas
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

    // NUEVAS VARIABLES: Para guardar el progreso cuando sales
    private float savedCookTime = 0f;
    private bool wasInTemporaryState = false;

    // Propiedades para acceso externo
    public float SpeedMultiplier { get; private set; } = 1f;
    public float JumpMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;

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

        UpdateMultipliers();
    }

    void Update()
    {
        // Mostrar mensaje de interacción en la consola
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

        // Manejar entrada para interactuar
        if (Input.GetKeyDown(KeyCode.R) && currentStation != null)
        {
            if (!isInCookingStation)
            {
                EnterCookingStation();
            }
            else
            {
                ExitCookingStation();
            }
        }

        // Procesar cocción (SOLO cuando está en la estación)
        if (isInCookingStation)
        {
            ProcessCooking();
        }

        // NUEVA LÓGICA: Los estados temporales NO avanzan fuera de la estación
        // Los estados frito/hervido son permanentes hasta que se conviertan en quemado
        // o hasta que vuelvas a la estación y sigas cocinándote

        // Manejar muerte por quemadura (SOLO cuando está en estación)
        if (isInCookingStation && currentState == PlayerFoodState.Burned)
        {
            HandleBurnDeath();
        }
    }

    public void EnterCookingStation()
    {
        if (isInCookingStation || currentStation == null) return;

        isInCookingStation = true;
        burnDeathTimer = 0f;
        isBurningToDeath = false;

        // NUEVO: Restaurar el tiempo de cocción guardado
        currentCookTime = savedCookTime;
        Debug.Log($"⏱️ Continuando cocción desde: {currentCookTime:F1}s");

        // Teletransportar al jugador encima de la estación
        TeleportToStation();

        // Desactivar movimiento temporalmente
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Efectos visuales y de sonido
        if (cookParticles != null)
            cookParticles.Play();

        if (currentStation != null)
            currentStation.StartCooking();

        if (enterPotSound != null)
            AudioSource.PlayClipAtPoint(enterPotSound, transform.position);

        string stationName = currentStation.stationType == CookingStation.StationType.Pot ? "olla" : "sartén";
        Debug.Log($"🍳 Entrando en la {stationName}... Tiempo acumulado: {currentCookTime:F1}s");
    }

    private void TeleportToStation()
    {
        if (currentStation == null) return;

        // Posición segura: usa el bounds superior del collider de la estación
        var col = currentStation.GetComponentInChildren<Collider>();
        Vector3 basePos = currentStation.transform.position;
        if (col != null) basePos.y = col.bounds.max.y;

        Vector3 targetPosition = basePos +
            (currentStation.stationType == CookingStation.StationType.Pot ? potPositionOffset : panPositionOffset);

        // Teleport limpio
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        transform.position = targetPosition;
        if (cc) cc.enabled = true;

        Debug.Log($"🚀 Teletransportado encima de la {currentStation.stationType} en {targetPosition}");
    }

    public void ExitCookingStation()
    {
        if (!isInCookingStation) return;

        // NUEVO: Guardar el progreso actual antes de salir
        savedCookTime = currentCookTime;

        // NUEVA LÓGICA: Si estás en estado temporal (frito/hervido), se mantiene
        // PERO el timer no avanza fuera de la estación
        if (currentState == PlayerFoodState.Fried || currentState == PlayerFoodState.Boiled)
        {
            wasInTemporaryState = true;
            Debug.Log($"💾 Estado temporal guardado: {currentState}. El estado se mantendrá hasta la próxima cocción.");
        }
        else
        {
            wasInTemporaryState = false;
        }

        // Reactivar movimiento
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Mover a posición segura
        if (currentStation != null)
        {
            Vector3 exitPosition = currentStation.transform.position +
                                 currentStation.transform.forward * 1.5f +
                                 Vector3.up * 0.5f;

            // Usar CharacterController para movimiento seguro
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.position = exitPosition;
            if (cc) cc.enabled = true;

            Debug.Log($"🚪 Saliendo de la estación - Tiempo acumulado: {savedCookTime:F1}s - Estado: {currentState}");
        }

        isInCookingStation = false;
        isBurningToDeath = false;

        // Detener efectos
        if (cookParticles != null)
            cookParticles.Stop();

        if (burnParticles != null)
            burnParticles.Stop();

        if (currentStation != null)
            currentStation.StopCooking();

        // Aplicar efectos del estado actual al salir
        ApplyStateEffects();
    }

    private void ProcessCooking()
    {
        if (currentStation == null) return;

        currentCookTime += Time.deltaTime;

        // Mostrar progreso de cocción cada 5 segundos
        if (Mathf.FloorToInt(currentCookTime) % 5 == 0 && Time.deltaTime > 0)
        {
            Debug.Log($"⏰ Tiempo de cocción: {currentCookTime:F1}s - Estado: {currentState}");
        }

        // Procesar según el tipo de estación
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
        // Sistema de progresión de estados para OLLA
        if (currentCookTime >= timeToDeath && currentState == PlayerFoodState.Burned)
        {
            // Ya está quemado y pasó el tiempo de muerte
            if (!isBurningToDeath)
            {
                StartBurningToDeath();
            }
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

            // NUEVO: Los estados temporales ya no tienen timer que los convierta en quemado
            // Solo te quemas si sigues en la estación más tiempo
        }
        else if (currentCookTime >= timeToCooked && currentState == PlayerFoodState.Normal)
        {
            SetState(PlayerFoodState.Cooked);
            Debug.Log("🍲 ¡Cocido! +Vida y -Daño recibido");
        }
    }

    private void ProcessFrying()
    {
        // Sistema de progresión de estados para SARTÉN
        if (currentCookTime >= timeToDeath && currentState == PlayerFoodState.Burned)
        {
            // Ya está quemado y pasó el tiempo de muerte
            if (!isBurningToDeath)
            {
                StartBurningToDeath();
            }
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

            // NUEVO: Los estados temporales ya no tienen timer que los convierta en quemado
            // Solo te quemas si sigues en la estación más tiempo
        }
        else if (currentCookTime >= timeToCooked && currentState == PlayerFoodState.Normal)
        {
            SetState(PlayerFoodState.Cooked);
            Debug.Log("🍳 ¡Cocido! +Vida y -Daño recibido");
        }
    }

    // NUEVO: Este método ya no se usa para estados temporales fuera de la estación
    // private void HandleTemporaryState()

    private void HandleBurnDeath()
    {
        if (!isBurningToDeath) return;

        burnDeathTimer += Time.deltaTime;

        // Aplicar daño por segundo cuando está quemándose
        if (burnDeathTimer >= 1f)
        {
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(burnDamagePerSecond);
                Debug.Log($"🔥 ¡Quemándose! Vida: {playerHealth.currentHealth} - ¡SAL RÁPIDO!");
            }
            burnDeathTimer = 0f;
        }

        // Mostrar advertencia cada pocos segundos
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
        // Solo cambiar si es un estado diferente
        if (currentState == newState) return;

        currentState = newState;
        UpdateMultipliers();
        UpdateVisuals();
        ApplyStateEffects();

        Debug.Log($"🔄 Cambio de estado: {currentState} -> {newState}");

        // Notificar el cambio de estado
        OnStateChanged?.Invoke(newState);

        if (newState == PlayerFoodState.Burned && isInCookingStation)
        {
            StartBurningToDeath();
        }
    }

    private void UpdateMultipliers()
    {
        switch (currentState)
        {
            case PlayerFoodState.Normal:
                SpeedMultiplier = 1f;
                JumpMultiplier = 1f;
                DamageMultiplier = 1f;
                break;
            case PlayerFoodState.Cooked:
                SpeedMultiplier = 1f;
                JumpMultiplier = 1f;
                DamageMultiplier = 1f - cookedDamageReduction;
                break;
            case PlayerFoodState.Fried:
                SpeedMultiplier = 1f;
                JumpMultiplier = 1f;
                DamageMultiplier = 1f - friedDamageReduction;
                break;
            case PlayerFoodState.Boiled:
                SpeedMultiplier = boiledSpeedMultiplier;
                JumpMultiplier = boiledJumpMultiplier;
                DamageMultiplier = 1f;
                break;
            case PlayerFoodState.Burned:
                SpeedMultiplier = burnedSpeedMultiplier;
                JumpMultiplier = burnedJumpMultiplier;
                DamageMultiplier = burnedDamageMultiplier;
                break;
        }

        Debug.Log($"📊 Multiplicadores actualizados - Vel: {SpeedMultiplier}x, Salto: {JumpMultiplier}x, Daño: {DamageMultiplier}x");
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

        // Aplicar efectos visuales y de sonido incluso fuera de la estación
        if (!isInCookingStation)
        {
            UpdateVisuals();

            // Efectos de sonido para estados temporales al aplicarse
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

    // Sistema de detección
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

    // Métodos para que otros scripts detecten el estado
    public bool IsBurned() => currentState == PlayerFoodState.Burned;
    public bool IsBurningToDeath() => isBurningToDeath;
    public bool HasBuff() => currentState == PlayerFoodState.Cooked ||
                            currentState == PlayerFoodState.Fried ||
                            currentState == PlayerFoodState.Boiled;

    // Método público para forzar un estado (útil para debugging)
    public void ForceState(PlayerFoodState newState)
    {
        SetState(newState);
    }

    // Método para obtener el tiempo actual de cocción
    public float GetCurrentCookTime() => currentCookTime;

    // Método para reiniciar el estado de cocción
    public void ResetCookingState()
    {
        SetState(PlayerFoodState.Normal);
        currentCookTime = 0f;
        savedCookTime = 0f;
        stateTimer = 0f;
        isBurningToDeath = false;
        wasInTemporaryState = false;
    }

    // NUEVO: Método para obtener el tiempo acumulado guardado
    public float GetSavedCookTime() => savedCookTime;
}