using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommandProgram : MonoBehaviour
{
    [Header("Refs")]
    public Rover rover;
    public GridMap map;
    public Transform goal;              // referencia a la meta (o guarda la celda)
    public Camera camEditor;
    public Camera camFollow;
    public RenderTexture followTex;     // asigna a RawImage si quieres “pantalla”
    public Text hintLabel;

    [Header("UI Slots")]
    public CommandSlotUI[] slots;       // arrastra Slot(0..n)

    [Header("Input")]
    public KeyCode keySelectLeft = KeyCode.LeftArrow;
    public KeyCode keySelectRight = KeyCode.RightArrow;
    public KeyCode keyModify = KeyCode.UpArrow; // cicla opciones
    public KeyCode keyClear = KeyCode.Backspace;
    public KeyCode keyAccept = KeyCode.Return;
    public KeyCode keyReset = KeyCode.R;

    int currentIndex = 0;

    void Start()
    {
        FocusSlot(0);
        camEditor.enabled = true;
        camFollow.enabled = false;
        if (hintLabel) hintLabel.text =
            "←/→ mover cursor | ↑ modificar | Enter ejecutar | Backspace borrar | R reiniciar";
    }

    void Update()
    {
        if (rover.isRunning) return;

        if (Input.GetKeyDown(keySelectLeft)) FocusSlot(Mathf.Max(0, currentIndex - 1));
        if (Input.GetKeyDown(keySelectRight)) FocusSlot(Mathf.Min(slots.Length - 1, currentIndex + 1));

        if (Input.GetKeyDown(keyModify)) slots[currentIndex].Cycle();
        if (Input.GetKeyDown(keyClear)) slots[currentIndex].SetCommand(Command.None);
        if (Input.GetKeyDown(keyReset)) { rover.ResetToStart(); ClearAll(); }

        if (Input.GetKeyDown(keyAccept))
        {
            var program = BuildProgram();
            StartCoroutine(Run(program));
        }
    }

    Command[] BuildProgram()
    {
        var list = new List<Command>();
        foreach (var s in slots) list.Add(s.Current);
        return list.ToArray();
    }

    System.Collections.IEnumerator Run(Command[] program)
    {
        // Cambiar cámaras
        camEditor.enabled = false;
        camFollow.enabled = true;

        yield return rover.RunProgram(program, success =>
        {
            // Al terminar, verificar meta
            bool reached = success && map.WorldToCell(rover.transform.position) == map.WorldToCell(goal.position);
            if (hintLabel)
                hintLabel.text = reached ? "¡Éxito! (R para reiniciar)" : "Falló (R para reiniciar)";
        });
    }

    void FocusSlot(int i)
    {
        currentIndex = i;
        for (int k = 0; k < slots.Length; k++)
            slots[k].SetFocused(k == i);
    }

    void ClearAll()
    {
        foreach (var s in slots) s.SetCommand(Command.None);
        if (hintLabel) hintLabel.text =
            "←/→ mover cursor | ↑ modificar | Enter ejecutar | Backspace borrar | R reiniciar";
    }
}
