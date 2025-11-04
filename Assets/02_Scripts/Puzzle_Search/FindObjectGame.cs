using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FindObjectGame : MonoBehaviour
{
    [Header("Configuración del juego")]
    public float tiempoLimite = 10f;
    private float tiempoRestante;
    private bool encontrado = false;
    private bool puedeClickear = true;

    [Header("UI")]
    public TMP_Text textoResultado;
    public TMP_Text textoTemporizador;
    public GameObject panelUI;
    public Button btnReintentar;
    public Button btnSalir;

    [Header("Objeto objetivo")]
    public GameObject objetoObjetivo;
    public float escalaObjetivo = 1.3f;

    [Header("Tapadores")]
    public Sprite[] spritesTapadores;
    public int cantidadTapadores = 12;
    public float radioCobertura = 1.2f;
    public float tamañoDeseado = 1.25f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxInicio;
    public AudioClip sfxClicTapador;
    public AudioClip sfxGanar;
    public AudioClip sfxPerder;
    public AudioClip sfxArrastrar;

    private Vector3 escalaInicial;
    public float velocidadRespiracion = 2.5f;
    public float intensidadRespiracion = 0.15f;

    private List<GameObject> tapadores = new List<GameObject>();
    private Collider2D colObjetivo;

    void Start()
    {
        tiempoRestante = tiempoLimite;
        panelUI.SetActive(false);
        if (btnReintentar) btnReintentar.gameObject.SetActive(false);
        if (btnSalir) btnSalir.gameObject.SetActive(false);

        if (!objetoObjetivo.TryGetComponent(out colObjetivo))
            colObjetivo = objetoObjetivo.AddComponent<PolygonCollider2D>();

        objetoObjetivo.transform.localScale *= escalaObjetivo;
        objetoObjetivo.transform.position = new Vector3(
            objetoObjetivo.transform.position.x,
            objetoObjetivo.transform.position.y,
            0f
        );

        escalaInicial = objetoObjetivo.transform.localScale;

        if (btnReintentar) btnReintentar.onClick.AddListener(ReiniciarJuego);
        if (btnSalir) btnSalir.onClick.AddListener(SalirEscena);

        CrearTapadores();

        // 🎵 Sonido de inicio
        if (audioSource && sfxInicio)
            audioSource.PlayOneShot(sfxInicio);
    }

    void Update()
    {
        if (!encontrado && puedeClickear)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0f)
            {
                tiempoRestante = 0f;
                Perder();
            }
            ActualizarTemporizador();
        }

        Respirar();

        if (Input.GetMouseButtonDown(0) && puedeClickear && !encontrado)
            IntentarClic();
    }

    void IntentarClic()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Si clicas sobre un tapador, no cuenta (pero hace sonido)
        Collider2D[] sobreClick = Physics2D.OverlapPointAll(mousePos);
        foreach (var c in sobreClick)
        {
            if (c.CompareTag("Tapador"))
            {
                if (audioSource && sfxClicTapador)
                    audioSource.PlayOneShot(sfxClicTapador);
                return;
            }
        }

        // Si clicas en el objetivo visible
        if (colObjetivo != null && colObjetivo.OverlapPoint(mousePos))
        {
            Encontrar();
        }
    }

    void Respirar()
    {
        float scaleOffset = Mathf.Sin(Time.time * velocidadRespiracion) * intensidadRespiracion;
        objetoObjetivo.transform.localScale = escalaInicial * (1f + scaleOffset);
    }

    void CrearTapadores()
    {
        if (!TagExists("Tapador"))
        {
            Debug.LogWarning("Etiqueta 'Tapador' no existe. Asignando 'Untagged'.");
        }

        if (spritesTapadores == null || spritesTapadores.Length == 0)
        {
            Debug.LogWarning("⚠️ No hay sprites en 'spritesTapadores'.");
            return;
        }

        Vector3 centro = objetoObjetivo.transform.position;

        for (int i = 0; i < cantidadTapadores; i++)
        {
            GameObject obj = new GameObject("Tapador_" + i);
            obj.tag = TagExists("Tapador") ? "Tapador" : "Untagged";
            obj.transform.position = centro + new Vector3(
                Random.Range(-radioCobertura, radioCobertura),
                Random.Range(-radioCobertura, radioCobertura),
                0f
            );

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = spritesTapadores[Random.Range(0, spritesTapadores.Length)];
            sr.sortingOrder = 100 + i;

            float ancho = sr.sprite.bounds.size.x;
            float factorEscala = (ancho > 0f) ? (tamañoDeseado / ancho) : 1f;
            obj.transform.localScale = new Vector3(factorEscala, factorEscala, 1f);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));

            PolygonCollider2D col = obj.AddComponent<PolygonCollider2D>();
            col.isTrigger = false;

            var drag = obj.AddComponent<DraggableSimple>();
            drag.limiteMovimiento = new Vector2(3.5f, 2.5f);
            drag.sfxArrastrar = sfxArrastrar;
            drag.audioSource = audioSource;

            tapadores.Add(obj);
        }
    }

    bool TagExists(string tag)
    {
        try
        {
            var temp = new GameObject();
            temp.tag = tag;
            Destroy(temp);
            return true;
        }
        catch
        {
            return false;
        }
    }

    void Encontrar()
    {
        encontrado = true;
        puedeClickear = false;

        foreach (var t in tapadores)
        {
            if (t)
                t.AddComponent<FadeOutAndDestroy>();
        }

        panelUI.SetActive(true);
        textoResultado.text = "¡Objeto Encontrado!";
        textoResultado.color = Color.yellow;

        if (btnReintentar) btnReintentar.gameObject.SetActive(true);
        if (btnSalir) btnSalir.gameObject.SetActive(true);

        // 🏆 Sonido de victoria
        if (audioSource && sfxGanar)
            audioSource.PlayOneShot(sfxGanar);
    }

    void Perder()
    {
        if (encontrado) return;
        puedeClickear = false;
        panelUI.SetActive(true);
        textoResultado.text = "¡Tiempo agotado!";
        textoResultado.color = Color.red;

        if (btnReintentar) btnReintentar.gameObject.SetActive(true);
        if (btnSalir) btnSalir.gameObject.SetActive(true);

        // 💥 Sonido de derrota
        if (audioSource && sfxPerder)
            audioSource.PlayOneShot(sfxPerder);
    }

    void ActualizarTemporizador()
    {
        if (!textoTemporizador) return;
        textoTemporizador.text = $"{tiempoRestante:F1}s";
        if (tiempoRestante > tiempoLimite * 0.6f)
            textoTemporizador.color = Color.green;
        else if (tiempoRestante > tiempoLimite * 0.3f)
            textoTemporizador.color = Color.yellow;
        else
            textoTemporizador.color = Color.red;
    }

    public void ReiniciarJuego() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void SalirEscena() => SceneManager.LoadScene("SceneMauricio");
}


// ------------------ DRAG CON AUDIO ------------------
public class DraggableSimple : MonoBehaviour
{
    private bool arrastrando = false;
    private Vector3 offset;
    public Vector2 limiteMovimiento = new Vector2(3f, 2f);
    private SpriteRenderer sr;
    private int sortingBase;
    private static int sortingTop = 1000;

    [HideInInspector] public AudioSource audioSource;
    [HideInInspector] public AudioClip sfxArrastrar;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sortingBase = sr.sortingOrder;
    }

    void OnMouseDown()
    {
        arrastrando = true;
        transform.localScale *= 1.05f;
        sr.sortingOrder = sortingTop++;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mouseWorld.x, mouseWorld.y, 0f);

        // 🎵 Sonido al comenzar arrastre
        if (audioSource && sfxArrastrar)
            audioSource.PlayOneShot(sfxArrastrar);
    }

    void OnMouseUp()
    {
        arrastrando = false;
        transform.localScale /= 1.05f;
    }

    void Update()
    {
        if (!arrastrando) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        mousePos.z = 0f;
        mousePos.x = Mathf.Clamp(mousePos.x, -limiteMovimiento.x, limiteMovimiento.x);
        mousePos.y = Mathf.Clamp(mousePos.y, -limiteMovimiento.y, limiteMovimiento.y);

        transform.position = Vector3.Lerp(transform.position, mousePos, 0.5f);
    }
}


// ------------------ FADE OUT ------------------
public class FadeOutAndDestroy : MonoBehaviour
{
    private SpriteRenderer sr;
    private float velocidad = 1.5f;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (!sr) return;
        var c = sr.color;
        c.a -= Time.deltaTime * velocidad;
        sr.color = c;
        if (c.a <= 0f) Destroy(gameObject);
    }
}
