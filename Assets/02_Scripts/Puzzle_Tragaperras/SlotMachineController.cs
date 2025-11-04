using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[RequireComponent(typeof(AudioSource))]
public class SlotMachineController : MonoBehaviour
{
    [Header("Reels (columnas)")]
    public ReelMotion[] reels;   // Debe haber 3

    [Header("UI")]
    public Button btnStart;
    public Button btnStop;
    public TMP_Text txtStatus;
    public TMP_Text txtResult;

    [Header("Configuración")]
    public float spinSpeed = 14f;

    [Header("Audio")]
    public AudioClip sfxStartSpin;  //  inicio de giro
    public AudioClip sfxReelLoop;   //  sonido de giro (loop)
    public AudioClip sfxStopReel;   //  detiene reel
    public AudioClip sfxWin;        //  ganar
    public AudioClip sfxLose;       //  perder

    private AudioSource audioSource;
    private AudioSource loopSource; // canal aparte para loop

    private bool spinning;
    private int nextToStop;

    void Awake()
    {
        // Canal principal
        audioSource = GetComponent<AudioSource>();

        // Canal secundario solo para el loop
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
    }

    void Start()
    {
        btnStart.onClick.AddListener(StartSpinning);
        btnStop.onClick.AddListener(StopNextReel);

        txtStatus.text = "Pulsa COMENZAR";
        txtResult.text = "";
        btnStop.interactable = false;
    }

    // ==================== INICIO ====================
    public void StartSpinning()
    {
        if (spinning) return;

        spinning = true;
        nextToStop = 0;
        txtStatus.text = "Girando...";
        txtResult.text = "";
        btnStart.interactable = false;
        btnStop.interactable = true;

        //  Sonido de arranque
        PlayOneShot(sfxStartSpin);

        //  Loop del giro (si existe)
        if (sfxReelLoop)
        {
            loopSource.clip = sfxReelLoop;
            loopSource.volume = 0.5f;
            loopSource.Play();
        }

        foreach (var r in reels)
            r.StartSpin(spinSpeed);
    }

    // ==================== DETENER ====================
    public void StopNextReel()
    {
        if (!spinning) return;

        if (nextToStop < reels.Length)
        {
            txtStatus.text = $"Deteniendo Reel {nextToStop + 1}";
            reels[nextToStop].StartSlowDown();

            //  Sonido de parada de cada reel
            PlayOneShot(sfxStopReel);

            nextToStop++;
        }

        if (nextToStop >= reels.Length)
            StartCoroutine(WaitAll());
    }

    IEnumerator WaitAll()
    {
        btnStop.interactable = false;
        yield return new WaitUntil(AllStopped);

        //  Detiene el loop del giro
        if (loopSource.isPlaying)
            loopSource.Stop();

        spinning = false;
        btnStart.interactable = true;

        CheckMiddleRowOnly();
    }

    bool AllStopped()
    {
        foreach (var r in reels)
            if (!r.IsStopped()) return false;
        return true;
    }

    // ==================== RESULTADO ====================
    void CheckMiddleRowOnly()
    {
        var c0 = reels[0].GetCenterSprite();
        var c1 = reels[1].GetCenterSprite();
        var c2 = reels[2].GetCenterSprite();

        bool win = (c0 && c1 && c2) && ReferenceEquals(c0, c1) && ReferenceEquals(c1, c2);

        Debug.Log($"[CHECK] Center = {(c0 ? c0.name : "null")} | {(c1 ? c1.name : "null")} | {(c2 ? c2.name : "null")}");

        if (win)
        {
            txtResult.text = "¡GANASTE!";
            txtStatus.text = "Combinación perfecta en la fila 2";

            //  Sonido de victoria
            PlayOneShot(sfxWin);

            //  Confeti visual
            var confetti = FindObjectOfType<ConfettiController>();
            if (confetti) confetti.PlayConfetti();
        }
        else
        {
            txtResult.text = "Vuelve a intentarlo";
            txtStatus.text = "¡Listo!";

            // 🔊 Sonido de pérdida
            PlayOneShot(sfxLose);
        }
    }

    // ==================== AUDIO HELPER ====================
    void PlayOneShot(AudioClip clip, float vol = 1f)
    {
        if (clip && audioSource)
            audioSource.PlayOneShot(clip, vol);
    }

    public void IrAEscena(string nombreEscena)
    {
        SceneManager.LoadScene("SceneMauricio");
    }
}
