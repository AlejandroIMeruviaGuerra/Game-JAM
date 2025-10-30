using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuzzleSequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class PadEntry
    {
        [Tooltip("Solo descriptivo (Rojo, Azul, Amarillo).")]
        public string name = "Pad";
        [Tooltip("Renderer del cubo (MeshRenderer).")]
        public Renderer renderer;
        [Tooltip("Color HDR para encendido (Emission).")]
        public Color onColor = Color.white;
        [Tooltip("Color base cuando está apagado.")]
        public Color baseColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        [Tooltip("Intensidad de emisión cuando está encendido.")]
        public float onIntensity = 5f;
        [Tooltip("Intensidad de emisión cuando está apagado.")]
        public float offIntensity = 0f;
    }

    public static PuzzleSequenceManager Instance { get; private set; }

    [Header("Pads en orden (0=Rojo, 1=Azul, 2=Amarillo)")]
    public PadEntry[] pads;

    [Header("Configuración del puzzle")]
    public int totalRounds = 3;
    public float showDelay = 0.5f;
    public float pulseTime = 0.25f;
    public float roundAccel = 0.9f;
    public int startLength = 3;
    [Tooltip("Duración del efecto de encendido/apagado suave.")]
    public float fadeDuration = 0.2f;

    [Header("Sonidos (arrastrar tus mp3 aquí)")]
    public AudioClip sonidoPad;
    public AudioClip sonidoAcierto;
    public AudioClip sonidoFallo;

    private AudioSource audioSource;

    [Header("Estado (debug)")]
    public bool gameStarted = false;
    public bool showingSequence = false;
    public bool waitingInput = false;
    public bool puzzleCompleted = false;
    public bool failed = false;
    public int inputIndex = 0;
    public int currentRound = 0;

    // Propiedades del shader
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int EmissionIntID = Shader.PropertyToID("_EmissionIntensity");

    private List<int> sequence = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Apaga todo al inicio
        for (int i = 0; i < pads.Length; i++)
            SetPadInstant(i, false);

        // Configura audio
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ----------------- Control visual -----------------
    void SetPadMaterial(Renderer r, Color baseCol, Color emissionCol, float emissionIntensity)
    {
        if (!r) return;
        var mat = r.material;
        mat.SetColor(BaseColorID, baseCol);
        mat.SetColor(EmissionColorID, emissionCol);
        mat.SetFloat(EmissionIntID, emissionIntensity);
    }

    void SetPadInstant(int index, bool on)
    {
        if (index < 0 || index >= pads.Length) return;
        var p = pads[index];
        if (!p.renderer) return;

        float intensity = on ? p.onIntensity : p.offIntensity;
        SetPadMaterial(p.renderer, p.baseColor, p.onColor, intensity);
    }

    IEnumerator FadePad(int index, bool turnOn)
    {
        if (index < 0 || index >= pads.Length) yield break;
        var p = pads[index];
        if (!p.renderer) yield break;

        float startIntensity = turnOn ? p.offIntensity : p.onIntensity;
        float endIntensity = turnOn ? p.onIntensity : p.offIntensity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            float current = Mathf.Lerp(startIntensity, endIntensity, t);
            SetPadMaterial(p.renderer, p.baseColor, p.onColor, current);
            yield return null;
        }

        if (turnOn)
            PlaySound(sonidoPad);
    }

    public void PulsePad(int index, float seconds)
    {
        StartCoroutine(CoPulsePad(index, seconds));
    }

    IEnumerator CoPulsePad(int index, float seconds)
    {
        yield return StartCoroutine(FadePad(index, true));
        yield return new WaitForSeconds(seconds);
        yield return StartCoroutine(FadePad(index, false));
    }

    // ----------------- Lógica del juego -----------------
    public void StartGame()
    {
        if (puzzleCompleted) return;

        StopAllCoroutines();

        sequence.Clear();
        gameStarted = true;
        failed = false;
        currentRound = 1;
        showDelay = 0.5f;
        inputIndex = 0;
        waitingInput = false;

        // Apaga todos los pads antes de iniciar
        for (int i = 0; i < pads.Length; i++)
            SetPadInstant(i, false);

        for (int i = 0; i < startLength; i++)
            sequence.Add(Random.Range(0, pads.Length));

        StartCoroutine(CoShowSequence());
    }

    public void StopGame()
    {
        gameStarted = false;
        showingSequence = false;
        waitingInput = false;
        inputIndex = 0;

        // Apaga todos los pads
        for (int i = 0; i < pads.Length; i++)
            SetPadInstant(i, false);
    }

    IEnumerator CoShowSequence()
    {
        showingSequence = true;
        waitingInput = false;
        inputIndex = 0;

        yield return new WaitForSeconds(0.5f);

        foreach (int idx in sequence)
        {
            PulsePad(idx, pulseTime);
            yield return new WaitForSeconds(showDelay);
        }

        showingSequence = false;
        waitingInput = true;
    }

    public void RegisterPlayerPress(int padIndex)
    {
        if (!gameStarted || !waitingInput || showingSequence || puzzleCompleted || failed)
            return;

        PulsePad(padIndex, 0.18f);

        if (padIndex == sequence[inputIndex])
        {
            inputIndex++;
            if (inputIndex >= sequence.Count)
            {
                waitingInput = false;
                StartCoroutine(CoRoundSuccess());
            }
        }
        else
        {
            StartCoroutine(CoFail());
        }
    }

    IEnumerator CoRoundSuccess()
    {
        PlaySound(sonidoAcierto);

        // Parpadeo de éxito
        for (int i = 0; i < pads.Length; i++) StartCoroutine(FadePad(i, true));
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < pads.Length; i++) StartCoroutine(FadePad(i, false));

        if (currentRound >= totalRounds)
        {
            StartCoroutine(CoWin());
        }
        else
        {
            currentRound++;
            AddNextStep();
        }
    }

    void AddNextStep()
    {
        sequence.Add(Random.Range(0, pads.Length));
        showDelay = Mathf.Max(0.12f, showDelay * roundAccel);
        StartCoroutine(CoShowSequence());
    }

    IEnumerator CoFail()
    {
        failed = true;
        waitingInput = false;
        gameStarted = false;

        PlaySound(sonidoFallo);

        // Parpadeo de fallo (todos ON/OFF)
        for (int k = 0; k < 3; k++)
        {
            for (int i = 0; i < pads.Length; i++)
                StartCoroutine(FadePad(i, true));
            yield return new WaitForSeconds(0.15f);
            for (int i = 0; i < pads.Length; i++)
                StartCoroutine(FadePad(i, false));
            yield return new WaitForSeconds(0.15f);
        }

        // Asegura que todo quede apagado
        for (int i = 0; i < pads.Length; i++)
            SetPadInstant(i, false);

        // No reinicia automáticamente, el jugador debe volver a iniciar desde el botón
        yield break;
    }

    IEnumerator CoWin()
    {
        puzzleCompleted = true;
        waitingInput = false;
        showingSequence = false;
        gameStarted = false;

        PlaySound(sonidoAcierto);

        // Mantener encendidos permanentemente
        for (int i = 0; i < pads.Length; i++)
            StartCoroutine(FadePad(i, true));

        yield break;
    }

    // ----------------- Audio -----------------
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
