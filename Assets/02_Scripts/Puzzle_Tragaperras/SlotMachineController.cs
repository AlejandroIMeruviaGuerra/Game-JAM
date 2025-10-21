using System.Collections;
using UnityEngine;

/// Tragamonedas 3 carretes (solo SpriteRenderer, sin Canvas)
public class SlotMachineController : MonoBehaviour
{
    public enum Estado { Listo, Girando, Parando1, Parando2, Parando3, Resultado, Final }

    [Header("Carretes (izq→der)")]
    [Tooltip("Arrastra 3 SpriteRenderers de la escena.")]
    public SpriteRenderer[] carretes = new SpriteRenderer[3];

    [Header("Símbolos")]
    [Tooltip("Sprites que pueden salir (ej: PRINCESA, JUAN, HUESO).")]
    public Sprite[] simbolos;

    [Header("Giro")]
    [Tooltip("Menor intervalo entre ticks (más rápido).")]
    public float pasoMin = 0.05f;
    [Tooltip("Mayor intervalo entre ticks.")]
    public float pasoMax = 0.12f;

    [Header("Frenado")]
    [Tooltip("Duración del frenado (s).")]
    public float desaceleracion = 0.8f;
    [Tooltip("Ticks extra durante el frenado.")]
    public int ticksExtra = 10;

    [Header("Sonido (opcional)")]
    [Tooltip("AudioSource desde el que suenan los clips.")]
    public AudioSource sfxFuente;
    [Tooltip("Tick durante el giro.")]
    public AudioClip sfxGiro;
    [Tooltip("Golpe al parar cada carrete.")]
    public AudioClip sfxParada;
    [Tooltip("Fanfarria al ganar.")]
    public AudioClip sfxGana;
    [Tooltip("Fail al perder.")]
    public AudioClip sfxFalla;

    // --- Internos ---
    [SerializeField] private Estado estado = Estado.Listo;
    int sigParada = 0;
    int[] idx = new int[3];
    bool[] girando = new bool[3];
    float[] paso = new float[3];
    Coroutine[] co = new Coroutine[3];
    bool ultimaFueVictoria = false;

    void Awake()
    {
        if (carretes == null || carretes.Length != 3) { Debug.LogError("Carretes: asigna 3."); enabled = false; }
        if (simbolos == null || simbolos.Length < 3) { Debug.LogError("Símbolos: mínimo 3."); enabled = false; }
    }

    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            idx[i] = Random.Range(0, simbolos.Length);
            carretes[i].sprite = simbolos[idx[i]];
        }
    }

    // Llama esto desde tu activador (tecla F, etc.)
    public void OnMainButton() => Boton();

    public void Boton()
    {
        if (estado == Estado.Final) return;

        switch (estado)
        {
            case Estado.Listo: Iniciar(); break;
            case Estado.Girando: PararSiguiente(); break;
            case Estado.Parando1: PararSiguiente(); break;
            case Estado.Parando2: PararSiguiente(); break; // luego evalúa
            case Estado.Resultado:
                if (!ultimaFueVictoria) { Resetear(); Iniciar(); }
                break;
        }
    }

    void Iniciar()
    {
        sigParada = 0;
        for (int i = 0; i < 3; i++)
        {
            if (co[i] != null) StopCoroutine(co[i]);
            girando[i] = true;
            paso[i] = Random.Range(pasoMin, pasoMax);
            co[i] = StartCoroutine(Girar(i));
        }
        estado = Estado.Girando;
    }

    void PararSiguiente()
    {
        if (sigParada > 2) return;
        int r = sigParada++;
        if (co[r] != null) StartCoroutine(FrenarYParar(r));

        if (estado == Estado.Girando) estado = Estado.Parando1;
        else if (estado == Estado.Parando1) estado = Estado.Parando2;
        else if (estado == Estado.Parando2) estado = Estado.Parando3;
    }

    IEnumerator Girar(int r)
    {
        while (girando[r])
        {
            Avanzar(r);
            Tocar(sfxGiro, 0.25f);
            yield return new WaitForSeconds(paso[r]);
        }
    }

    IEnumerator FrenarYParar(int r)
    {
        girando[r] = false;

        float t = 0f, inicio = paso[r], fin = inicio + 0.12f;
        int ticks = ticksExtra;

        while (t < desaceleracion || ticks > 0)
        {
            paso[r] = Mathf.Lerp(inicio, fin, Mathf.Clamp01(t / desaceleracion));
            Avanzar(r);
            Tocar(sfxGiro, 0.25f);

            float espera = paso[r];
            yield return new WaitForSeconds(espera);
            t += espera; ticks--;
        }

        idx[r] = Random.Range(0, simbolos.Length);
        carretes[r].sprite = simbolos[idx[r]];
        Tocar(sfxParada, 0.9f);

        if (!girando[0] && !girando[1] && !girando[2]) Evaluar();
    }

    void Avanzar(int r)
    {
        idx[r] = (idx[r] + 1) % simbolos.Length;
        carretes[r].sprite = simbolos[idx[r]];
    }

    void Evaluar()
    {
        bool win = (idx[0] == idx[1]) && (idx[1] == idx[2]);
        ultimaFueVictoria = win;

        if (win)
        {
            Tocar(sfxGana, 1f);
            estado = Estado.Final;   // termina
            Debug.Log("GANASTE ✅");
        }
        else
        {
            Tocar(sfxFalla, 1f);
            estado = Estado.Resultado; // espera nueva pulsación
            Debug.Log("Intenta de nuevo ❌");
        }
    }

    void Resetear()
    {
        for (int i = 0; i < 3; i++)
        {
            if (co[i] != null) StopCoroutine(co[i]);
            co[i] = null; girando[i] = false;
        }
        estado = Estado.Listo;
    }

    void Tocar(AudioClip clip, float vol = 1f)
    {
        if (sfxFuente && clip) sfxFuente.PlayOneShot(clip, vol);
    }

    // Útil si quieres permitir nueva partida tras ganar desde otro trigger
    public void ReiniciarTrasGanar()
    {
        ultimaFueVictoria = false;
        Resetear();
        Iniciar();
    }
}
