using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Flags]
public enum Conn { None = 0, Up = 1, Right = 2, Down = 4, Left = 8 }

public enum TileType { Empty, Straight, Corner, TJunction, Cross, Source, Target, Block }

public class CircuitTile : MonoBehaviour, IPointerClickHandler
{
    [Header("Config")]
    public TileType type = TileType.Straight;
    [Tooltip("Cuántas veces rotado 90° CW (0..3)")]
    public int rotationSteps = 0;
    public bool lockedRotation = false;

    [Header("UI")]
    public Image bg;        // asigna la Image del Button
    public Image icon;      // opcional si pones un ícono por tipo (puedes dejar null)

    [Header("Runtime (read)")]
    public bool isPowered = false;  // lo setea el solver
    public int x, y;                // lo setea el controlador

    // Conexiones base por tipo (sin rotación)
    public Conn BaseMask
    {
        get
        {
            switch (type)
            {
                case TileType.Empty: return Conn.None;
                case TileType.Block: return Conn.None;
                case TileType.Straight: return Conn.Up | Conn.Down;        // | Right-Left si lo rotas
                case TileType.Corner: return Conn.Up | Conn.Right;
                case TileType.TJunction: return Conn.Up | Conn.Right | Conn.Left;
                case TileType.Cross: return Conn.Up | Conn.Right | Conn.Down | Conn.Left;
                case TileType.Source: return Conn.Right; // por defecto, “sale” hacia la derecha
                case TileType.Target: return Conn.Left;  // por defecto, “entra” por la izquierda
                default: return Conn.None;
            }
        }
    }

    public Conn WorldMask
    {
        get { return RotateMask(BaseMask, rotationSteps); }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (lockedRotation) return;
        RotateCW();
    }

    public void RotateCW()
    {
        rotationSteps = (rotationSteps + 1) % 4;
        transform.localRotation = Quaternion.Euler(0, 0, -90f * rotationSteps); // UI rota en Z
        OnChanged?.Invoke(this);
    }

    public void SetPowered(bool powered)
    {
        isPowered = powered;
        if (bg)
            bg.color = powered ? new Color(1f, 0.93f, 0.35f, 1f) : Color.white; // amarillo suave
    }

    public System.Action<CircuitTile> OnChanged;

    // --- Helpers ---
    public static Conn RotateMask(Conn m, int stepsCW)
    {
        stepsCW = ((stepsCW % 4) + 4) % 4;
        for (int i = 0; i < stepsCW; i++)
            m = RotateOnce(m);
        return m;
    }

    static Conn RotateOnce(Conn m)
    {
        Conn r = Conn.None;
        if (m.HasFlag(Conn.Up)) r |= Conn.Right;
        if (m.HasFlag(Conn.Right)) r |= Conn.Down;
        if (m.HasFlag(Conn.Down)) r |= Conn.Left;
        if (m.HasFlag(Conn.Left)) r |= Conn.Up;
        return r;
    }

    public static bool AreReciprocallyConnected(Conn aMask, Conn bMask, int ax, int ay, int bx, int by)
    {
        int dx = bx - ax;
        int dy = by - ay;

        if (dx == 1 && dy == 0)
            return aMask.HasFlag(Conn.Right) && bMask.HasFlag(Conn.Left);
        if (dx == -1 && dy == 0)
            return aMask.HasFlag(Conn.Left) && bMask.HasFlag(Conn.Right);
        if (dx == 0 && dy == 1)
            return aMask.HasFlag(Conn.Up) && bMask.HasFlag(Conn.Down);
        if (dx == 0 && dy == -1)
            return aMask.HasFlag(Conn.Down) && bMask.HasFlag(Conn.Up);

        return false;
    }
}

