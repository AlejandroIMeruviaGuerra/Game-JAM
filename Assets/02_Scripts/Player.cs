using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    [Range(0f, 1f)] public float airControl = 0.5f;

    [Header("Salto & Gravedad")]
    public float jumpHeight = 1.8f;
    public float gravity = -20f;

    [Header("Escalada Avanzada")]
    public float climbSpeed = 3.5f;
    public string ladderTag = "Ladder";
    public LayerMask ladderLayer = 1;
    public float ladderDetectionRange = 2f;
    public float ladderSnapDistance = 0.3f;
    public float topDetectionHeight = 0.5f; // Altura para detectar la cima

    [Header("Daño por caída")]
    public float safeFallSpeed = 10f;
    public float damagePerMS = 2.0f;

    // Referencias
    private CharacterController controller;
    private PlayerHealth health;
    private PlayerCookingSystem cookingSystem;

    // Estado
    private Vector3 velocity;
    private bool isClimbing = false;
    private bool wasGroundedLastFrame = true;
    private float peakFallSpeed = 0f;
    private Transform currentLadder;
    private Vector3 ladderSnapPosition;
    private Vector3 ladderForward;
    private float ladderTopY;   

    [Header("Sistema de Llaves")]
    public List<string> collectedKeys = new List<string>();

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();
        cookingSystem = GetComponent<PlayerCookingSystem>();
        if (health == null) health = gameObject.AddComponent<PlayerHealth>();

        Debug.Log("Player inicializado. Buscando escaleras con tag: " + ladderTag);
    }

    void Update()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        // Movimiento normal en ejes globales (siempre)
        //Vector3 planarDir = (Vector3.right * inputX + Vector3.forward * inputZ).normalized;
        Vector3 camF = ThirdPersonCameraController.PlanarForward;
        Vector3 camR = ThirdPersonCameraController.PlanarRight;
        Vector3 planarDir = (camR * inputX + camF * inputZ);
        if (planarDir.sqrMagnitude > 0.0001f) planarDir.Normalize();

        // APLICAR MULTIPLICADOR DE VELOCIDAD
        float speedMultiplier = cookingSystem != null ? cookingSystem.SpeedMultiplier : 1f;
        Vector3 planarMove = planarDir * moveSpeed * speedMultiplier;

        bool isGrounded = controller.isGrounded;

        // ================== DETECCIÓN ACTIVA DE ESCALERAS ==================
        CheckForLadders();

        // ================== MODO ESCALADA ==================
        if (isClimbing)
        {
            HandleClimbing(inputX, inputZ);

            // Aplicar gravedad si no está presionando teclas de escalada
            if (!IsClimbingInputPressed())
            {
                ApplyGravityDuringClimb();
            }

            return;
        }

        // ================== MOVIMIENTO NORMAL ==================
        HandleNormalMovement(planarMove, isGrounded);

        wasGroundedLastFrame = isGrounded;
    }

    private void CheckForLadders()
    {
        if (!isClimbing)
        {
            // Solo detectar si no estamos escalando
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

            // Raycast hacia adelante
            bool ladderDetected = Physics.Raycast(
                rayOrigin,
                transform.forward,
                out hit,
                ladderDetectionRange,
                ladderLayer
            );

            if (ladderDetected && hit.collider.CompareTag(ladderTag))
            {
                Debug.Log($"Escalera detectada: {hit.collider.name} - Distancia: {hit.distance:F2}");

                // Entrar en escalada con W o FLECHA ARRIBA
                if (IsClimbingInputPressed())
                {
                    StartClimbing(hit);
                }
            }
        }
        else
        {
            // Si estamos escalando, verificar si debemos salir
            CheckLadderProximity();
        }
    }

    // Detectar ambas teclas para escalar
    private bool IsClimbingInputPressed()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    private void StartClimbing(RaycastHit ladderHit)
    {
        if (!isClimbing)
        {
            isClimbing = true;
            currentLadder = ladderHit.collider.transform;
            velocity.y = 0f;

            CalculateLadderSnapPosition(ladderHit);

            // Calcular la altura de la cima
            CalculateLadderTop(ladderHit);

            Debug.Log($"🎯 ESCALADA ACTIVADA - Cima en Y: {ladderTopY:F2}");
        }
    }

    private void CalculateLadderSnapPosition(RaycastHit hit)
    {
        ladderForward = hit.normal;
        ladderSnapPosition = hit.point - ladderForward * ladderSnapDistance;
        ladderSnapPosition.y = transform.position.y;

        StartCoroutine(SmoothSnapToLadder());
    }

    // **NUEVO: Calcular la cima de manera simple**
    private void CalculateLadderTop(RaycastHit hit)
    {
        // Usar el punto de impacto y subir desde ahí
        Collider ladderCollider = hit.collider;
        Bounds bounds = ladderCollider.bounds;

        // La cima es la parte superior del collider de la escalera
        ladderTopY = bounds.max.y - topDetectionHeight;

        Debug.Log($"📏 Cima calculada: {ladderTopY:F2} (bounds: {bounds.max.y:F2})");
    }

    private System.Collections.IEnumerator SmoothSnapToLadder()
    {
        Vector3 startPosition = transform.position;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, ladderSnapPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = ladderSnapPosition;
    }

    private void CheckLadderProximity()
    {
        if (currentLadder == null)
        {
            StopClimbing();
            return;
        }

        float distanceToLadder = Vector3.Distance(transform.position, ladderSnapPosition);

        if (distanceToLadder > ladderDetectionRange * 1.2f)
        {
            Debug.Log("Alejado de la escalera - Saliendo automáticamente");
            StopClimbing();
        }
    }

    private void StopClimbing()
    {
        if (isClimbing)
        {
            isClimbing = false;
            currentLadder = null;

            // Aplicar gravedad inmediatamente al salir
            ApplyGravity();

            // Pequeño impulso al salir con espacio - APLICAR MULTIPLICADOR DE SALTO
            if (Input.GetButtonDown("Jump"))
            {
                float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;
                velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * gravity);
                Debug.Log($"Salto desde escalera con impulso (multiplicador: {jumpMultiplier}x)");
            }

            Debug.Log("ESCALADA DESACTIVADA - Cayendo por gravedad");
        }
    }

    private void HandleClimbing(float inputX, float inputZ)
    {
        Vector3 climbMovement = Vector3.zero;

        // Movimiento vertical con W/UP (subir) y S/DOWN (bajar)
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput = 1f; // Subir
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput = -1f; // Bajar
        }

        climbMovement.y = verticalInput * climbSpeed;

        // **DETECCIÓN SIMPLE DE CIMA: Verificar altura del jugador**
        if (transform.position.y >= ladderTopY && verticalInput > 0)
        {
            Debug.Log("🏁 ¡Llegaste a la cima! Saltando automáticamente...");
            JumpFromTop();
            return;
        }

        // Movimiento lateral muy limitado
        if (Mathf.Abs(inputX) > 0.1f)
        {
            climbMovement += transform.right * inputX * climbSpeed * 0.1f;
        }

        // Mantener posición frente a la escalera
        MaintainLadderPosition();

        // Aplicar movimiento
        controller.Move(climbMovement * Time.deltaTime);

        // Debug de altura
        if (verticalInput > 0)
        {
            Debug.Log($"📊 Altura: {transform.position.y:F2} / Cima: {ladderTopY:F2}");
        }

        // Salir de escalada
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.E))
        {
            StopClimbing();
        }

        // Salir si llegamos al suelo y presionamos S
        if (controller.isGrounded && verticalInput < -0.5f)
        {
            StopClimbing();
        }
    }

    // **NUEVO MÉTODO: Salto automático desde la cima**
    private void JumpFromTop()
    {
        if (!isClimbing) return;

        isClimbing = false;
        currentLadder = null;

        // **SALTO FUERTE HACIA ARRIBA** - APLICAR MULTIPLICADOR DE SALTO
        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;
        velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * 1.5f * -2f * gravity); // 1.5x más fuerte

        // **PEQUEÑO IMPULSO HACIA ADELANTE** para alejarse de la escalera
        Vector3 forwardImpulse = -ladderForward * 2f;
        forwardImpulse.y = 0;

        // Aplicar el movimiento
        controller.Move(forwardImpulse * Time.deltaTime);

        Debug.Log($"🚀 SALTO desde cima! Velocidad Y: {velocity.y} (multiplicador salto: {jumpMultiplier}x)");

        // Aplicar gravedad normal inmediatamente
        ApplyGravity();
    }

    // Aplicar gravedad durante escalada cuando no se presionan teclas
    private void ApplyGravityDuringClimb()
    {
        velocity.y += gravity * Time.deltaTime * 0.5f; // Gravedad reducida durante escalada

        // Aplicar movimiento de caída
        Vector3 fallMotion = Vector3.up * velocity.y;
        controller.Move(fallMotion * Time.deltaTime);
    }

    // Aplicar gravedad normal
    private void ApplyGravity()
    {
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    private void MaintainLadderPosition()
    {
        if (currentLadder != null)
        {
            float horizontalDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(ladderSnapPosition.x, 0, ladderSnapPosition.z)
            );

            if (horizontalDistance > 0.1f)
            {
                Vector3 correctedPosition = new Vector3(
                    ladderSnapPosition.x,
                    transform.position.y,
                    ladderSnapPosition.z
                );

                transform.position = Vector3.Lerp(transform.position, correctedPosition, Time.deltaTime * 5f);
            }
        }
    }

    private void HandleNormalMovement(Vector3 planarMove, bool isGrounded)
    {
        // OBTENER MULTIPLICADORES ACTUALES
        float speedMultiplier = cookingSystem != null ? cookingSystem.SpeedMultiplier : 1f;
        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;

        // APLICAR MULTIPLICADOR DE VELOCIDAD (ya aplicado en planarMove)
        Vector3 adjustedPlanarMove = planarMove; // Ya tiene el multiplicador aplicado

        // Gravedad y salto
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * gravity);
            Debug.Log($"🦘 Salto ejecutado - Fuerza: {velocity.y} (multiplicador: {jumpMultiplier}x)");
        }

        // Aplicar gravedad normal
        ApplyGravity();

        // Daño por caída
        HandleFallDamage(isGrounded);

        // Movimiento final
        Vector3 finalPlanar = isGrounded ? adjustedPlanarMove : adjustedPlanarMove * airControl;
        Vector3 motion = finalPlanar + Vector3.up * velocity.y;
        controller.Move(motion * Time.deltaTime);

        // DEBUG: Mostrar estado actual (opcional, quitar si no quieres spam)
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"🏃‍♂️ ESTADO ACTUAL - Velocidad: x{speedMultiplier}, Salto: x{jumpMultiplier}, Daño: x{cookingSystem?.DamageMultiplier ?? 1f}");
        }
    }

    private void HandleFallDamage(bool isGrounded)
    {
        if (!isGrounded)
        {
            if (velocity.y < peakFallSpeed) peakFallSpeed = velocity.y;
        }

        if (isGrounded && !wasGroundedLastFrame)
        {
            float impactSpeed = Mathf.Abs(peakFallSpeed);
            if (impactSpeed > safeFallSpeed)
            {
                float extra = impactSpeed - safeFallSpeed;
                int dmg = Mathf.RoundToInt(extra * damagePerMS);

                // APLICAR MULTIPLICADOR DE DAÑO DEL SISTEMA DE COCCIÓN
                float damageMultiplier = cookingSystem != null ? cookingSystem.DamageMultiplier : 1f;
                int finalDamage = Mathf.RoundToInt(dmg * damageMultiplier);

                health.ApplyDamage(finalDamage);
                Debug.Log($"Daño por caída: {dmg} -> {finalDamage} (velocidad: {impactSpeed}m/s, multiplicador: {damageMultiplier}x)");
            }
            peakFallSpeed = 0f;
        }
    }

    // Debug visual
    void OnDrawGizmosSelected()
    {
        // Raycast de detección
        Gizmos.color = isClimbing ? Color.green : Color.blue;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawRay(rayOrigin, transform.forward * ladderDetectionRange);

        // Posición de snap (solo cuando escalando)
        if (isClimbing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ladderSnapPosition, 0.2f);
            Gizmos.DrawLine(transform.position, ladderSnapPosition);

            // **MOSTRAR LA CIMA DE LA ESCALERA**
            Gizmos.color = Color.yellow;
            Vector3 topPosition = new Vector3(transform.position.x, ladderTopY, transform.position.z);
            Gizmos.DrawWireCube(topPosition, new Vector3(1f, 0.1f, 1f));
            Gizmos.DrawLine(transform.position, topPosition);
        }
    }
    public void CollectKey(Key key)
    {
        if (!collectedKeys.Contains(key.keyID))
        {
            collectedKeys.Add(key.keyID);
            Debug.Log($"🗝️ Llave '{key.keyID}' obtenida! Llaves totales: {collectedKeys.Count}");

            // Efecto visual/auditivo opcional
            // Puedes agregar UI feedback aquí
        }
    }

    public bool HasKey(string keyID)
    {
        return collectedKeys.Contains(keyID);
    }

    public bool HasAllKeys(string[] requiredKeys)
    {
        foreach (string keyID in requiredKeys)
        {
            if (!collectedKeys.Contains(keyID))
                return false;
        }
        return true;
    }

    public string GetMissingKeys(string[] requiredKeys)
    {
        List<string> missing = new List<string>();
        foreach (string keyID in requiredKeys)
        {
            if (!collectedKeys.Contains(keyID))
                missing.Add(keyID);
        }
        return string.Join(", ", missing);
    }

    // Método para debug
    private void PrintKeys()
    {
        Debug.Log($"🔑 Llaves recolectadas ({collectedKeys.Count}): {string.Join(", ", collectedKeys)}");
    }
}