using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CircuitPuzzleController : MonoBehaviour
{
    [Header("Grid")]
    public int width = 6;
    public int height = 4;
    public RectTransform gridParent;     // Panel con GridLayoutGroup
    public CircuitTile tilePrefab;

    [Header("Eventos")]
    public UnityEvent onPuzzleCompleted;

    [Header("Opciones")]
    public bool randomizeRotationsOnStart = true;

    [Header("Wires (visual)")]
    public bool showWires = true;
    public RectTransform wireContainer;  // hijo de PanelGrid
    public Image wirePrefab;             // el prefab UI (Image rojo)

    private CircuitTile[,] tiles;
    private List<CircuitTile> sources = new List<CircuitTile>();
    private List<CircuitTile> targets = new List<CircuitTile>();

    // Almacén reusable de wires
    private readonly List<Image> wirePool = new List<Image>();
    private int wireUsedThisFrame = 0;

    [TextArea(2, 10)]
    public string[] asciiLevel = new string[]
    {
        "S  -  -  L1  .   .",
        ".  .  L0  -   -   X",
        ".  .  .   .   .   .",
        ".  .  .   .   .   ."
    };

    void Start()
    {
        BuildGridFromASCII();
        SolveAndUpdate();
    }

    void BuildGridFromASCII()
    {
        // Limpia hijos anteriores (tiles)
        foreach (Transform c in gridParent) Destroy(c.gameObject);

        // Resetea wires del contenedor
        if (wireContainer)
        {
            foreach (Transform c in wireContainer) Destroy(c.gameObject);
            wirePool.Clear();
            wireUsedThisFrame = 0;
        }

        var gl = gridParent.GetComponent<GridLayoutGroup>();
        if (gl)
        {
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = width;
        }

        tiles = new CircuitTile[width, height];
        sources.Clear(); targets.Clear();

        for (int y = 0; y < height; y++)
        {
            string row = (y < asciiLevel.Length) ? asciiLevel[y] : "";
            var tokens = row.Split(' '); // separados por espacios
            int tx = 0;

            for (int x = 0; x < width; x++)
            {
                var t = Instantiate(tilePrefab, gridParent);
                t.transform.localRotation = Quaternion.identity;

                t.type = TileType.Empty;
                t.rotationSteps = 0;
                t.lockedRotation = false;

                if (tx < tokens.Length)
                {
                    ParseToken(tokens[tx], out var ty, out var rot);
                    t.type = ty;
                    t.rotationSteps = rot;
                    tx++;
                }

                if (randomizeRotationsOnStart &&
                    t.type != TileType.Source && t.type != TileType.Target && t.type != TileType.Block)
                {
                    t.rotationSteps = Random.Range(0, 4);
                }

                t.x = x; t.y = y;
                t.transform.localRotation = Quaternion.Euler(0, 0, -90f * t.rotationSteps);
                t.OnChanged += OnAnyTileChanged;

                tiles[x, y] = t;

                if (t.type == TileType.Source) sources.Add(t);
                if (t.type == TileType.Target) targets.Add(t);
            }
        }
    }

    void ParseToken(string token, out TileType type, out int rot)
    {
        type = TileType.Empty;
        rot = 0;
        token = token.Trim();

        if (string.IsNullOrEmpty(token) || token == ".") { type = TileType.Empty; return; }

        string head = token;
        int lastDigit = token.Length - 1;
        if (lastDigit >= 0 && char.IsDigit(token[lastDigit]))
        {
            rot = Mathf.Clamp(token[lastDigit] - '0', 0, 3);
            head = token.Substring(0, token.Length - 1);
        }

        switch (head)
        {
            case "#": type = TileType.Block; break;
            case "-": type = TileType.Straight; break;
            case "L": type = TileType.Corner; break;
            case "T": type = TileType.TJunction; break;
            case "+": type = TileType.Cross; break;
            case "S": type = TileType.Source; break;
            case "X": type = TileType.Target; break;
            default: type = TileType.Empty; break;
        }
    }

    void OnAnyTileChanged(CircuitTile _)
    {
        SolveAndUpdate();
    }

    void SolveAndUpdate()
    {
        // 1) Limpia energía
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tiles[x, y].SetPowered(false);

        // 2) BFS desde sources
        Queue<CircuitTile> q = new Queue<CircuitTile>();
        HashSet<CircuitTile> visited = new HashSet<CircuitTile>();

        foreach (var s in sources)
        {
            s.SetPowered(true);
            q.Enqueue(s);
            visited.Add(s);
        }

        while (q.Count > 0)
        {
            var a = q.Dequeue();
            var aMask = a.WorldMask;

            foreach (var b in Neighbors(a.x, a.y))
            {
                if (b.type == TileType.Block || b.type == TileType.Empty) continue;
                var bMask = b.WorldMask;

                if (CircuitTile.AreReciprocallyConnected(aMask, bMask, a.x, a.y, b.x, b.y))
                {
                    if (!b.isPowered) b.SetPowered(true);
                    if (!visited.Contains(b))
                    {
                        visited.Add(b);
                        q.Enqueue(b);
                    }
                }
            }
        }

        // 3) Dibujar wires
        if (showWires) DrawWires();

        // 4) ¿Gané?
        bool allTargetsPowered = true;
        foreach (var t in targets)
            if (!t.isPowered) { allTargetsPowered = false; break; }

        if (allTargetsPowered)
            onPuzzleCompleted?.Invoke();
    }

    IEnumerable<CircuitTile> Neighbors(int x, int y)
    {
        if (x + 1 < width) yield return tiles[x + 1, y];
        if (x - 1 >= 0) yield return tiles[x - 1, y];
        if (y + 1 < height) yield return tiles[x, y + 1];
        if (y - 1 >= 0) yield return tiles[x, y - 1];
    }

    // ========= Wires =========
    void DrawWires()
    {
        if (!wireContainer || !wirePrefab) return;

        wireUsedThisFrame = 0;

        // Recorre pares adyacentes; si ambos están alimentados y conectados, dibuja un segmento
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var a = tiles[x, y];
                if (!a.isPowered) continue;
                var aMask = a.WorldMask;

                // derecha
                if (x + 1 < width)
                {
                    var b = tiles[x + 1, y];
                    if (b.isPowered && CircuitTile.AreReciprocallyConnected(aMask, b.WorldMask, a.x, a.y, b.x, b.y))
                    {
                        PlaceWireBetween(a, b, horizontal: true);
                    }
                }
                // arriba
                if (y + 1 < height)
                {
                    var b = tiles[x, y + 1];
                    if (b.isPowered && CircuitTile.AreReciprocallyConnected(aMask, b.WorldMask, a.x, a.y, b.x, b.y))
                    {
                        PlaceWireBetween(a, b, horizontal: false);
                    }
                }
            }
        }

        // Desactiva sobrantes del pool
        for (int i = wireUsedThisFrame; i < wirePool.Count; i++)
            wirePool[i].gameObject.SetActive(false);
    }

    void PlaceWireBetween(CircuitTile a, CircuitTile b, bool horizontal)
    {
        var wire = GetWireFromPool();
        wire.gameObject.SetActive(true);

        // Posición
        var ca = (RectTransform)a.transform;
        var cb = (RectTransform)b.transform;

        Vector3 posA = ca.localPosition;
        Vector3 posB = cb.localPosition;
        Vector3 mid = (posA + posB) * 0.5f;

        var wrect = (RectTransform)wire.transform;
        wrect.SetParent(wireContainer, false);
        wrect.localPosition = mid;

        // Tamaño + rotación (toma el ancho de la celda x)
        float cellX = ((RectTransform)a.transform).rect.width;
        float thickness = Mathf.Max(8f, cellX * 0.1f); // grosor
        wrect.sizeDelta = horizontal ? new Vector2(cellX, thickness) : new Vector2(thickness, cellX);
        wrect.localRotation = Quaternion.identity;
    }

    Image GetWireFromPool()
    {
        if (wireUsedThisFrame < wirePool.Count)
            return wirePool[wireUsedThisFrame++];

        var inst = Instantiate(wirePrefab, wireContainer);
        wirePool.Add(inst);
        wireUsedThisFrame++;
        return inst;
    }
}
