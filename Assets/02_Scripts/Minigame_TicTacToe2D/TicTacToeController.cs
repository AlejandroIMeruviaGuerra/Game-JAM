using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class TicTacToeController : MonoBehaviour
{
    [Header("UI References")]
    public Button[] cellButtons;
    public Text statusText;
    public Text titleText;
    public GameObject gameOverPanel;
    public Text gameOverText;

    [Header("Resultados")]
    public GameObject resultsPanel;     // Panel con botones
    public Button btnReplay;
    public Button btnExitToLuis;
    public string sceneLuisName = "SceneLuis";

    // Estado interno
    private int[] board = new int[9];   // 0 = vacío, 1 = X (jugador), 2 = O (IA)
    private bool playerTurn = true;
    private bool gameOver = false;

    private WinLineManager winLine;
    private RectTransform[] cellRects;
    [Header("Controles en juego (persistentes)")]
    public Button btnPlayAgainTop;   // botón siempre visible: Reintentar
    public Button btnExitTop;        // botón siempre visible: Salir


    // ================== START ==================
    void Start()
    {
        // Sanitiza el entorno de UI
        Time.timeScale = 1f;
        EnsureEventSystem();
        EnsureGraphicRaycasters();
        WireUpButtons();

        if (resultsPanel) resultsPanel.SetActive(false);
        if (btnReplay) btnReplay.onClick.AddListener(Replay);
        if (btnExitToLuis) btnExitToLuis.onClick.AddListener(ExitToLuis);
        if (btnPlayAgainTop) btnPlayAgainTop.onClick.AddListener(ReplaySafe);
        if (btnExitTop) btnExitTop.onClick.AddListener(ExitToLuis);

        InitializeGame();

        // Forzar un frame y refrescar interactividad
        StartCoroutine(ForceUIUpdate());

        winLine = FindObjectOfType<WinLineManager>();

        cellRects = new RectTransform[cellButtons.Length];
        for (int i = 0; i < cellButtons.Length; i++)
            cellRects[i] = cellButtons[i].GetComponent<RectTransform>();
    }
    void ClearWinLine()
    {
        var wl = FindObjectOfType<WinLineManager>();
        if (wl != null)
        {
            // apaga cualquier línea activa
            // (si tu WinLineManager expone un método público para apagar, mejor)
            wl.StopAllCoroutines();
            // fuerza disable de los LineRenderer hijos si existen
            foreach (var lr in wl.GetComponentsInChildren<LineRenderer>(true))
                lr.enabled = false;
        }
    }

    public void ReplaySafe()
    {
        // por si quedaban invokes/corutinas pendientes
        CancelInvoke(nameof(AIMove));
        StopAllCoroutines();

        ClearWinLine();

        for (int i = 0; i < 9; i++) board[i] = 0;
        gameOver = false; playerTurn = true;

        if (statusText) statusText.text = "Tu turno (X)";
        if (resultsPanel) resultsPanel.SetActive(false);

        UpdateAllCells();              // limpia X/O visual
        UpdateAllCellsInteractable();  // re-habilita celdas
    }

    // ====== Helpers de UI/Input ======
    void EnsureEventSystem()
    {
        // Busca cualquier EventSystem vivo (incluye DontDestroyOnLoad)
        var all = Resources.FindObjectsOfTypeAll<EventSystem>();
        if (all.Length == 0)
        {
            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();

            // Usa nuevo módulo si está disponible
            var newType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", false);
            if (newType != null) go.AddComponent(newType);
            else go.AddComponent<StandaloneInputModule>();

            Debug.Log("✅ EventSystem creado");
        }
        else
        {
            // elimina duplicados, deja el primero
            for (int i = 1; i < all.Length; i++)
            {
                Debug.LogWarning($"🧹 Eliminando EventSystem duplicado: {all[i].name}");
                Destroy(all[i].gameObject);
            }

            var keep = all[0];
            var newType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", false);
            var hasNew = (newType != null) && keep.GetComponent(newType) != null;
            var old = keep.GetComponent<StandaloneInputModule>();
            if (newType != null)
            {
                if (!hasNew) keep.gameObject.AddComponent(newType);
                if (old) old.enabled = false;
            }
            else
            {
                if (!old) keep.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        // Cursor visible por si vienes de FPS
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EnsureGraphicRaycasters()
    {
        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            if (!c.GetComponent<GraphicRaycaster>())
                c.gameObject.AddComponent<GraphicRaycaster>();
            c.sortingOrder = 100; // encima de todo
        }

        // Evita bloqueadores: cualquier Image que no sea botón, sin raycast
        foreach (var img in FindObjectsOfType<Image>(true))
        {
            if (!img.GetComponent<Button>()) img.raycastTarget = false;
        }
    }

    void WireUpButtons()
    {
        // Conecta onClick por código: no dependas del Inspector
        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (!cellButtons[i]) { Debug.LogError($"❌ cellButtons[{i}] es null"); continue; }
            int idx = i;
            cellButtons[i].onClick.RemoveAllListeners();
            cellButtons[i].onClick.AddListener(() => OnCellClicked(idx));
        }
    }

    IEnumerator ForceUIUpdate()
    {
        yield return null;
        UpdateAllCellsInteractable();
    }

    // ====== Lógica del juego ======
    void InitializeGame()
    {
        for (int i = 0; i < 9; i++) board[i] = 0;
        playerTurn = true;
        gameOver = false;

        if (titleText) titleText.text = " JUEGO 3 EN RAYA";
        if (statusText) statusText.text = "Tu turno (X)";
        if (gameOverPanel) gameOverPanel.SetActive(false);

        UpdateAllCells();
        UpdateAllCellsInteractable();

        Debug.Log("🎮 Juego inicializado");
    }

    public void OnCellClicked(int index)
    {
        if (gameOver) return;
        if (!playerTurn) return;
        if (board[index] != 0) return;

        board[index] = 1; // Jugador X
        UpdateCellVisual(index);

        if (CheckGameEnd()) return;

        playerTurn = false;
        if (statusText) statusText.text = "Turno IA (O)";
        UpdateAllCellsInteractable();

        Invoke(nameof(AIMove), 0.4f);
    }

    void AIMove()
    {
        if (gameOver) return;

        int move = FindBestMove();
        if (move >= 0 && board[move] == 0)
        {
            board[move] = 2; // IA O
            UpdateCellVisual(move);
        }

        if (CheckGameEnd()) return;

        playerTurn = true;
        if (statusText) statusText.text = "Tu turno (X)";
        UpdateAllCellsInteractable();
    }

    void UpdateAllCells()
    {
        for (int i = 0; i < 9; i++) UpdateCellVisual(i);
    }

    void UpdateAllCellsInteractable()
    {
        for (int i = 0; i < 9; i++)
        {
            if (!cellButtons[i]) continue;
            bool can = (board[i] == 0) && playerTurn && !gameOver;
            cellButtons[i].interactable = can;
        }
    }

    void UpdateCellVisual(int index)
    {
        if (!cellButtons[index]) return;
        var txt = cellButtons[index].GetComponentInChildren<Text>();
        if (!txt) return;

        if (board[index] == 1) { txt.text = "X"; txt.color = Color.blue; }
        else if (board[index] == 2) { txt.text = "O"; txt.color = Color.red; }
        else { txt.text = ""; }
    }

    bool CheckGameEnd()
    {
        if (CheckWinner(1))
        {
            gameOver = true;
            if (statusText) statusText.text = "¡GANASTE! 🎉";
            //if (resultsPanel) resultsPanel.SetActive(true);
            // Señal para el NPC
            TTTTransfer.Won = true;
            TTTTransfer.WinMessage = "Me ganaste. La llave está encima del refrigerador.";
            StartCoroutine(DelayedExitToLuis(2f));
            //ExitToLuis(); // volver a la escena principal
            return true;
        }

        if (CheckWinner(2))
        {
            gameOver = true;
            if (statusText) statusText.text = "Perdiste 😢";
            if (resultsPanel) resultsPanel.SetActive(true);
            return true;
        }

        if (IsBoardFull())
        {
            gameOver = true;
            if (statusText) statusText.text = "Empate 🤝";
            if (resultsPanel) resultsPanel.SetActive(true);
            return true;
        }

        return false;
    }
    IEnumerator DelayedExitToLuis(float delay)
    {
        // Bloquea el tablero
        UpdateAllCellsInteractable();
        yield return new WaitForSeconds(delay);

        ExitToLuis();
    }

    bool CheckWinner(int player)
    {
        int[,] lines = {
        {0,1,2},{3,4,5},{6,7,8},
        {0,3,6},{1,4,7},{2,5,8},
        {0,4,8},{2,4,6}
    };

        for (int i = 0; i < 8; i++)
        {
            int a = lines[i, 0], b = lines[i, 1], c = lines[i, 2];
            if (board[a] == player && board[b] == player && board[c] == player)
            {
                // Aquí sí dibujamos la línea real
                if (winLine && cellRects[a] && cellRects[c])
                {
                    Vector3 start = cellRects[a].position;
                    Vector3 end = cellRects[c].position;
                    Color color = (player == 1) ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.3f, 0.3f);
                    winLine.DrawLine(start, end, color, 0.5f);
                }
                return true;
            }
        }
        return false;
    }



    bool IsBoardFull()
    {
        foreach (var c in board) if (c == 0) return false;
        return true;
    }

    int FindBestMove()
    {
        // gana si puede
        for (int i = 0; i < 9; i++)
        {
            if (board[i] != 0) continue;
            board[i] = 2; if (HasWinner(2)) { board[i] = 0; return i; }
            board[i] = 0;
        }
        // bloquea al jugador
        for (int i = 0; i < 9; i++)
        {
            if (board[i] != 0) continue;
            board[i] = 1; if (HasWinner(1)) { board[i] = 0; return i; }
            board[i] = 0;
        }
        if (board[4] == 0) return 4;
        int[] corners = { 0, 2, 6, 8 }; foreach (var c in corners) if (board[c] == 0) return c;
        int[] sides = { 1, 3, 5, 7 }; foreach (var s in sides) if (board[s] == 0) return s;
        return -1;
    }
    // Esta función solo revisa si hay ganador, sin efectos visuales
    bool HasWinner(int player)
    {
        int[,] lines = {
        {0,1,2},{3,4,5},{6,7,8},
        {0,3,6},{1,4,7},{2,5,8},
        {0,4,8},{2,4,6}
    };

        for (int i = 0; i < 8; i++)
        {
            int a = lines[i, 0], b = lines[i, 1], c = lines[i, 2];
            if (board[a] == player && board[b] == player && board[c] == player)
                return true;
        }
        return false;
    }


    public void Replay()
    {
        for (int i = 0; i < 9; i++) board[i] = 0;
        gameOver = false; playerTurn = true;
        if (statusText) statusText.text = "Tu turno (X)";
        UpdateAllCells();
        UpdateAllCellsInteractable();
        if (resultsPanel) resultsPanel.SetActive(false);
    }

    public void ExitToLuis()
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneLuisName))
        {
            Debug.LogError($"[TTT] La escena '{sceneLuisName}' no está en Build Settings.");
            return;
        }
        GameEvents.RaiseActive(false);
        SceneManager.LoadScene(sceneLuisName, LoadSceneMode.Single);
    }
}
