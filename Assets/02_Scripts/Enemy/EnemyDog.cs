using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyDog : MonoBehaviour
{
    public enum State { Patrol, Chase, Search }

    [Header("Referencias")]
    public Transform player;
    public Transform[] patrolPoints;
    public LayerMask visionBlockers;
    public LayerMask groundMask;
    public float viewRadius = 10f;
    public float viewAngle = 90f;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float turnSpeed = 5f;
    public float obstacleAvoidanceDistance = 1.5f;
    public float proximityDetectionRadius = 3f;
    public float detectionTime = 0.5f;
    public float forgetTime = 1.5f;
    public float searchDuration = 4f;
    public float stuckResetTime = 3f;

    [Header("Giro de búsqueda (ajustable en Inspector)")]
    [Range(0.2f, 3f)] public float searchTurnSpeed = 1.2f;
    [Range(10f, 80f)] public float searchTurnAngle = 40f;
    [Range(1f, 10f)] public float searchTurnSmooth = 3f;

    private int patrolIndex = 0;
    private Vector3 lastSeenPosition;
    private bool playerVisible = false;
    private float seeTimer = 0f;
    private float loseTimer = 0f;
    private float searchTimer = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private State state = State.Patrol;
    private Rigidbody rb;

    // Control del giro al buscar
    private Quaternion searchBaseRotation;
    private bool baseRotationSet = false;

    // Prevención de loops contra paredes
    private float wallAvoidTimer = 0f;
    private const float wallAvoidDuration = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        DetectPlayer();

        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Search:
                Search();
                break;
        }

        KeepGrounded();
        CheckIfStuck();
    }

    // =======================
    // === ESTADOS ===========
    // =======================
    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Vector3 target = patrolPoints[patrolIndex].position;
        MoveTowards(target);

        if (Vector3.Distance(transform.position, target) < 0.6f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        if (playerVisible)
        {
            seeTimer += Time.deltaTime;
            if (seeTimer >= detectionTime)
            {
                state = State.Chase;
                Debug.Log("EnemyDog: Persiguiendo al jugador!");
            }
        }
        else seeTimer = 0f;
    }

    void Chase()
    {
        if (player)
        {
            MoveTowards(player.position);
            lastSeenPosition = player.position;
        }

        if (playerVisible)
        {
            loseTimer = 0f;
        }
        else
        {
            loseTimer += Time.deltaTime;
            if (loseTimer >= forgetTime)
            {
                Debug.Log("EnemyDog: Perdió al jugador — iniciando búsqueda.");
                state = State.Search;
                searchTimer = 0f;
                baseRotationSet = false;
            }
        }
    }

    void Search()
    {
        float dist = Vector3.Distance(transform.position, lastSeenPosition);
        Vector3 origin = transform.position + Vector3.up * 0.4f;

        // === Detección de pared cercana ===
        bool wallFront = Physics.Raycast(origin, transform.forward, 0.8f, visionBlockers);
        bool wallLeft = Physics.Raycast(origin, -transform.right, 0.6f, visionBlockers);
        bool wallRight = Physics.Raycast(origin, transform.right, 0.6f, visionBlockers);

        if (wallFront)
        {
            wallAvoidTimer += Time.deltaTime;
            Vector3 turnDir = (!wallRight) ? transform.right : -transform.right;
            Quaternion avoidRot = Quaternion.LookRotation(turnDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, avoidRot, Time.deltaTime * 3f);

            if (wallAvoidTimer > wallAvoidDuration)
                wallAvoidTimer = 0f;

            Debug.DrawRay(origin, transform.forward * 0.6f, Color.red);
            Debug.DrawRay(origin, turnDir * 0.6f, Color.yellow);
            return; // evita quedarse trabado
        }

        // === Movimiento hasta el punto de búsqueda ===
        if (dist > 1f)
        {
            MoveTowards(lastSeenPosition);
            return;
        }

        // === Giro exploratorio natural ===
        searchTimer += Time.deltaTime;

        if (!baseRotationSet)
        {
            searchBaseRotation = transform.rotation;
            baseRotationSet = true;
        }

        float oscillation = Mathf.Sin(Time.time * searchTurnSpeed);
        float yawOffset = oscillation * searchTurnAngle;
        Quaternion offsetRot = Quaternion.Euler(0, yawOffset, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            searchBaseRotation * offsetRot,
            Time.deltaTime * searchTurnSmooth
        );

        // === Retomar persecución si lo ve ===
        if (playerVisible)
        {
            Debug.Log("EnemyDog: ¡Lo encontró de nuevo!");
            state = State.Chase;
            baseRotationSet = false;
            return;
        }

        // === Volver a patrullar si no lo encuentra ===
        if (searchTimer >= searchDuration)
        {
            Debug.Log("EnemyDog: No lo encontró, retomando patrulla.");
            state = State.Patrol;
            baseRotationSet = false;
        }
    }

    // =======================
    // === DETECCIÓN VISUAL ===
    // =======================
    void DetectPlayer()
    {
        if (!player) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (dist < proximityDetectionRadius)
        {
            bool blocked = Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, dist, visionBlockers);
            bool playerInFront = angle < 100f;

            if (!blocked && playerInFront)
            {
                RotateToFace(player.position);
                if (state != State.Chase)
                {
                    state = State.Chase;
                    Debug.Log("EnemyDog: Jugador muy cerca — persecución iniciada!");
                }
                playerVisible = true;
                return;
            }
        }

        if (dist < viewRadius && angle < viewAngle / 2f)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, dist, visionBlockers))
            {
                playerVisible = true;
                return;
            }
        }

        playerVisible = false;
    }

    // =======================
    // === MOVIMIENTO BASE ===
    // =======================
    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        AvoidObstacles(ref direction);
        RotateToFace(transform.position + direction);

        Vector3 move = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    void RotateToFace(Vector3 point)
    {
        Vector3 dir = (point - transform.position);
        dir.y = 0;
        if (dir.magnitude > 0.1f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * turnSpeed);
        }
    }

    // =======================
    // === EVITAR OBSTÁCULOS ==
    // =======================
    void AvoidObstacles(ref Vector3 moveDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float sideCheckDistance = 1.0f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hitFront, obstacleAvoidanceDistance))
        {
            if (!hitFront.collider.CompareTag("Player"))
            {
                Vector3 avoidDir = Vector3.Reflect(transform.forward, hitFront.normal);
                avoidDir.y = 0;
                moveDir = Vector3.Lerp(moveDir, avoidDir, 0.7f);
                Debug.DrawRay(origin, avoidDir * 2f, Color.red);
            }
        }

        bool hitLeft = Physics.Raycast(origin, -transform.right, sideCheckDistance);
        bool hitRight = Physics.Raycast(origin, transform.right, sideCheckDistance);

        if (hitLeft && !hitRight)
            moveDir = Vector3.Lerp(moveDir, transform.right, 0.4f);
        else if (hitRight && !hitLeft)
            moveDir = Vector3.Lerp(moveDir, -transform.right, 0.4f);
    }

    // =======================
    // === MANTENER ALTURA ===
    // =======================
    void KeepGrounded()
    {
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit groundHit, 3f, groundMask))
        {
            Vector3 pos = transform.position;
            pos.y = groundHit.point.y;
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 10f);
        }
    }

    // =======================
    // === DETECTAR ATASCO ===
    // =======================
    void CheckIfStuck()
    {
        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moved < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckResetTime)
            {
                Debug.Log("EnemyDog: Atascado — reseteando a patrulla.");
                state = State.Patrol;
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;
    }

    // =======================
    // === DEBUG VISUAL =======
    // =======================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, proximityDetectionRadius);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRadius);
    }
}
