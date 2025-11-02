using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PadInteractable : MonoBehaviour
{
    [Header("Índice del pad (0=Rojo, 1=Azul, 2=Amarillo)")]
    public int padIndex = 0;

    [Header("Entrada")]
    public KeyCode interactKey = KeyCode.F;

    bool playerInside = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(interactKey))
        {
            var mgr = PuzzleSequenceManager.Instance;
            if (mgr == null) return;

            // feedback inmediato (parpadeo corto en este pad)
            mgr.PulsePad(padIndex, 0.18f);

            // registrar la pulsación para validar contra la secuencia
            mgr.RegisterPlayerPress(padIndex);
        }
    }
}
