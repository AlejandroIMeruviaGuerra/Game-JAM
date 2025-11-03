using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(SphereCollider))]
public class NPCChallenge : MonoBehaviour
{
    [Header("UI Proximidad")]
    public Canvas promptCanvas;
    public Text promptText;
    [TextArea]
    public string mensaje = "¿Quieres jugar 3 en raya? Presiona N.\nSi ganas te daré la llave.";

    [Header("Minijuego")]
    public string ticTacToeSceneName = "Minigame_TicTacToe2D";

    [Header("Detección")]
    public string playerTag = "Player";
    public float reenablePlayerDelay = 0.5f;

    private bool playerInside = false;
    private GameObject player;
    private bool minigameActive = false;
    [Header("Pista al volver del minijuego")]
    public bool showHintOnReturn = true;
    public float hintAutoHideSeconds = 4f;
    [TextArea] public string defaultWinHint = "Me ganaste. La llave está encima del refrigerador.";

    void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;

        if (promptCanvas != null)
            promptCanvas.enabled = false;
        else
            Debug.LogError("Prompt Canvas no asignado en el Inspector");

        GameEvents.OnTTTActiveChanged += HandleMinigameActive;
    }

    void OnDestroy()
    {
        GameEvents.OnTTTActiveChanged -= HandleMinigameActive;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;
        player = other.gameObject;

        // Prioridad: si vienes de ganar, muestra la pista
        if (TTTTransfer.Won)
        {
            ShowWinHintIfAny();
        }
        else
        {
            if (promptText) promptText.text = mensaje; // el reto normal
            if (promptCanvas) promptCanvas.enabled = true;
        }

        Debug.Log("Jugador entró en zona del NPC");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        player = null;

        if (promptCanvas != null)
            promptCanvas.enabled = false;

        Debug.Log("Jugador salió de zona del NPC");
    }

    void Update()
    {
        if (!playerInside || minigameActive) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("Iniciando 3 en raya...");
            StartTicTacToe();
        }
    }

    void StartTicTacToe()
    {
        minigameActive = true;
        SetPlayerControl(false);

        // Verificar que la escena existe
        if (!SceneExists(ticTacToeSceneName))
        {
            Debug.LogError($"Escena no encontrada: {ticTacToeSceneName}. Añádela en Build Settings.");
            minigameActive = false;
            SetPlayerControl(true);
            return;
        }

        try
        {
            SceneManager.LoadSceneAsync(ticTacToeSceneName, LoadSceneMode.Single).completed += _ =>
            {
                Debug.Log("Escena de 3 en raya cargada exitosamente");
                GameEvents.RaiseActive(true);
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error cargando escena: {e.Message}");
            minigameActive = false;
            SetPlayerControl(true);
        }

        if (promptCanvas != null)
            promptCanvas.enabled = false;
    }

    bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
                return true;
        }
        return false;
    }

    void HandleMinigameActive(bool active)
    {
        Debug.Log($"Minijuego activo: {active}");
        if (!active)
        {
            ShowWinHintIfAny();
            Invoke(nameof(ReenablePlayerControl), reenablePlayerDelay);
        }
    }

    void ReenablePlayerControl()
    {
        minigameActive = false;
        SetPlayerControl(true);

        if (playerInside && promptCanvas != null)
            promptCanvas.enabled = true;

        Debug.Log("Control del jugador restaurado");
    }

    void SetPlayerControl(bool enabled)
    {
        if (player == null)
        {
            Debug.LogWarning("Player es null en SetPlayerControl");
            return;
        }

        Player p = player.GetComponent<Player>();
        CharacterController cc = player.GetComponent<CharacterController>();

        if (p != null)
            p.enabled = enabled;
        else
            Debug.LogWarning("Componente Player no encontrado");

        if (cc != null)
            cc.enabled = enabled;
        else
            Debug.LogWarning("CharacterController no encontrado");

        Debug.Log($"Control del jugador: {enabled}");
    }
    System.Collections.IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSeconds(hintAutoHideSeconds);
        if (promptCanvas) promptCanvas.enabled = false;
    }
    void ShowWinHintIfAny()
    {
        if (!showHintOnReturn) return;
        if (!TTTTransfer.Won) return;

        if (promptText)
            promptText.text = string.IsNullOrEmpty(TTTTransfer.WinMessage) ? defaultWinHint : TTTTransfer.WinMessage;

        if (promptCanvas) promptCanvas.enabled = true;
        if (hintAutoHideSeconds > 0f) StartCoroutine(HideHintAfterDelay());

        TTTTransfer.Clear(); // para que no se repita
    }

}