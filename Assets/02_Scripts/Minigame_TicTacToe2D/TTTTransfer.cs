using UnityEngine;

public static class TTTTransfer
{
    public static bool Won = false;
    public static string WinMessage = "";
    public static string LastNPCScene = ""; // ✅ SOLO esta línea nueva

    public static void Clear()
    {
        Won = false;
        WinMessage = "";
    }
}

