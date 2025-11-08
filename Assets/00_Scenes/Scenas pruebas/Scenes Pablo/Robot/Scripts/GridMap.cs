using System.Collections.Generic;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 8;
    public float cellSize = 1f;

    [Tooltip("Offset adicional sobre la posición del GridBoard (opcional).")]
    public Vector3 origin = Vector3.zero;

    [Header("Bloqueos")]
    [Tooltip("Celdas bloqueadas por lógica (además de colliders).")]
    public List<Vector2Int> blockedCells = new();

    [Tooltip("Capa usada para obstáculos físicos (cubos, paredes, etc.).")]
    public LayerMask obstacleMask; // crea la capa 'Obstacle' y asígnala

    // === Origen real del tablero ===
    public Vector3 BoardOrigin => transform.position + origin;

    public bool IsInside(Vector2Int cell) =>
        cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

    public Vector3 CellToWorld(Vector2Int cell)
        => BoardOrigin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

    public Vector2Int WorldToCell(Vector3 pos)
    {
        var p = pos - BoardOrigin;
        return new Vector2Int(
            Mathf.RoundToInt(p.x / cellSize),
            Mathf.RoundToInt(p.z / cellSize)
        );
    }

    public bool IsBlocked(Vector2Int cell)
    {
        if (!IsInside(cell)) return true;
        if (blockedCells.Contains(cell)) return true;

        // Si hay capa de obstáculos, revisa colliders
        if (obstacleMask.value != 0)
        {
            var center = CellToWorld(cell) + Vector3.up * 0.5f;
            var half = new Vector3(cellSize, 0.4f, cellSize) * 0.45f;
            if (Physics.CheckBox(center, half, Quaternion.identity, obstacleMask))
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // cuadricula
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var w = CellToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(w, new Vector3(cellSize, 0f, cellSize));
            }

        // bloqueadas
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        foreach (var bc in blockedCells)
            Gizmos.DrawCube(CellToWorld(bc), new Vector3(cellSize, 0.01f, cellSize));
    }
}

// === Comandos y dirección ===
public enum Command
{
    None,
    Forward1, Forward2,
    Left1, Left2,
    Right1, Right2
}

public enum Facing { North, East, South, West }

public static class DirUtil
{
    public static Vector2Int ToVec(this Facing f) => f switch
    {
        Facing.North => Vector2Int.up,     // +Z
        Facing.East => Vector2Int.right,  // +X
        Facing.South => Vector2Int.down,   // -Z
        Facing.West => Vector2Int.left,   // -X
        _ => Vector2Int.up
    };

    public static Facing TurnLeft(this Facing f) => (Facing)(((int)f + 3) & 3);
    public static Facing TurnRight(this Facing f) => (Facing)(((int)f + 1) & 3);
}
