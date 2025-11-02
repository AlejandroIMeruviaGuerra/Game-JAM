using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class NPCDialogueSimple : MonoBehaviour
{
    [Header("Datos del NPC")]
    public string npcName = "NPC";
    [TextArea(2, 6)]
    public string[] dialogueLines;

    [Header("Rangos")]
    public float interactionRange = 3f;     // Rango para hablar
    public float iconVisibleRange = 10f;    // Rango para ver iconos lejanos

    [Header("UI del NPC (Canvas World Space)")]
    public Canvas npcCanvas;

    [Header("Sprites principales (encima del icono de hablar)")]
    public Image farIconBefore;             // "!" antes de hablar
    public Image farIconAfter;              // "✅" después de hablar

    [Header("Icono de interacción (Presiona B)")]
    public Image iconImage;                 // Imagen del icono de hablar
    public TMP_Text iconText;               // Texto "Presiona B para hablar"

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private bool playerInRange = false;
    private bool isTalking = false;
    private bool hasTalked = false;
    private Transform player;
    private SphereCollider interactionCollider;
    private int currentLine = 0;

    // Para controlar las corrutinas de fade
    private Coroutine currentFarIconCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogWarning("NPCDialogueSimple: No se encontró el Player con Tag 'Player'.");

        interactionCollider = GetComponent<SphereCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.radius = interactionRange;

        if (npcCanvas != null)
            npcCanvas.gameObject.SetActive(true);

        // Iniciar invisibles
        SetAlpha(iconImage, 0f);
        SetAlpha(iconText, 0f);
        SetAlpha(farIconBefore, 0f);
        SetAlpha(farIconAfter, 0f);

        // Mostrar el ícono correcto al iniciar
        UpdateFarIcons();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Mostrar los íconos principales si está dentro del rango visible
        if (distance <= iconVisibleRange && !isTalking)
        {
            UpdateFarIcons();
        }
        else if (distance > iconVisibleRange && !isTalking)
        {
            FadeOutFarIcons();
        }

        // Detectar inicio de diálogo
        if (playerInRange && !isTalking && Input.GetKeyDown(KeyCode.B))
        {
            StartDialogue();
        }

        // Avanzar diálogo
        if (isTalking && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            NextDialogue();
        }
    }

    private void UpdateFarIcons()
    {
        if (!hasTalked)
        {
            // Mostrar farIconBefore y ocultar farIconAfter
            if (currentFarIconCoroutine != null)
                StopCoroutine(currentFarIconCoroutine);
            currentFarIconCoroutine = StartCoroutine(SwitchFarIcons(farIconBefore, farIconAfter));
        }
        else
        {
            // Mostrar farIconAfter y ocultar farIconBefore
            if (currentFarIconCoroutine != null)
                StopCoroutine(currentFarIconCoroutine);
            currentFarIconCoroutine = StartCoroutine(SwitchFarIcons(farIconAfter, farIconBefore));
        }
    }

    private IEnumerator SwitchFarIcons(Image showIcon, Image hideIcon)
    {
        // Fade out del icono a ocultar
        if (hideIcon != null && hideIcon.color.a > 0)
        {
            yield return StartCoroutine(FadeRoutine(hideIcon, 0f));
        }

        // Fade in del icono a mostrar
        if (showIcon != null && showIcon.color.a < 1)
        {
            yield return StartCoroutine(FadeRoutine(showIcon, 1f));
        }
    }

    private void FadeOutFarIcons()
    {
        if (currentFarIconCoroutine != null)
            StopCoroutine(currentFarIconCoroutine);

        StartCoroutine(FadeOutBothFarIcons());
    }

    private IEnumerator FadeOutBothFarIcons()
    {
        if (farIconBefore != null && farIconBefore.color.a > 0)
        {
            yield return StartCoroutine(FadeRoutine(farIconBefore, 0f));
        }

        if (farIconAfter != null && farIconAfter.color.a > 0)
        {
            yield return StartCoroutine(FadeRoutine(farIconAfter, 0f));
        }
    }

    private void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        isTalking = true;
        currentLine = 0;

        // Mostrar el primer diálogo
        DialogueManager.Instance?.ShowDialogue(npcName, dialogueLines[currentLine]);

        // Ocultar todos los iconos al iniciar diálogo
        HideAllIcons();
    }

    private void NextDialogue()
    {
        currentLine++;
        if (currentLine < dialogueLines.Length)
        {
            // Mostrar siguiente línea
            DialogueManager.Instance?.ShowDialogue(npcName, dialogueLines[currentLine]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isTalking = false;
        currentLine = 0;
        hasTalked = true;

        DialogueManager.Instance?.HideDialogue();

        // Actualizar iconos lejanos
        UpdateFarIcons();

        // ✅ CORREGIDO: Si el jugador sigue en rango, mantener los iconos de interacción visibles
        if (playerInRange)
        {
            FadeIn(iconImage);
            FadeIn(iconText);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;

        // ✅ CORREGIDO: Mostrar "[B] Hablar" siempre que el jugador esté en rango, sin importar si ya habló
        if (!isTalking)
        {
            FadeIn(iconImage);
            FadeIn(iconText);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        // Ocultar iconos de interacción
        FadeOut(iconImage);
        FadeOut(iconText);

        // Si no está en diálogo, actualizar iconos lejanos
        if (!isTalking)
        {
            float distance = Vector3.Distance(player.position, transform.position);
            if (distance > iconVisibleRange)
            {
                FadeOutFarIcons();
            }
        }
    }

    private void HideAllIcons()
    {
        // Ocultar iconos de interacción
        FadeOut(iconImage);
        FadeOut(iconText);

        // Ocultar iconos lejanos
        FadeOutFarIcons();
    }

    // 🔹 Fades suaves
    private void FadeIn(Graphic uiElement)
    {
        if (uiElement == null) return;
        StopAllCoroutinesForElement(uiElement);
        StartCoroutine(FadeRoutine(uiElement, 1f));
    }

    private void FadeOut(Graphic uiElement)
    {
        if (uiElement == null) return;
        StopAllCoroutinesForElement(uiElement);
        StartCoroutine(FadeRoutine(uiElement, 0f));
    }

    private void StopAllCoroutinesForElement(Graphic uiElement)
    {
        // No podemos detener una corrutina específica fácilmente,
        // así que manejamos esto con flags en la corrutina
    }

    private IEnumerator FadeRoutine(Graphic uiElement, float targetAlpha)
    {
        if (uiElement == null) yield break;

        float startAlpha = uiElement.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(uiElement, newAlpha);
            yield return null;
        }
        SetAlpha(uiElement, targetAlpha);
    }

    private void SetAlpha(Graphic uiElement, float alpha)
    {
        if (uiElement == null) return;
        Color c = uiElement.color;
        c.a = alpha;
        uiElement.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, iconVisibleRange);
    }
}