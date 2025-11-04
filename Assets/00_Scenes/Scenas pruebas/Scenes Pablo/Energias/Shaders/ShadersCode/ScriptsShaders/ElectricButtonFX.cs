using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ElectricButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Material mat;
    private RectTransform rect;

    [Header("Animación")]
    public float hoverScale = 1.1f;
    public float clickScale = 1.25f;
    public float shakeStrength = 4f;
    public float shakeDuration = 0.2f;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (mat != null) mat.SetFloat("_Pulse", 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack);
        if (mat != null) mat.DOFloat(0.5f, "_Pulse", 0.3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
        if (mat != null) mat.DOFloat(0, "_Pulse", 0.4f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOShakeAnchorPos(shakeDuration, shakeStrength, 15, 90, false, true);
        rect.DOScale(clickScale, 0.15f).SetEase(Ease.OutQuad)
            .OnComplete(() => rect.DOScale(hoverScale, 0.25f).SetEase(Ease.OutBack));

        if (mat != null)
            StartCoroutine(PulseEffect());
    }

    private System.Collections.IEnumerator PulseEffect()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            mat.SetFloat("_Pulse", Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
    }
}
