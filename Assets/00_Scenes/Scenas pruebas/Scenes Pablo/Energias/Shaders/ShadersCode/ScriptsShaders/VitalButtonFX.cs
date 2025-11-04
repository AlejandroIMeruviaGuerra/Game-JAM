using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class VitalButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Material mat;
    private RectTransform rect;
    private Image img;
    private Color originalColor;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (img != null) originalColor = img.color;
        if (mat != null) mat.SetFloat("_LifeFlow", 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
        if (mat != null) mat.DOFloat(0.4f, "_LifeFlow", 0.3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad);
        if (mat != null) mat.DOFloat(0f, "_LifeFlow", 0.5f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOShakeAnchorPos(0.4f, 5f, 10, 90, false, true);
        rect.DOScale(1.25f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            rect.DOScale(1f, 0.3f).SetEase(Ease.InOutQuad);
        });

        if (mat != null)
            StartCoroutine(PulseEffect());
    }

    private System.Collections.IEnumerator PulseEffect()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            float val = Mathf.Lerp(1f, 0f, t);
            mat.SetFloat("_LifeFlow", val);
            yield return null;
        }
    }
}
