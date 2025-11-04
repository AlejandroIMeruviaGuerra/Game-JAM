using System.Collections;
using UnityEngine;

public class ReelMotion : MonoBehaviour
{
    [Header("Movimiento del carrete")]
    public float spacing = 3.2f;      // Distancia vertical fija entre símbolos
    public float spinSpeed = 14f;     // Velocidad al iniciar
    public Sprite[] symbols;          // Banco de sprites

    // Siempre mantenemos 3 hijos llamados Symbol_0, _1, _2
    private SpriteRenderer[] nodes = new SpriteRenderer[3];

    // Estado
    private bool isSpinning;
    private bool slowingDown;
    private bool aligning;
    private float currentSpeed;
    private const float decel = 10f;

    void Awake()
    {
        // Localiza los tres nodos (deben existir)
        for (int i = 0; i < 3; i++)
        {
            var t = transform.Find($"Symbol_{i}");
            if (!t)
            {
                Debug.LogError($"[ReelMotion] Falta hijo 'Symbol_{i}' en {name}");
                continue;
            }
            nodes[i] = t.GetComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        // Posiciones iniciales perfectamente espaciadas (arriba, centro, abajo)
        if (nodes[0]) nodes[0].transform.localPosition = new Vector3(0, spacing, 0);
        if (nodes[1]) nodes[1].transform.localPosition = new Vector3(0, 0, 0);
        if (nodes[2]) nodes[2].transform.localPosition = new Vector3(0, -spacing, 0);

        // Sprites al azar
        RandomizeAll();
    }

    void Update()
    {
        if (!isSpinning) return;

        // Desplazamiento hacia abajo
        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i]) continue;
            var p = nodes[i].transform.localPosition;
            p.y -= currentSpeed * Time.deltaTime;

            // Cuando pasa el límite, reaparece arriba con sprite nuevo
            if (p.y <= -spacing * 1.51f)
            {
                p.y += spacing * 3f; // salta 3 pasos (de abajo a arriba)
                if (symbols != null && symbols.Length > 0)
                    nodes[i].sprite = symbols[Random.Range(0, symbols.Length)];
            }
            nodes[i].transform.localPosition = p;
        }

        // Frenado progresivo
        if (slowingDown)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);
            if (currentSpeed <= 0.3f && !aligning)
            {
                aligning = true;
                StartCoroutine(AlignAndSnap());
            }
        }
    }

    public void StartSpin(float startSpeed)
    {
        if (symbols == null || symbols.Length == 0)
        {
            Debug.LogWarning($"[ReelMotion] {name} no tiene símbolos asignados.");
            return;
        }

        isSpinning = true;
        slowingDown = false;
        aligning = false;
        currentSpeed = startSpeed;
    }

    public void StartSlowDown()
    {
        if (isSpinning) slowingDown = true;
    }

    public bool IsStopped() => !isSpinning && !slowingDown && !aligning;

    IEnumerator AlignAndSnap()
    {
        // 1) Calcula offset para llevar el símbolo más cercano a y=0
        float targetOffset = CalcAlignOffset();

        // 2) Suaviza el ajuste (feeling tragaperras)
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(1f, 0f, t / dur); // frena suave
            for (int i = 0; i < nodes.Length; i++)
            {
                var p = nodes[i].transform.localPosition;
                p.y += targetOffset * Time.deltaTime * 6f * k;
                nodes[i].transform.localPosition = p;
            }
            yield return null;
        }

        // 3) Snap exacto y normalización (+spacing, 0, -spacing)
        SnapToGrid();

        isSpinning = false;
        slowingDown = false;
        aligning = false;
    }

    float CalcAlignOffset()
    {
        // Busca el sprite más cercano al centro (y=0)
        float bestDist = float.MaxValue;
        float offset = 0f;

        foreach (var n in nodes)
        {
            if (!n) continue;
            float y = n.transform.localPosition.y;
            float d = Mathf.Abs(y);
            if (d < bestDist) { bestDist = d; offset = -y; }
        }
        return offset;
    }

    void SnapToGrid()
    {
        // Ordena por Y descendente (arriba→abajo)
        System.Array.Sort(nodes, (a, b) =>
        {
            float ay = a ? a.transform.localPosition.y : -999f;
            float by = b ? b.transform.localPosition.y : -999f;
            return -ay.CompareTo(by);
        });

        // Reubica a posiciones exactas y re-etiqueta internamente:
        // nodes[0] = top, nodes[1] = center, nodes[2] = bottom
        if (nodes[0]) nodes[0].transform.localPosition = new Vector3(0, spacing, 0);
        if (nodes[1]) nodes[1].transform.localPosition = new Vector3(0, 0, 0);
        if (nodes[2]) nodes[2].transform.localPosition = new Vector3(0, -spacing, 0);
    }

    void RandomizeAll()
    {
        if (symbols == null || symbols.Length == 0) return;
        foreach (var n in nodes)
            if (n) n.sprite = symbols[Random.Range(0, symbols.Length)];
    }

    // === API para el controlador ===
    public Sprite GetCenterSprite()
    {
        // Tras SnapToGrid(), nodes[1] SIEMPRE es el del centro
        return nodes[1] ? nodes[1].sprite : null;
    }
}
