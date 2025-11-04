using UnityEngine;
using UnityEngine.Events;

public class InteractableScreen : MonoBehaviour
{
    public Canvas puzzleCanvas;               // asigna el Canvas del puzzle
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.0f;
    public Transform player;

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    bool open = false;

    void Start()
    {
        if (puzzleCanvas) puzzleCanvas.enabled = false;
    }

    void Update()
    {
        if (!player) return;

        float d = Vector3.Distance(player.position, transform.position);
        if (d <= interactDistance && Input.GetKeyDown(interactKey))
        {
            TogglePuzzle(true);
        }
        // Puedes agregar una tecla para cerrar manualmente
        if (open && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePuzzle(false);
        }
    }

    public void TogglePuzzle(bool state)
    {
        open = state;
        if (puzzleCanvas) puzzleCanvas.enabled = state;

        // TODO: Deshabilitar/habilitar input del jugador aquí según tu controlador
        if (state) onOpened?.Invoke(); else onClosed?.Invoke();
    }
}
