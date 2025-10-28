using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleSimonManager : MonoBehaviour
{
    public static PuzzleSimonManager Instance { get; private set; }

    [Header("Pads en orden (izquierda→derecha)")]
    public LightPad[] pads; // arrastra Luz1, Luz2, Luz3 (component LightPad)

    [Header("Gameplay")]
    public int totalRounds = 5;
    public int startLength = 1;
    public float showDelay = 0.8f;   // tiempo base entre luces
    public float pulseTime = 0.45f;  // duración del pulso visual
    [Tooltip("Multiplica el delay por este factor cada ronda (acelera)")]
    public float roundAccel = 0.9f;

    [Header("Audio (opcional)")]
    public AudioSource audioSource;
    public AudioClip sfxSuccess;
    public AudioClip sfxFail;
    public AudioClip sfxRoundUp;

    [Header("Estado (debug)")]
    public bool gameStarted = false;
    public bool showingSequence = false;
    public bool waitingInput = false;
    public bool puzzleCompleted = false;
    public bool failed = false;
    public int currentRound = 0;

    private List<int> sequence = new();
    private int inputIndex = 0;
    private PuzzleSimonUI ui;

    // 🔹 Nuevo: valor base del ShowDelay para reiniciarlo correctamente
    private float baseShowDelay;

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        ui = Object.FindFirstObjectByType<PuzzleSimonUI>();
    }

    void Start()
    {
        // Guardamos el valor inicial del showDelay (el del Inspector)
        baseShowDelay = showDelay;

        // Apaga todos los pads al iniciar
        foreach (var p in pads)
            if (p) p.SetOn(false, true);

        // Muestra menú principal al iniciar
        if (ui)
            ui.ShowMainMenu();
    }

    void Update()
    {
        if (!ui) return;

        // Mostrar pantallas según estado
        if (failed && !gameStarted)
            ui.ShowFail();
        else if (puzzleCompleted)
            ui.ShowWin();
    }

    // ===================== LÓGICA DEL JUEGO =====================
    public void StartGame()
    {
        StopAllCoroutines();

        // 🔹 Reinicia el valor del delay base
        showDelay = baseShowDelay;

        sequence.Clear();
        gameStarted = true;
        failed = false;
        puzzleCompleted = false;
        showingSequence = false;
        waitingInput = false;
        inputIndex = 0;
        currentRound = 1;

        foreach (var p in pads)
            if (p) p.SetOn(false, true);

        // Generar secuencia inicial
        for (int i = 0; i < startLength; i++)
            sequence.Add(Random.Range(0, pads.Length));

        StartCoroutine(CoShowSequence());
    }

    public void StopGame()
    {
        StopAllCoroutines();
        gameStarted = false;
        showingSequence = false;
        waitingInput = false;
        inputIndex = 0;

        foreach (var p in pads)
            if (p) p.SetOn(false, true);
    }

    IEnumerator CoShowSequence()
    {
        showingSequence = true;
        waitingInput = false;
        inputIndex = 0;

        yield return new WaitForSeconds(0.5f);

        foreach (int idx in sequence)
        {
            pads[idx].Pulse(pulseTime);
            yield return new WaitForSeconds(showDelay);
        }

        showingSequence = false;
        waitingInput = true;
    }

    public void RegisterPlayerPress(int padIndex)
    {
        if (!gameStarted || !waitingInput || showingSequence || puzzleCompleted || failed)
            return;

        if (padIndex == sequence[inputIndex])
        {
            inputIndex++;
            if (inputIndex >= sequence.Count)
                StartCoroutine(CoRoundSuccess());
        }
        else
        {
            StartCoroutine(CoFail());
        }
    }

    IEnumerator CoRoundSuccess()
    {
        waitingInput = false;
        Play(sfxRoundUp);

        foreach (var p in pads) p.SetOn(true);
        yield return new WaitForSeconds(0.25f);
        foreach (var p in pads) p.SetOn(false);

        if (currentRound >= totalRounds)
            StartCoroutine(CoWin());
        else
        {
            currentRound++;
            sequence.Add(Random.Range(0, pads.Length));

            // 🔹 Acelera un poco, pero nunca por debajo de 0.4 segundos
            showDelay = Mathf.Max(0.4f, showDelay * roundAccel);

            yield return new WaitForSeconds(0.3f);
            StartCoroutine(CoShowSequence());
        }
    }

    IEnumerator CoFail()
    {
        failed = true;
        waitingInput = false;
        gameStarted = false;
        Play(sfxFail);

        for (int k = 0; k < 3; k++)
        {
            foreach (var p in pads) p.SetOn(true);
            yield return new WaitForSeconds(0.15f);
            foreach (var p in pads) p.SetOn(false);
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator CoWin()
    {
        puzzleCompleted = true;
        waitingInput = false;
        showingSequence = false;
        gameStarted = false;
        Play(sfxSuccess);

        foreach (var p in pads) p.SetOn(true);
        yield return new WaitForSeconds(1.5f);
        foreach (var p in pads) p.SetOn(false);
    }

    void Play(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }
}
