using System;

public static class GameEvents
{
    // Disparados por el minijuego 2D:
    public static Action OnTTTWin;
    public static Action OnTTTLose;

    // Disparado al abrir/cerrar el minijuego:
    public static Action<bool> OnTTTActiveChanged; // true = activo; false = cerrado

    public static void RaiseWin() => OnTTTWin?.Invoke();
    public static void RaiseLose() => OnTTTLose?.Invoke();
    public static void RaiseActive(bool active) => OnTTTActiveChanged?.Invoke(active);
}
