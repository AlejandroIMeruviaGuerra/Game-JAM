using TMPro;
using UnityEngine;
using DG.Tweening;

public class AnimatedScore : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private int currentScore = 0;

    public void SetScore(int newScore)
    {
        DOTween.To(() => currentScore, x => {
            currentScore = x;
            scoreText.text = "Score: " + currentScore.ToString();
        }, newScore, 0.8f).SetEase(Ease.OutQuad);
    }
}
