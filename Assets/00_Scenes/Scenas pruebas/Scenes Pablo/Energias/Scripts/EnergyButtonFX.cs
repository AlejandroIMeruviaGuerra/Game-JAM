using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class EnergyButtonFX : MonoBehaviour
{
    private Button btn;
    private RectTransform rect;
    private Image img;
    private Color originalColor;

    [ColorUsage(true, true)] public Color glowColor = new Color(0.3f, 1f, 0.6f);
    public float glowIntensity = 1.6f;

    void Awake()
    {
        btn = GetComponent<Button>();
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        originalColor = img.color;

        btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        AnimatePress();
        AnimateGlow();
    }

    void AnimatePress()
    {
        rect.DOKill();
        rect.localScale = Vector3.one;
        rect.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            rect.DOScale(1f, 0.2f).SetEase(Ease.InQuad);
        });
    }

    void AnimateGlow()
    {
        img.DOColor(glowColor * glowIntensity, 0.1f).OnComplete(() =>
        {
            img.DOColor(originalColor, 0.3f);
        });
    }
}
