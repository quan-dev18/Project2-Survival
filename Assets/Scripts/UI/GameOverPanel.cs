using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;

    [SerializeField] private float floatDistance = 40f;
    [SerializeField] private float floatDuration = 1f;

    private Tween titleTween;
    private float titleBaseY;

    private void Awake()
    {
        if (titleText == null)
            titleText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnDestroy()
    {
        KillTween();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (titleText != null)
        {
            bool win = GameManager.Instance != null && GameManager.Instance.IsWin;
            titleText.text = win ? "You Win" : "Game Over";
            titleText.color = win ? Color.yellow : Color.red;
        }

        PlayTitleFloat();
    }

    public void Hide()
    {
        KillTween();
        gameObject.SetActive(false);
    }

    private void PlayTitleFloat()
    {
        if (titleText == null) return;

        titleBaseY = titleText.rectTransform.anchoredPosition.y;
        KillTween();
        titleTween = titleText.rectTransform
            .DOAnchorPosY(titleBaseY + floatDistance, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void KillTween()
    {
        if (titleTween != null)
        {
            titleTween.Kill();
            titleTween = null;
        }

        if (titleText != null)
        {
            Vector2 pos = titleText.rectTransform.anchoredPosition;
            titleText.rectTransform.anchoredPosition = new Vector2(pos.x, titleBaseY);
        }
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameMap1");
    }

    public void BackToHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}