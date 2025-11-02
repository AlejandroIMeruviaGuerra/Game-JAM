using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI del Diálogo")]
    public Canvas dialogueCanvas;
    public TMP_Text npcNameText;
    public TMP_Text dialogueText;

    [Header("Animación")]
    public float typingSpeed = 0.03f;
    public float fadeDuration = 0.4f;
    public AudioSource audioSource;
    public AudioClip typingSound;

    private ThirdPersonCameraController cameraController;
    private Player player;
    private Coroutine typingCoroutine;
    private CanvasGroup canvasGroup;

    private bool isTyping = false;
    private bool dialogueActive = false; // 👈 indica si ya se mostró el diálogo al menos una vez
    private string currentText = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        cameraController = FindObjectOfType<ThirdPersonCameraController>();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
            canvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = dialogueCanvas.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.GetComponent<Player>();
    }

    // Mostrar diálogo (solo fade-in al inicio de la conversación)
    public void ShowDialogue(string npcName, string text)
    {
        if (dialogueCanvas == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueManager: Canvas o textos no asignados.");
            return;
        }

        StopAllCoroutines();
        dialogueCanvas.gameObject.SetActive(true);

        // 👇 Solo hacer fade-in si es la primera línea del diálogo
        if (!dialogueActive)
        {
            dialogueActive = true;
            StartCoroutine(FadeInCanvas());
        }
        else
        {
            canvasGroup.alpha = 1f; // mostrar de inmediato para siguientes líneas
        }

        // Bloquear movimiento y cámara
        if (player != null)
            player.SetMovementLock(true);
        if (cameraController != null)
        {
            cameraController.canRotate = false;
            cameraController.canZoom = false;
        }

        npcNameText.text = npcName;
        currentText = text;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;

            if (typingSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(typingSound, 0.6f);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private IEnumerator FadeInCanvas()
    {
        float t = 0f;
        canvasGroup.alpha = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void HideDialogue()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutCanvas());
    }

    private IEnumerator FadeOutCanvas()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        dialogueCanvas.gameObject.SetActive(false);
        dialogueActive = false; // 👈 reseteamos para que el siguiente diálogo vuelva a hacer fade-in

        // Restaurar movimiento y cámara
        if (player != null)
            player.SetMovementLock(false);
        if (cameraController != null)
        {
            cameraController.canRotate = true;
            cameraController.canZoom = true;
        }
    }
}
