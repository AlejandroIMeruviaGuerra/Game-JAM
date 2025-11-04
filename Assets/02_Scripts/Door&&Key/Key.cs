using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Configuración de Llave")]
    public string keyID = "puerta_1"; // ID único para identificar qué puerta abre
    public Color keyColor = Color.yellow; // Color para identificar visualmente

    [Header("Efectos")]
    public GameObject collectEffect;
    public AudioClip collectSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.CollectKey(this);

                // Efectos al recoger
                if (collectEffect != null)
                    Instantiate(collectEffect, transform.position, Quaternion.identity);

                if (collectSound != null)
                    AudioSource.PlayClipAtPoint(collectSound, transform.position);

                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = keyColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.7f, "Key Gizmo");
    }
}