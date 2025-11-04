using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;


public class EnergyVisualManager : MonoBehaviour
{
    [Header("Referencias visuales")]
    public Camera mainCamera;
    public TextMeshProUGUI comboText;
    public GameObject comboParticlesPrefab;
    public Material backgroundMaterial;

    [Header("Configuración")]
    public Gradient comboColorGradient;
    public float cameraShakeIntensity = 0.2f;
    public float cameraShakeDuration = 0.2f;

    private Vector3 originalCamPos;

    [Header("Animación del texto")]
    public float comboTextScale = 1.5f;
    public float comboTextDuration = 0.3f;

    public BackgroundFX backgroundFX;

    public EnergyBackgroundPulse bgPulse;

    public TextMeshProUGUI scoreText;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        originalCamPos = mainCamera.transform.position;

        comboText.gameObject.SetActive(false);
    }

    public void TriggerCombo(int comboCount, Vector3 position)
    {
        // Mostrar texto temporal
        comboText.gameObject.SetActive(true);
        comboText.text = "COMBO ×" + comboCount;
        comboText.color = comboColorGradient.Evaluate(Mathf.InverseLerp(1, 10, comboCount));
        StopAllCoroutines();
        StartCoroutine(FadeOutText());

        // Crear partículas
        if (comboParticlesPrefab)
            Instantiate(comboParticlesPrefab, position, Quaternion.identity);

        if (bgPulse != null)
        {
            bgPulse.SetPulseLevel(comboCount * 0.2f);
        }

        AnimateScorePulse();

        AnimateComboText("COMBO ×" + comboCount, comboText.color);
        // Sacudir cámara
        StartCoroutine(ShakeCamera());

        // Cambiar fondo
        if (backgroundMaterial)
            backgroundMaterial.SetColor("_EmissionColor", comboText.color * 2f);
        // Escala inicial pequeña, luego rebota
        comboText.transform.localScale = Vector3.one * 0.5f;
        comboText.transform.DOScale(comboTextScale, comboTextDuration)
            .SetEase(Ease.OutBack);

    }

    public void AnimateComboText(string text, Color color)
    {
        comboText.text = text;
        comboText.color = color;
        comboText.alpha = 0;
        comboText.gameObject.SetActive(true);

        comboText.transform.localScale = Vector3.one * 0.5f;
        comboText.DOFade(1f, 0.2f);
        comboText.transform.DOScale(1.3f, 0.3f).SetEase(Ease.OutBack);
        comboText.transform.DOScale(1f, 0.4f).SetEase(Ease.InOutQuad).SetDelay(0.3f);

        comboText.DOFade(0, 0.5f).SetDelay(1.2f);
    }

    IEnumerator FadeOutText()
    {
        yield return new WaitForSeconds(1.0f);
        comboText.gameObject.SetActive(false);
    }

    IEnumerator ShakeCamera()
    {
        float elapsed = 0;
        while (elapsed < cameraShakeDuration)
        {
            Vector3 offset = Random.insideUnitSphere * cameraShakeIntensity;
            mainCamera.transform.position = originalCamPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.position = originalCamPos;
    }

    public void AnimateScorePulse()
    {
        if (scoreText == null) return;
        scoreText.transform.DOKill();
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform.DOScale(1.3f, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => scoreText.transform.DOScale(1f, 0.3f));
    }

}

