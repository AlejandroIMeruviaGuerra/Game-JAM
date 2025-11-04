using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ChaosButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Material mat;
    private RectTransform rect;
    private Image img;
    private bool isChaosActive = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (mat != null) mat.SetFloat("_Pulse", 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isChaosActive = true;
        rect.DOKill();
        rect.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
        StartCoroutine(RandomChaos());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isChaosActive = false;
        rect.DOKill();
        rect.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
        if (mat != null) mat.DOFloat(0, "_Pulse", 0.5f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOShakeAnchorPos(0.4f, 8f, 25, 100, false, true);
        rect.DOScale(1.3f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            rect.DOScale(1f, 0.3f).SetEase(Ease.InOutQuad);
        });
        if (mat != null) StartCoroutine(PulseBurst());
    }

    private System.Collections.IEnumerator PulseBurst()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float val = Mathf.Lerp(1.2f, 0f, t);
            mat.SetFloat("_Pulse", val);
            yield return null;
        }
    }

    private System.Collections.IEnumerator RandomChaos()
    {
        while (isChaosActive && mat != null)
        {
            float pulse = Random.Range(0.2f, 0.8f);
            mat.SetFloat("_Pulse", pulse);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }
}
