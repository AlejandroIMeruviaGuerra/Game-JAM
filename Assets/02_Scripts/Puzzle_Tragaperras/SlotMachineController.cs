using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotMachineController : MonoBehaviour
{
    [Header("Reels (columnas)")]
    public ReelMotion[] reels;

    [Header("UI")]
    public Button btnStart;
    public Button btnStop;
    public TMP_Text txtStatus;
    public TMP_Text txtResult;

    [Header("Configuración")]
    public float spinSpeed = 14f;

    private bool spinning = false;
    private int nextReelToStop = 0;

    void Start()
    {
        btnStart.onClick.AddListener(StartSpinning);
        btnStop.onClick.AddListener(StopNextReel);

        txtStatus.text = "Pulsa COMENZAR";
        txtResult.text = "";
        btnStop.interactable = false;
    }

    public void StartSpinning()
    {
        if (spinning) return;

        spinning = true;
        nextReelToStop = 0;
        txtStatus.text = "🎰 Girando...";
        txtResult.text = "";
        btnStart.interactable = false;
        btnStop.interactable = true;

        foreach (var reel in reels)
            reel.StartSpin(spinSpeed);
    }

    public void StopNextReel()
    {
        if (!spinning) return;

        if (nextReelToStop < reels.Length)
        {
            txtStatus.text = $"🛑 Deteniendo Reel {nextReelToStop + 1}";
            reels[nextReelToStop].StartSlowDown();
            nextReelToStop++;
        }

        if (nextReelToStop >= reels.Length)
            StartCoroutine(WaitForAllToStop());
    }

    private IEnumerator WaitForAllToStop()
    {
        btnStop.interactable = false;
        yield return new WaitUntil(() => AllStopped());

        spinning = false;
        btnStart.interactable = true;
        txtStatus.text = "✅ ¡Listo!";

        CheckResult();
    }

    private bool AllStopped()
    {
        foreach (var reel in reels)
            if (!reel.IsStopped()) return false;
        return true;
    }

    // --- Comparar por nombre ---
    private void CheckResult()
    {
        Sprite s1 = reels[0].GetCenterSymbol();
        Sprite s2 = reels[1].GetCenterSymbol();
        Sprite s3 = reels[2].GetCenterSymbol();

        if (s1 == null || s2 == null || s3 == null)
        {
            txtResult.text = "😅 Error en símbolos";
            return;
        }

        string n1 = s1.name;
        string n2 = s2.name;
        string n3 = s3.name;

        // 🔹 Comparación por nombre
        if (n1 == n2 && n2 == n3)
        {
            txtResult.text = "🎉 ¡GANASTE!";
            txtStatus.text = "💰 Combinación perfecta";
        }
        else
        {
            txtResult.text = "😅 Vuelve a intentarlo";
            txtStatus.text = "¡Listo!";
        }
    }
}
