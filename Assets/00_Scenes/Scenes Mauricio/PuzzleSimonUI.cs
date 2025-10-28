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

    [Header("Botones")]
    public Button btnStart;   // "Comenzar"
    public Button btnRetry;   // "Continuar"
    public Button btnExit;    // "Rendirse"

    private PuzzleSimonManager manager;

    void Awake()
    {
        // Si no está asignado manualmente, buscarlo automáticamente en la escena
        if (!btnExit)
        {
            btnExit = GameObject.Find("Rendirse")?.GetComponent<Button>();
            if (btnExit)
                Debug.Log("✅ Botón 'Rendirse' detectado automáticamente y asignado.");
        }
    }

    void Start()
    {
        manager = PuzzleSimonManager.Instance;

        // Asignar eventos
        if (btnStart) btnStart.onClick.AddListener(OnStartClicked);
        if (btnRetry) btnRetry.onClick.AddListener(OnRetryClicked);
        if (btnExit) btnExit.onClick.AddListener(OnExitClicked);

        ShowMainMenu();
    }

    void Update()
    {
        if (manager && manager.gameStarted && roundText && panelGameHUD.activeSelf)
            roundText.text = $"Ronda: {manager.currentRound}/{manager.totalRounds}";
    }

    // ==================== EVENTOS ====================
    void OnStartClicked()
    {
        panelMainMenu.SetActive(false);
        panelFail.SetActive(false);
        panelWin.SetActive(false);
        panelGameHUD.SetActive(true);
        manager.StartGame();
    }

    void OnRetryClicked()
    {
        panelFail.SetActive(false);
        panelWin.SetActive(false);
        panelGameHUD.SetActive(true);
        manager.StartGame();
    }

    public void OnExitClicked()
    {
        // Cambia de escena al rendirse
        if (Application.CanStreamedLevelBeLoaded("SceneMauricio"))
        {
            SceneManager.LoadScene("SceneMauricio");
        }
        else
        {
            Debug.LogError("⚠️ La escena 'SceneMauricio' no está incluida en Build Settings.");
        }
    }

    // ==================== PANEL CONTROL ====================
    public void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelFail.SetActive(false);
        panelWin.SetActive(false);
        panelGameHUD.SetActive(false);
    }

    public void ShowFail()
    {
        panelMainMenu.SetActive(false);
        panelFail.SetActive(true);
        panelWin.SetActive(false);
        panelGameHUD.SetActive(false);
    }

    public void ShowWin()
    {
        panelMainMenu.SetActive(false);
        panelFail.SetActive(false);
        panelWin.SetActive(true);
        panelGameHUD.SetActive(false);
    }
}
