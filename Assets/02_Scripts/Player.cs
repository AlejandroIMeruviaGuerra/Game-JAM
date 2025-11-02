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
    public float climbJumpBoost = 2.5f; // NUEVO: impulso al saltar mientras sube
    public string ladderTag = "Ladder";
    public LayerMask ladderLayer = 1;
    public float ladderDetectionRange = 2f;
    public float ladderSnapDistance = 0.3f;
    public float topDetectionHeight = 0.5f;

    [Tooltip("Qué tanto debes mirar la escalera para permitir entrar (1 = de frente)")]
    [Range(0f, 1f)] public float ladderFacingDot = 0.7f;
    [Tooltip("Radio del spherecast para detectar escalera")]
    public float ladderDetectRadius = 0.25f;

    [Tooltip("Velocidad de descenso automática al soltar W manteniéndose en la escalera")]
    public float autoDescendSpeed = 2.5f;

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

        Vector3 camF = ThirdPersonCameraController.PlanarForward;
        Vector3 camR = ThirdPersonCameraController.PlanarRight;
        Vector3 planarDir = (camR * inputX + camF * inputZ);
        if (planarDir.sqrMagnitude > 0.0001f) planarDir.Normalize();

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

        CheckForLadders();

        if (isClimbing)
        {
            HandleClimbing(inputX, inputZ);
            if (!IsClimbingInputPressed()) AutoDescendDuringClimb();
            PushAnimator(isGrounded, Vector3.zero);
            return;
        }

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
            Vector3 rayOrigin = transform.position + Vector3.up * 0.9f;

            bool ladderDetected = Physics.SphereCast(
                rayOrigin, ladderDetectRadius, transform.forward, out hit,
                ladderDetectionRange, ladderLayer, QueryTriggerInteraction.Collide
            );

            if (ladderDetected && hit.collider.CompareTag(ladderTag))
            {
                Vector3 faceDir = -hit.normal; faceDir.y = 0f;
                faceDir.Normalize();
                float facing = Vector3.Dot(transform.forward, faceDir);

                if (facing >= ladderFacingDot && IsClimbingInputPressed())
                    StartClimbing(hit);
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
        velocity = Vector3.zero;
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
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 target = new Vector3(ladderSnapPosition.x, transform.position.y, ladderSnapPosition.z);
            transform.position = Vector3.Lerp(startPosition, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = new Vector3(ladderSnapPosition.x, transform.position.y, ladderSnapPosition.z);
    }

    private void CheckLadderProximity()
    {
        if (currentLadder == null) { StopClimbing(false); return; }

        float distanceToLadder = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(ladderSnapPosition.x, 0, ladderSnapPosition.z)
        );

        if (distanceToLadder > ladderDetectionRange * 0.8f)
            StopClimbing(true);
    }

    private void StopClimbing(bool pushBack)
    {
        if (!isClimbing) return;
        isClimbing = false;
        currentLadder = null;
        if (anim) anim.SetBool("Climbing", false);
        ApplyGravity();

        if (pushBack)
        {
            Vector3 away = ladderForward; away.y = 0f;
            controller.Move(away.normalized * 0.1f);
        }
    }

    private void HandleClimbing(float inputX, float inputZ)
    {
        velocity.x = 0f; velocity.z = 0f;

        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) verticalInput = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) verticalInput = -1f;

        lastVerticalClimbInput = Mathf.Abs(verticalInput);

        Vector3 climbMovement = Vector3.up * (verticalInput * climbSpeed);

        // 🚀 NUEVO: impulso al saltar mientras sube
        if (Input.GetButtonDown("Jump"))
        {
            climbMovement.y += climbJumpBoost;
            if (anim) anim.SetTrigger("Jump");
        }

        if (transform.position.y >= ladderTopY && verticalInput > 0f)
        {
            JumpFromTop();
            return;
        }

        if (Mathf.Abs(inputX) > 0.1f)
        {
            Vector3 side = transform.right * inputX * climbSpeed * 0.05f;
            climbMovement += new Vector3(side.x, 0f, side.z);
        }

        MaintainLadderPosition();
        controller.Move(climbMovement * Time.deltaTime);

        if (controller.isGrounded && verticalInput < -0.5f) StopClimbing(false);
    }

    private void JumpFromTop()
    {
        isClimbing = false;
        currentLadder = null;

        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;
        velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * 1.5f * -2f * gravity);

        Vector3 back = -ladderForward; back.y = 0;
        controller.Move(back.normalized * 0.15f);

        FireJumpTrigger();
        ApplyGravity();
    }

    private void AutoDescendDuringClimb()
    {
        float descent = -autoDescendSpeed;
        Vector3 fall = Vector3.up * descent;
        controller.Move(fall * Time.deltaTime);

        if (controller.isGrounded) StopClimbing(false);
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
            Vector3 corrected = new Vector3(ladderSnapPosition.x, transform.position.y, ladderSnapPosition.z);
            transform.position = Vector3.Lerp(transform.position, corrected, Time.deltaTime * 8f);
        }
    }

    // ================== MOVIMIENTO BASE ==================
    private void HandleNormalMovement(Vector3 planarMove, bool isGrounded)
    {
        float jumpMultiplier = cookingSystem != null ? cookingSystem.JumpMultiplier : 1f;

        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            if (planarMove.sqrMagnitude < 0.01f)
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }

            velocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * -2f * gravity);
            FireJumpTrigger();
        }

        ApplyGravity();
        HandleFallDamage(isGrounded);

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

        if (!grounded && controller.velocity.magnitude < 0.2f)
            anim.SetFloat("Speed", 0f);
        else
            anim.SetFloat("Speed", speedSmoothed);

        //anim.SetFloat("YVel", velocity.y);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isClimbing ? Color.green : Color.blue;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.9f;
        Gizmos.DrawWireSphere(rayOrigin + transform.forward * Mathf.Min(ladderDetectionRange, 0.2f), ladderDetectRadius);
        Gizmos.DrawRay(rayOrigin, transform.forward * ladderDetectionRange);

        if (isClimbing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ladderSnapPosition, 0.15f);
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
