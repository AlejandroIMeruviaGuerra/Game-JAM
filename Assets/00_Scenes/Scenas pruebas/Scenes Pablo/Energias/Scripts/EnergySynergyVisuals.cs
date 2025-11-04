using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class EnergySynergyVisuals : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI synergyText;
    public Transform synergyFXSpawnPoint;

    [Header("Configuración visual")]
    public float textDuration = 1.8f;
    public float textScale = 1.3f;
    public float fadeTime = 0.4f;

    [Header("FX combinados")]
    public GameObject fireWaterFX;    // Tormenta Ígnea
    public GameObject sunNatureFX;    // Fusión Vital
    public GameObject lightDarkFX;    // Eclipse Total

    private Coroutine activeRoutine;

    public void ShowSynergy(string synergyKey)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlaySynergyVisual(synergyKey));
    }

    IEnumerator PlaySynergyVisual(string key)
    {
        GameObject fx = null;
        string synergyName = "";

        switch (key)
        {
            case "Blue-Red":
                fx = fireWaterFX;
                synergyName = "⚡ Tormenta Ígnea!";
                break;

            case "Red-Green":
                fx = sunNatureFX;
                synergyName = "🔥 Fusión Vital!";
                break;

            case "Purple-Yellow":
                fx = lightDarkFX;
                synergyName = "🌑 Eclipse Total!";
                break;

            default:
                yield break;
        }

        if (synergyText != null)
        {
            synergyText.text = synergyName;
            synergyText.alpha = 0;
            synergyText.gameObject.SetActive(true);
            synergyText.transform.localScale = Vector3.one * 0.5f;

            // Escala + fade in
            synergyText.DOFade(1, fadeTime);
            synergyText.transform.DOScale(textScale, 0.3f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(textDuration);

            // Fade out
            synergyText.DOFade(0, fadeTime).OnComplete(() =>
            {
                synergyText.gameObject.SetActive(false);
            });
        }

        // Instanciar partículas de sinergia
        if (fx != null && synergyFXSpawnPoint != null)
            Instantiate(fx, synergyFXSpawnPoint.position, Quaternion.identity);
        // 🔥 Nuevo: conectar con fondo pulsante si existe
    
        var bgPulse = FindObjectOfType<EnergyBackgroundPulse>();
        if (bgPulse != null)
        {
            switch (key)
            {
                case "Blue-Red": // Tormenta Ígnea
                    bgPulse.SetEnergyColor(new Color(0.2f, 0.5f, 1f), new Color(1f, 0.3f, 0.1f));
                    break;
                case "Red-Green": // Fusión Vital
                    bgPulse.SetEnergyColor(new Color(1f, 0.3f, 0.1f), new Color(0.2f, 1f, 0.3f));
                    break;
                case "Purple-Yellow": // Eclipse Total
                    bgPulse.SetEnergyColor(new Color(0.6f, 0.2f, 0.8f), new Color(1f, 0.9f, 0.2f));
                    break;
            }
            bgPulse.SetPulseLevel(1f);
        }


        // Pequeño “shake” para impacto
        Camera.main.transform.DOShakePosition(0.4f, 0.4f, 12, 90);
    }
}
