using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SolarButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Material mat;
    private RectTransform rect;
    private Image img;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (mat != null) mat.SetFloat("_Pulse", 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack);
        if (mat != null) mat.DOFloat(0.3f, "_Pulse", 0.3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad);
        if (mat != null) mat.DOFloat(0f, "_Pulse", 0.5f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mat != null)
            StartCoroutine(SolarBurst());

        rect.DOShakeAnchorPos(0.3f, 6f, 20, 90, false, true);
        rect.DOScale(1.3f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            rect.DOScale(1f, 0.3f).SetEase(Ease.InOutQuad);
        });
    }

    private System.Collections.IEnumerator SolarBurst()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            float val = Mathf.Lerp(1.2f, 0f, t);
            mat.SetFloat("_Pulse", val);
            yield return null;
        }
    }
}
