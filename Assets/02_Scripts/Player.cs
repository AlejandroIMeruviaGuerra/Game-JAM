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

    [Header("Rotación")]
    public float turnSpeed = 12f;
    public bool rotateModelOnly = true;

    [Header("Salto & Gravedad")]
    public float jumpHeight = 1.8f;
    public float gravity = -20f;

    [Header("Escalada Avanzada")]
    public float climbSpeed = 3.5f;
    public string ladderTag = "Ladder";
    public LayerMask ladderLayer = 1;
    public float ladderDetectionRange = 2f;
    public float ladderSnapDistance = 0.3f;
    public float topDetectionHeight = 0.5f;

    [Header("Daño por caída")]
    public float safeFallSpeed = 10f;
    public float damagePerMS = 2.0f;

    // Referencias
    private CharacterController controller;
    private PlayerHealth health;
    private PlayerCookingSystem cookingSystem;

    // Animator
    private Animator anim;
    private float speedSmoothed;
    private float lastVerticalClimbInput;
    private bool firedJumpThisFrame;

    // Rotación
    private Transform model;

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

        anim = GetComponentInChildren<Animator>();
        model = (rotateModelOnly && anim) ? anim.transform : this.transform;

        Debug.Log("Player inicializado. Buscando escaleras con tag: " + ladderTag);
    }

    void Update()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        // Dirección en plano desde la cámara
        Vector3 camF = ThirdPersonCameraController.PlanarForward;
        Vector3 camR = ThirdPersonCameraController.PlanarRight;
        Vector3 planarDir = (camR * inputX + camF * inputZ);
        if (planarDir.sqrMagnitude > 0.0001f) planarDir.Normalize();

        // Girar hacia la dirección de movimiento
        if (!isClimbing)
        {
            Vector3 lookDir = planarDir; lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                model.rotation = Quaternion.Slerp(model.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        float speedMultiplier = cookingSystem != null ? cookingSystem.SpeedMultiplier : 1f;
        Vector3 planarMove = planarDir * moveSpeed * speedMultiplier;
        bool isGrounded = controller.isGrounded;

        firedJumpThisFrame = false;

        // Detección de escaleras
        CheckForLadders();

        // Escalada
        if (isClimbing)
        {
            HandleClimbing(inputX, inputZ);
            if (!IsClimbingInputPressed()) ApplyGravityDuringClimb();
            PushAnimator(isGrounded, Vector3.zero);
            return;
        }

        // Movimiento normal
        HandleNormalMovement(planarMove, isGrounded);
        PushAnimator(isGrounded, planarMove);
        wasGroundedLastFrame = isGrounded;
    }

    // ================== ESCALERAS ==================
    private void CheckForLadders()
    {
        if (!isClimbing)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

            bool ladderDetected = Physics.Raycast(rayOrigin, transform.forward, out hit, ladderDetectionRange, ladderLayer);
            if (ladderDetected && hit.collider.CompareTag(ladderTag))
            {
                if (IsClimbingInputPressed()) StartClimbing(hit);
            }
        }
        else CheckLadderProximity();
    }

    private bool IsClimbingInputPressed()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    private void StartClimbing(RaycastHit ladderHit)
    {
        isClimbing = true;
        currentLadder = ladderHit.collider.transform;
        velocity.y = 0f;
        if (anim) anim.SetBool("Climbing", true);

        CalculateLadderSnapPosition(ladderHit);
        CalculateLadderTop(ladderHit);

        Vector3 face = -ladderHit.normal; face.y = 0f;
        if (face.sqrMagnitude > 0.0001f)
            model.rotation = Quaternion.LookRotation(face, Vector3.up);
    }

    private void CalculateLadderSnapPosition(RaycastHit hit)
    {
        ladderForward = hit.normal;
        ladderSnapPosition = hit.point - ladderForward * ladderSnapDistance;
        ladderSnapPosition.y = transform.position.y;
        StartCoroutine(SmoothSnapToLadder());
    }

    private void CalculateLadderTop(RaycastHit hit)
    {
        Collider ladderCollider = hit.collider;
        Bounds bounds = ladderCollider.bounds;
        ladderTopY = bounds.max.y - topDetectionHeight;
    }

    private IEnumerator SmoothSnapToLadder()
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
        if (currentLadder == null) { StopClimbing(); return; }

        float distanceToLadder = Vector3.Distance(transform.position, ladderSnapPosition);
        if (distanceToLadder > ladderDetectionRange * 1.2f) StopClimbing();
    }

    private void StopClimbing()
    {
        isClimbing = false;
        currentLadder = null;
        if (anim) anim.SetBool("Climbing", false);
        ApplyGravity();

        if (Input.GetButtonDown("Jump"))
        {
            float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;
            velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * gravity);
            FireJumpTrigger();
        }
    }

    private void HandleClimbing(float inputX, float inputZ)
    {
        Vector3 climbMovement = Vector3.zero;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) verticalInput = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) verticalInput = -1f;

        lastVerticalClimbInput = Mathf.Abs(verticalInput);
        climbMovement.y = verticalInput * climbSpeed;

        if (transform.position.y >= ladderTopY && verticalInput > 0)
        {
            JumpFromTop();
            return;
        }

        if (Mathf.Abs(inputX) > 0.1f)
            climbMovement += transform.right * inputX * climbSpeed * 0.1f;

        MaintainLadderPosition();
        controller.Move(climbMovement * Time.deltaTime);

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.E)) StopClimbing();
        if (controller.isGrounded && verticalInput < -0.5f) StopClimbing();
    }

    private void JumpFromTop()
    {
        isClimbing = false;
        currentLadder = null;

        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;
        velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * 1.5f * -2f * gravity);

        Vector3 forwardImpulse = -ladderForward * 2f; forwardImpulse.y = 0;
        controller.Move(forwardImpulse * Time.deltaTime);

        FireJumpTrigger();
        ApplyGravity();
    }

    private void ApplyGravityDuringClimb()
    {
        velocity.y += gravity * Time.deltaTime * 0.5f;
        Vector3 fallMotion = Vector3.up * velocity.y;
        controller.Move(fallMotion * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (!controller.isGrounded) velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0f) velocity.y = -2f;
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
                Vector3 corrected = new Vector3(
                    ladderSnapPosition.x,
                    transform.position.y,
                    ladderSnapPosition.z
                );

                transform.position = Vector3.Lerp(transform.position, corrected, Time.deltaTime * 5f);
            }
        }
    }

    // ================== MOVIMIENTO BASE ==================
    private void HandleNormalMovement(Vector3 planarMove, bool isGrounded)
    {
        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;

        // Si está en el suelo, reinicia la componente vertical
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        // --- SALTO ---
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            // 🔹 Si el jugador está casi quieto, no heredar movimiento horizontal previo
            if (planarMove.sqrMagnitude < 0.01f)
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }

            velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * gravity);
            FireJumpTrigger();
        }

        // --- GRAVEDAD ---
        ApplyGravity();
        HandleFallDamage(isGrounded);

        // 🔹 Si no hay input horizontal, no aplicar arrastre lateral en el aire
        Vector3 finalPlanar = Vector3.zero;
        if (planarMove.sqrMagnitude > 0.001f)
            finalPlanar = isGrounded ? planarMove : planarMove * airControl;

        Vector3 motion = finalPlanar + Vector3.up * velocity.y;
        controller.Move(motion * Time.deltaTime);
    }


    // ================== ANIMATOR ==================
    private void PushAnimator(bool grounded, Vector3 planarMove)
    {
        if (!anim) return;

        Vector3 hv = controller.velocity; hv.y = 0f;
        float planarSpeed = hv.magnitude;
        if (planarSpeed < 0.1f) planarSpeed = 0f;

        float maxPlanar = moveSpeed * (cookingSystem ? cookingSystem.SpeedMultiplier : 1f);
        float normalized = maxPlanar > 0.01f ? Mathf.Clamp01(planarSpeed / maxPlanar) : 0f;

        speedSmoothed = Mathf.Lerp(speedSmoothed, normalized, 12f * Time.deltaTime);

        // ✅ Detiene la animación de caminar si está en el aire sin moverse
        if (!grounded && controller.velocity.magnitude < 0.2f)
            anim.SetFloat("Speed", 0f);
        else
            anim.SetFloat("Speed", speedSmoothed);

        anim.SetFloat("YVel", velocity.y);
        anim.SetBool("Grounded", grounded);
        anim.SetBool("Climbing", isClimbing);
        anim.SetFloat("ClimbAbs", lastVerticalClimbInput);

        if (grounded && !wasGroundedLastFrame)
            anim.SetTrigger("Land");
    }

    private void FireJumpTrigger()
    {
        if (anim && !firedJumpThisFrame)
        {
            firedJumpThisFrame = true;
            anim.SetTrigger("Jump");
        }
    }

    // ================== DAÑO POR CAÍDA ==================
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
                float damageMultiplier = cookingSystem != null ? cookingSystem.DamageMultiplier : 1f;
                int finalDamage = Mathf.RoundToInt(dmg * damageMultiplier);
                health.ApplyDamage(finalDamage);
            }
            peakFallSpeed = 0f;
        }
    }

    // ================== DEBUG ==================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isClimbing ? Color.green : Color.blue;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawRay(rayOrigin, transform.forward * ladderDetectionRange);

        if (isClimbing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ladderSnapPosition, 0.2f);
            Gizmos.DrawLine(transform.position, ladderSnapPosition);
            Gizmos.color = Color.yellow;
            Vector3 topPosition = new Vector3(transform.position.x, ladderTopY, transform.position.z);
            Gizmos.DrawWireCube(topPosition, new Vector3(1f, 0.1f, 1f));
        }
    }

    // ================== SISTEMA DE LLAVES ==================
    public void CollectKey(Key key)
    {
        if (!collectedKeys.Contains(key.keyID))
        {
            collectedKeys.Add(key.keyID);
            Debug.Log($"🗝️ Llave '{key.keyID}' obtenida! Total: {collectedKeys.Count}");
        }
    }

    public bool HasKey(string keyID) => collectedKeys.Contains(keyID);

    public bool HasAllKeys(string[] requiredKeys)
    {
        foreach (string keyID in requiredKeys)
            if (!collectedKeys.Contains(keyID)) return false;
        return true;
    }

    public string GetMissingKeys(string[] requiredKeys)
    {
        List<string> missing = new List<string>();
        foreach (string keyID in requiredKeys)
            if (!collectedKeys.Contains(keyID)) missing.Add(keyID);
        return string.Join(", ", missing);
    }
}
