using UnityEngine;

/// <summary>
/// Third-person camera orbit + follow + zoom + collision avoiding + ajuste al retroceder.
/// </summary>
[DefaultExecutionOrder(100)]
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target / Follow")]
    public Transform target;
    public Vector3 targetOffset = Vector3.zero;
    [Range(0.0f, 1.0f)] public float followSmooth = 0.15f;

    [Header("Orbit (Mouse)")]
    public float sensX = 180f;
    public float sensY = 180f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public bool invertY = false;
    public bool rightMouseToRotate = false;

    [Header("Zoom")]
    public float minDistance = 1.0f;
    public float maxDistance = 6.0f;
    public float zoomSpeed = 4.0f;
    public float zoomSmooth = 0.15f;
    public float cameraRadius = 0.25f;

    [Header("Colisión con el entorno")]
    public LayerMask collisionLayers = ~0;
    public float collisionBuffer = 0.1f;

    [Header("Cursor")]
    public bool lockCursor = true;
    public CursorLockMode cursorLockMode = CursorLockMode.Locked;

    // === NUEVO: Ejes planares accesibles desde Player ===
    public static Vector3 PlanarForward { get; private set; }
    public static Vector3 PlanarRight { get; private set; }

    // Estado interno
    float yaw;
    float pitch;
    float desiredDistance;
    float currentDistance;
    Vector3 currentFollowPos;
    Vector3 followVel;

    // === NUEVO: suavizado adicional para colisiones ===
    float collisionDistanceSmoothVel;

    void Start()
    {
        if (target == null)
            Debug.LogWarning("[ThirdPersonCameraController] Asigna un Target (pivot del jugador).");

        desiredDistance = Mathf.Clamp((minDistance + maxDistance) * 0.5f, minDistance, maxDistance);
        currentDistance = desiredDistance;
        currentFollowPos = (target != null) ? target.position + targetOffset : transform.position;

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = ClampPitch(e.x);

        if (lockCursor) Cursor.lockState = cursorLockMode;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (lockCursor && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = cursorLockMode;
            Cursor.visible = false;
        }

        HandleOrbitInput();
        HandleZoomInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + targetOffset;
        currentFollowPos = Vector3.SmoothDamp(currentFollowPos, targetPos, ref followVel, followSmooth);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Suavizado de distancia de zoom
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance,
            1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, zoomSmooth)));

        // Resuelve colisión
        float finalDistance = ResolveCollision(currentFollowPos, rot, currentDistance);

        // === NUEVO: ajuste dinámico cuando el jugador camina hacia la cámara ===
        // Si el jugador se acerca demasiado, alejamos la cámara suavemente
        float playerSpeedZ = Input.GetAxis("Vertical");
        if (playerSpeedZ < -0.1f) // presionando "S"
        {
            finalDistance = Mathf.Lerp(finalDistance, maxDistance * 1.1f, Time.deltaTime * 2f);
        }

        // Interpolación suave para evitar saltos
        float smoothDist = Mathf.SmoothDamp(currentDistance, finalDistance, ref collisionDistanceSmoothVel, 0.05f);

        Vector3 camPos = currentFollowPos - rot * Vector3.forward * smoothDist;
        transform.SetPositionAndRotation(camPos, rot);

        // === Actualizar ejes planares ===
        Vector3 fwd = rot * Vector3.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = rot * Vector3.right; rgt.y = 0f; rgt.Normalize();
        PlanarForward = fwd;
        PlanarRight = rgt;

        currentDistance = smoothDist; // mantener coherencia
    }

    void HandleOrbitInput()
    {
        bool allowRotate = !(rightMouseToRotate && !Input.GetMouseButton(1));
        if (!allowRotate) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * sensX * Time.deltaTime;
        float ySign = invertY ? 1f : -1f;
        pitch += mouseY * sensY * Time.deltaTime * ySign;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            desiredDistance -= scroll * zoomSpeed;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }
    }

    float ResolveCollision(Vector3 pivot, Quaternion rot, float desiredDist)
    {
        Vector3 desiredCamPos = pivot - rot * Vector3.forward * desiredDist;
        Vector3 dir = (desiredCamPos - pivot).normalized;
        float dist = desiredDist;

        if (Physics.SphereCast(pivot, cameraRadius, dir, out RaycastHit hit,
            desiredDist + collisionBuffer, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            dist = Mathf.Max(minDistance, hit.distance - collisionBuffer);
        }

        return dist;
    }

    float ClampPitch(float rawX)
    {
        float x = rawX;
        if (x > 180f) x -= 360f;
        return Mathf.Clamp(x, minPitch, maxPitch);
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position + targetOffset, 0.05f);
        }
    }
}
