using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Radio : MonoBehaviour
{
    [Header("UI")]
    public GameObject promptUI;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] clips;
    private int currentClip = 0;

    [Header("Configuración")]
    public string playerTag = "Player";
    public KeyCode actionKey = KeyCode.F;

    private bool playerCerca = false;

    void Start()
    {
        // Asegura que el texto esté oculto al iniciar
        if (promptUI != null)
            promptUI.SetActive(false);

        // Busca AudioSource si no está asignado
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Configura el audio como global (2D)
        audioSource.spatialBlend = 0f; // 0 = 2D, se escucha en toda la escena
        audioSource.volume = 1f;

        // Asigna la primera canción si existe
        if (clips != null && clips.Length > 0)
            audioSource.clip = clips[currentClip];
    }

    void Update()
    {
        // Si el jugador está cerca y presiona F
        if (playerCerca && Input.GetKeyDown(actionKey))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else
            {
                CambiarCancion();
            }
        }
    }

    void CambiarCancion()
    {
        if (clips.Length == 0) return;

        currentClip = (currentClip + 1) % clips.Length;
        audioSource.clip = clips[currentClip];
        audioSource.Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Jugador cerca de la radio");
            playerCerca = true;

            if (promptUI != null)
                promptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Jugador se alejó de la radio ❌");
            playerCerca = false;

            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }
}
