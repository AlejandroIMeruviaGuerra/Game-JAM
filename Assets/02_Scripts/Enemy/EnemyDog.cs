using UnityEngine;

public class EnemyDog : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;          // jugador
    public LayerMask visionBlockers;  // capas que bloquean la vista

    [Header("Parámetros de Visión")]
    public float viewRadius = 10f;    // distancia máxima
    public float viewAngle = 90f;     // ángulo del cono
    public int rayCount = 7;          // cantidad de rayos en el cono

    [Header("Debug")]
    public bool playerVisible = false;

    void Start()
    {
        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        DetectPlayer();
        DrawVisionRays();
    }

    void DetectPlayer()
    {
        if (!player) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        // Dentro del rango y ángulo
        if (dist < viewRadius && angle < viewAngle / 2f)
        {
            // Raycast: si no hay bloqueador
            if (!Physics.Raycast(transform.position, dirToPlayer, dist, visionBlockers))
            {
                if (!playerVisible)
                {
                    Debug.Log("EnemyDog: ¡Jugador detectado!");
                }
                playerVisible = true;
                return;
            }
        }

        // Si llega aquí, significa que no lo ve
        if (playerVisible)
        {
            Debug.Log("EnemyDog: Jugador perdido.");
        }
        playerVisible = false;
    }


    void DrawVisionRays()
    {
        // Centro del enemigo
        Vector3 start = transform.position + Vector3.up * 0.5f;

        // Dibujar rayos distribuidos dentro del ángulo
        for (int i = 0; i < rayCount; i++)
        {
            float t = (float)i / (rayCount - 1);
            float angleOffset = Mathf.Lerp(-viewAngle / 2f, viewAngle / 2f, t);

            Vector3 dir = Quaternion.Euler(0, angleOffset, 0) * transform.forward;

            // Raycast para detectar obstáculos
            if (Physics.Raycast(start, dir, out RaycastHit hit, viewRadius, visionBlockers))
            {
                Debug.DrawLine(start, hit.point, Color.red); // obstáculo
            }
            else
            {
                Debug.DrawRay(start, dir * viewRadius, Color.green); // sin obstáculo
            }
        }

        // Dibuja el radio del cono en la Scene
        Debug.DrawRay(start, transform.forward * viewRadius, Color.yellow);
    }

    private void OnDrawGizmosSelected()
    {
        // Muestra el radio del cono de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRadius);
    }
}
