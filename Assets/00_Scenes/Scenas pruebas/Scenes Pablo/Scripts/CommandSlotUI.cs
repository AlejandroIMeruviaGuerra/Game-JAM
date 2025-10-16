using UnityEngine;
using UnityEngine.UI;

public class CommandSlotUI : MonoBehaviour
{
    public Image frame;     // borde para resaltar foco
    public Text label;      // muestra símbolo y x1/x2

    public Command Current { get; private set; } = Command.None;

    // Orden de ciclo (igual al juego: Up1→Up2→Left1→Left2→Right1→Right2→None)
    static readonly Command[] cycle = new[]
    {
        Command.Forward1, Command.Forward2,
        Command.Left1, Command.Left2,
        Command.Right1, Command.Right2,
        Command.None
    };
    int idx = cycle.Length - 1;

    public void Cycle()
    {
        idx = (idx + 1) % cycle.Length;
        SetCommand(cycle[idx]);
    }

    public void SetCommand(Command c)
    {
        Current = c;
        if (!label) return;

        string s = c switch
        {
            Command.Forward1 => "↑ x1",
            Command.Forward2 => "↑ x2",
            Command.Left1 => "← x1",
            Command.Left2 => "← x2",
            Command.Right1 => "→ x1",
            Command.Right2 => "→ x2",
            _ => "—"
        };
        label.text = s;
    }

    public void SetFocused(bool on)
    {
        if (frame) frame.color = on ? new Color(0, 1, 0, 1) : new Color(1, 1, 1, 0.25f);
    }
}
