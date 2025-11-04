using UnityEngine;
using System.Collections;

public class AvatarBlink : MonoBehaviour
{
    [Header("Referencias")]
    public CanvasGroup eyesClosedGroup;   // el objeto con la imagen de ojos cerrados

    [Header("Tiempos")]
    public float minBlinkTime = 3f;       // tiempo mínimo entre parpadeos
    public float maxBlinkTime = 6f;       // tiempo máximo entre parpadeos
    public float closeDuration = 0.08f;   // qué tan rápido cierra
    public float holdClosed = 0.05f;      // cuánto se queda cerrado
    public float openDuration = 0.12f;    // qué tan lento abre

    private float timer;

    void Start()
    {
        if (eyesClosedGroup != null)
            eyesClosedGroup.alpha = 0f; // empieza con ojos abiertos

        ScheduleNextBlink();
    }

    void ScheduleNextBlink()
    {
        timer = Random.Range(minBlinkTime, maxBlinkTime);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            StartCoroutine(BlinkRoutine());
            ScheduleNextBlink();
        }
    }

    IEnumerator BlinkRoutine()
    {
        // 1) cerrar (alpha 0 -> 1)
        float t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / closeDuration);
            if (eyesClosedGroup != null) eyesClosedGroup.alpha = a;
            yield return null;
        }

        // 2) mantener cerrado
        yield return new WaitForSeconds(holdClosed);

        // 3) abrir (alpha 1 -> 0)
        t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / openDuration);
            if (eyesClosedGroup != null) eyesClosedGroup.alpha = a;
            yield return null;
        }

        if (eyesClosedGroup != null) eyesClosedGroup.alpha = 0f;
    }
}
