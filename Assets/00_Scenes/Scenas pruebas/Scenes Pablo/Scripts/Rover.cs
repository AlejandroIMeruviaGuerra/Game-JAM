using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Rover : MonoBehaviour
{
    public GridMap map;
    [Header("Tiempos (más grande = más lento)")]
    public float moveTimePerCell = 0.50f; // subí el valor para que vaya más lento
    public float rotateTime = 0.20f;

    public Facing startFacing = Facing.North;
    public Vector2Int startCell = new(4, 5);
    public Transform modelToRotate; // malla/visual
    public float ySnap = 0.0f;      // altura base (0 si el piso es y=0)

    [Header("Estado runtime")]
    public Facing facing;
    public Vector2Int cell;
    public bool isRunning;

    private CharacterController cc;

    private void Awake() { cc = GetComponent<CharacterController>(); }
    private void Start() { ResetToStart(); }

    public void ResetToStart()
    {
        isRunning = false;
        facing = startFacing;
        cell = startCell;

        var p = map.CellToWorld(cell);
        transform.position = new Vector3(p.x, ySnap, p.z);

        if (modelToRotate)
            modelToRotate.rotation = Quaternion.LookRotation(facing.ToVec().To3(), Vector3.up);
    }

    public IEnumerator RunProgram(Command[] program, System.Action<bool> onFinish)
    {
        isRunning = true;

        foreach (var cmd in program)
        {
            if (cmd == Command.None) continue;

            // Giro si es necesario
            int steps = (cmd == Command.Forward1 || cmd == Command.Forward2) ? 0 :
                        (cmd == Command.Left1 || cmd == Command.Left2) ? -1 : +1;

            if (steps != 0)
            {
                facing = (steps < 0) ? facing.TurnLeft() : facing.TurnRight();
                yield return RotateTo(facing);
            }

            // Avance 1 o 2 celdas
            int moveCount = cmd switch
            {
                Command.Forward1 => 1,
                Command.Forward2 => 2,
                Command.Left1 => 1,
                Command.Left2 => 2,
                Command.Right1 => 1,
                Command.Right2 => 2,
                _ => 0
            };

            for (int i = 0; i < moveCount; i++)
            {
                var next = cell + facing.ToVec();
                if (map.IsBlocked(next))
                {
                    isRunning = false;
                    onFinish?.Invoke(false); // choque/limite
                    yield break;
                }
                yield return MoveTo(next);
                cell = next;
            }
        }

        isRunning = false;
        onFinish?.Invoke(true); // terminó sin chocar (verifica meta aparte)
    }

    IEnumerator RotateTo(Facing f)
    {
        if (!modelToRotate) yield break;
        Quaternion a = modelToRotate.rotation;
        Quaternion b = Quaternion.LookRotation(f.ToVec().To3(), Vector3.up);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateTime;
            modelToRotate.rotation = Quaternion.Slerp(a, b, t);
            yield return null;
        }
    }

    IEnumerator MoveTo(Vector2Int targetCell)
    {
        Vector3 a = transform.position;
        Vector3 b = map.CellToWorld(targetCell);
        b.y = ySnap;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / moveTimePerCell;
            Vector3 p = Vector3.Lerp(a, b, t);
            cc.Move(p - transform.position);
            yield return null;
        }

        // Snap final
        var end = map.CellToWorld(targetCell); end.y = ySnap;
        cc.enabled = false; transform.position = end; cc.enabled = true;
    }

    // ===== DEBUG opcional: muestra la celda actual en pantalla =====
    void OnGUI()
    {
        // comenta si no lo necesitas
        if (!map) return;
        var c = map.WorldToCell(transform.position);
        GUI.Label(new Rect(10, 10, 260, 22), $"Robot cell: {c.x},{c.y}");
    }
}

static class VecExt
{
    public static Vector3 To3(this Vector2Int v) => new Vector3(v.x, 0, v.y);
}
