using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour
{
    [Header("Configuración de Puerta")]
    public string requiredKeyID = "puerta_1";
    public bool isFinalDoor = false;
    [Tooltip("Listado de TODAS las llaves requeridas para la PUERTA FINAL. Debe tener al menos 1 elemento.")]
    public string[] allRequiredKeys;

    [Header("Animación")]
    public float openSpeed = 2f;
    public float openAngle = 90f;
    public Transform pivotPoint;

    [Header("Interacción")]
    [Tooltip("Distancia para poder interactuar (E).")]
    public float interactionDistance = 2.0f;
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Capa del Player para la detección por esfera (opcional).")]
    public LayerMask playerLayer = ~0; // por defecto, todas

    [Header("Efectos")]
    public AudioClip openSound;
    public AudioClip lockedSound;
    public GameObject lockedEffect;

    // Estado
    protected Vector3 closedRotation;
    protected Vector3 openRotation;
    protected bool isOpen = false;
    protected bool isMoving = false;

    // Componentes
    private Collider doorCollider;
    private Rigidbody doorRigidbody;

    // Cache de jugador cercano (solo informativo)
    private Player nearbyPlayer;

    void Start()
    {
        doorCollider = GetComponent<Collider>();
        closedRotation = transform.eulerAngles;
        openRotation = closedRotation + new Vector3(0, openAngle, 0);

        // Rigidbody kinematic para no caer
        doorRigidbody = GetComponent<Rigidbody>();
        if (doorRigidbody == null)
        {
            doorRigidbody = gameObject.AddComponent<Rigidbody>();
            doorRigidbody.isKinematic = true;
            doorRigidbody.useGravity = false;
        }

        // ¡Puerta sólida! (no trigger) mientras está cerrada
        if (doorCollider != null)
            doorCollider.isTrigger = false;
    }

    void Update()
    {
        // 1) Detectar jugador cercano sin usar triggers
        nearbyPlayer = FindNearbyPlayer();

        // 2) Interacción
        if (nearbyPlayer != null && Input.GetKeyDown(interactKey))
        {
            TryOpenDoor(nearbyPlayer);
        }
    }

    private Player FindNearbyPlayer()
    {
        // Detectar cualquier collider con tag Player en radio
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionDistance, playerLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Player"))
            {
                Player p = hits[i].GetComponent<Player>();
                if (p != null) return p;
            }
        }
        return null;
    }

    public void TryOpenDoor(Player player)
    {
        if (isOpen || isMoving) return;

        bool canOpen = CheckPlayerKeys(player);
        if (!canOpen)
        {
            DoorLockedFeedback(player);
            return;
        }

        StartCoroutine(OpenDoor());
        Debug.Log($"🔓 {(isFinalDoor ? "Puerta final" : "Puerta")} abierta!");
    }

    private bool CheckPlayerKeys(Player player)
    {
        if (isFinalDoor)
        {
            // Si no definiste llaves requeridas, NO abras nunca.
            if (allRequiredKeys == null || allRequiredKeys.Length == 0)
            {
                Debug.LogWarning("Puerta final sin 'allRequiredKeys' definidos. No se abrirá.");
                return false;
            }
            return player.HasAllKeys(allRequiredKeys);
        }
        else
        {
            // Para puerta normal, exige su llave específica
            if (string.IsNullOrEmpty(requiredKeyID))
            {
                Debug.LogWarning("Puerta normal sin 'requiredKeyID' definido. No se abrirá.");
                return false;
            }
            return player.HasKey(requiredKeyID);
        }
    }

    private void DoorLockedFeedback(Player player)
    {
        string msg;
        if (isFinalDoor)
        {
            string missing = (allRequiredKeys == null || allRequiredKeys.Length == 0)
                ? "(configura 'allRequiredKeys' en el inspector)"
                : player.GetMissingKeys(allRequiredKeys);
            msg = $"🔒 Puerta final bloqueada. Te faltan: {missing}";
        }
        else
        {
            msg = $"🔒 Puerta bloqueada. Necesitas la llave: {requiredKeyID}";
        }

        Debug.Log(msg);

        if (lockedSound != null)
            AudioSource.PlayClipAtPoint(lockedSound, transform.position);

        if (lockedEffect != null)
            Instantiate(lockedEffect, transform.position, Quaternion.identity);
    }

    protected virtual IEnumerator OpenDoor()
    {
        isMoving = true;

        // Permitir paso al abrir (desde ahora sí trigger)
        if (doorCollider != null)
            doorCollider.isTrigger = true;

        if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, transform.position);

        // Animación
        float t = 0f;
        Quaternion rotA = Quaternion.Euler(closedRotation);
        Quaternion rotB = Quaternion.Euler(openRotation);

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            if (pivotPoint != null)
            {
                transform.RotateAround(pivotPoint.position, Vector3.up, openAngle * Time.deltaTime * openSpeed);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(rotA, rotB, t);
            }
            yield return null;
        }

        transform.rotation = rotB;
        isOpen = true;
        isMoving = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Radio de interacción
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Estado
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            Gizmos.color = isOpen ? Color.green : (isMoving ? Color.yellow : Color.red);
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"{(isFinalDoor ? "FINAL" : requiredKeyID)}\n" +
                $"{(isOpen ? "ABIERTA" : isMoving ? "ABRIENDO" : "CERRADA")}" +
                $"{(nearbyPlayer != null ? "\n[E] Interactuar" : "")}");
        }
    }
#endif
}
