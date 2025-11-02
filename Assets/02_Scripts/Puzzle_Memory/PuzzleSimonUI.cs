using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PuzzleSimonUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMainMenu;
    public GameObject panelFail;
    public GameObject panelWin;
    public GameObject panelGameHUD;

    [Header("Textos")]
    public TMP_Text roundText;
    public TMP_Text failText;
    public TMP_Text winText;

    [Header("Botones MAIN")]
    public Button btnStart;          // Comenzar (menú principal)

    [Header("Botones FAIL")]
    public Button btnRetryFail;      // Continuar (al perder)
    public Button btnExitFail;       // Rendirse (al perder)

    [Header("Botones WIN")]
    public Button btnContinueWin;    // Continuar (al ganar)
    public Button btnExitWin;        // Salir (al ganar)

    [Header("Configuración")]
    [Tooltip("Nombre de la escena a la que volverá el botón Salir/Rendirse")]
    public string returnSceneName = "SceneMauricio";

    private PuzzleSimonManager manager;

    void Start()
    {
        manager = PuzzleSimonManager.Instance;

        // 🎮 Asignar listeners
        if (btnStart) btnStart.onClick.AddListener(OnStartClicked);
        if (btnRetryFail) btnRetryFail.onClick.AddListener(OnRetryClicked);
        if (btnExitFail) btnExitFail.onClick.AddListener(OnExitClicked);
        if (btnContinueWin) btnContinueWin.onClick.AddListener(OnContinueClicked);
        if (btnExitWin) btnExitWin.onClick.AddListener(OnExitClicked);

        ShowMainMenu();
    }

    void Update()
    {
        // Mostrar ronda en tiempo real
        if (manager && manager.gameStarted && roundText && panelGameHUD.activeSelf)
            roundText.text = $"Ronda: {manager.currentRound}/{manager.totalRounds}";
    }

    // ==================== EVENTOS DE BOTONES ====================

    void OnStartClicked()
    {
        ShowGameHUD();
        manager.StartGame();
    }

    void OnRetryClicked()
    {
        ShowGameHUD();
        manager.StartGame();
    }

    void OnContinueClicked()
    {
        ShowGameHUD();
        manager.StartGame();
    }

    void OnExitClicked()
    {
        manager.StopGame();

        if (Application.CanStreamedLevelBeLoaded(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
        else
        {
            Debug.LogError($"⚠️ La escena '{returnSceneName}' no está incluida en Build Settings.");
            ShowMainMenu();
        }
    }

    // ==================== PANEL CONTROL ====================

    public void ShowMainMenu()
    {
        panelMainMenu?.SetActive(true);
        panelFail?.SetActive(false);
        panelWin?.SetActive(false);
        panelGameHUD?.SetActive(false);
    }

    public void ShowGameHUD()
    {
        panelMainMenu?.SetActive(false);
        panelFail?.SetActive(false);
        panelWin?.SetActive(false);
        panelGameHUD?.SetActive(true);
    }

    public void ShowFail()
    {
       
        panelMainMenu?.SetActive(false);
        panelFail?.SetActive(true);
        panelWin?.SetActive(false);
        panelGameHUD?.SetActive(false);
    }

    public void ShowWin()
    {
        
        panelMainMenu?.SetActive(false);
        panelFail?.SetActive(false);
        panelWin?.SetActive(true);
        panelGameHUD?.SetActive(false);
    }
}
