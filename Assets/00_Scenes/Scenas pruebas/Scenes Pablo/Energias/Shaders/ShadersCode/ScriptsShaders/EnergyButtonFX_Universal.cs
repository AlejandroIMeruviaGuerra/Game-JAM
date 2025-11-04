using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class EnergyButtonFX_Universal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Shader Control")]
    public Material mat;
    public string pulseProperty = "_Pulse";   // Nombre del parámetro del shader
    public string explosionProperty = "_Explosion"; // Alternativo (para fuego)

    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float clickScale = 1.25f;
    public float shakeStrength = 6f;
    public float shakeDuration = 0.25f;

    private RectTransform rect;
    private bool isHovered = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (mat != null)
        {
            if (mat.HasProperty(pulseProperty)) mat.SetFloat(pulseProperty, 0);
            if (mat.HasProperty(explosionProperty)) mat.SetFloat(explosionProperty, 0);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        rect.DOKill();
        rect.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack);

        if (mat != null)
        {
            if (mat.HasProperty(pulseProperty)) mat.DOFloat(0.5f, pulseProperty, 0.3f);
            if (mat.HasProperty(explosionProperty)) mat.DOFloat(0.3f, explosionProperty, 0.3f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        rect.DOKill();
        rect.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);

        if (mat != null)
        {
            if (mat.HasProperty(pulseProperty)) mat.DOFloat(0, pulseProperty, 0.5f);
            if (mat.HasProperty(explosionProperty)) mat.DOFloat(0, explosionProperty, 0.5f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.DOKill();

        // Pequeña vibración al presionar
        rect.DOShakeAnchorPos(shakeDuration, shakeStrength, 10, 100, false, true);
        rect.DOScale(clickScale, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            rect.DOScale(isHovered ? hoverScale : 1f, 0.3f).SetEase(Ease.OutBack);
        });

        // Activar el parámetro correspondiente (Pulse o Explosion)
        if (mat != null)
        {
            if (mat.HasProperty(pulseProperty))
                StartCoroutine(ShaderPulse(mat, pulseProperty));
            if (mat.HasProperty(explosionProperty))
                StartCoroutine(ShaderPulse(mat, explosionProperty));
        }
    }

    private System.Collections.IEnumerator ShaderPulse(Material m, string property)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            float val = Mathf.Lerp(1f, 0f, t);
            m.SetFloat(property, val);
            yield return null;
        }
    }
}
