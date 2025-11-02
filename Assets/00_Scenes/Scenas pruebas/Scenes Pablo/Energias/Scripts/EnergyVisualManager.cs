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
}
