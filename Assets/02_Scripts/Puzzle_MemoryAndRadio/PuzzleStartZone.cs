using UnityEngine;
using TMPro;

public class PuzzleStartZone : MonoBehaviour
{
    [Header("UI de mensaje")]
    public TextMeshProUGUI promptText; // Texto: "Pulsa F para empezar a jugar"

    [Header("Entrada")]
    public KeyCode startKey = KeyCode.F;

    bool playerInside = false;

    void Start()
    {
        if (promptText)
        {
            promptText.text = "Pulsa F para empezar a jugar";
            promptText.gameObject.SetActive(false); // Ocultamos el texto al inicio
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;

        if (promptText)
            promptText.gameObject.SetActive(true); // Mostramos el texto
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;

        if (promptText)
            promptText.gameObject.SetActive(false); // Ocultamos el texto
    }

    void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(startKey))
        {
            if (PuzzleSequenceManager.Instance != null)
                PuzzleSequenceManager.Instance.StartGame();

            if (promptText)
                promptText.gameObject.SetActive(false);
        }
    }
}
