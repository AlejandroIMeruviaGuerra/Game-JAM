using UnityEngine;

[ExecuteAlways]
public class GridAnchor : MonoBehaviour
{
    public GridMap map;
    public Vector2Int cell;
    public bool snapOnValidate = true;

    private void OnValidate() { if (snapOnValidate) Snap(); }
    private void Reset() { Snap(); }

    public void Snap()
    {
        if (!map) return;
        Vector3 p = map.CellToWorld(cell);
        p.y = transform.position.y; // respeta altura actual (cámbialo si quieres)
        transform.position = p;
    }
}
