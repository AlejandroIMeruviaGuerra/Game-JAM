using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HeatButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Material mat;
    private RectTransform rect;
    private Image img;
    private Color originalColor;

    [Header("Animación")]
    public float hoverScale = 1.15f;
    public float clickScale = 1.3f;
    public float shakeStrength = 7f;
    public float shakeDuration = 0.25f;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (img != null) originalColor = img.color;
        if (mat != null) mat.SetFloat("_Explosion", 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack);
        if (mat != null) mat.DOFloat(0.3f, "_Explosion", 0.3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
        if (mat != null) mat.DOFloat(0, "_Explosion", 0.4f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.DOKill();

        // Efecto de golpe y rebote
        rect.DOShakeAnchorPos(shakeDuration, shakeStrength, 10, 90, false, true);
        rect.DOScale(clickScale, 0.15f).SetEase(Ease.OutQuad)
            .OnComplete(() => rect.DOScale(hoverScale, 0.25f).SetEase(Ease.OutBack));

        if (mat != null)
            StartCoroutine(ExplosionPulse());
    }

    private System.Collections.IEnumerator ExplosionPulse()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3.5f;
            mat.SetFloat("_Explosion", Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
    }
}
