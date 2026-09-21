using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text goldText;

    [Header("Double Reward (assign in Inspector)")]
    [Tooltip("Only this button shows: the base 1x payout is auto-claimed on Show.")]
    [SerializeField] private Button doubleButton;

    [SerializeField] private float floatDistance = 40f;
    [SerializeField] private float floatDuration = 1f;

    private Tween titleTween;
    private float titleBaseY;

    private int pendingGold;
    private bool doubled;

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

        if (GameManager.Instance != null)
        {
            bool win = GameManager.Instance.IsWin;
            titleText.text = win ? "You Win" : "Game Over";
            titleText.color = win ? Color.yellow : Color.red;

            float total = GameManager.Instance.TotalElapsedTime;
            int minutes = Mathf.FloorToInt(total / 60f);
            int seconds = Mathf.FloorToInt(total % 60f);

            if (statsText != null)
                statsText.text = $"Time Alive: {minutes:D2}:{seconds:D2}\nKills: {GameManager.Instance.KillCount}";

            {
                int kills = GameManager.Instance.KillCount;
                int timeSeconds = Mathf.FloorToInt(GameManager.Instance.TotalElapsedTime);
                GoldConfig config = Resources.Load<GoldConfig>("GoldConfig");
                int sessionGold = 0;
                if (config != null)
                    sessionGold = (kills * config.goldPerKill) + (timeSeconds * config.goldPerSecond);
                else
                    sessionGold = kills + timeSeconds;
                if (goldText != null)
                    goldText.text = $"Gold Earned: {sessionGold}";

                // Base 1x payout is auto-claimed; only the Double button remains.
                if (UserData.Instance != null)
                {
                    UserData.Instance.AddSessionGold(sessionGold);
                    UserData.Instance.ClaimSessionGold();
                }
                pendingGold = sessionGold;
                doubled = false;
                SetDoubleVisible(pendingGold > 0);
                RefreshDoubleButton();
            }
        }

        PlayTitleFloat();
    }

    private void Update()
    {
        // The victory ad may finish loading after the panel is already up.
        if (!doubled && doubleButton != null)
            RefreshDoubleButton();
    }

    /// <summary>Claims any residual session gold. Safe to call repeatedly or on exit.</summary>
    public void ClaimGold()
    {
        if (UserData.Instance != null)
            UserData.Instance.ClaimSessionGold();
    }

    /// <summary>Shows the victory rewarded ad; on completion grants +1x on top of the auto-claimed base.</summary>
    public void ClaimDoubleGold()
    {
        if (doubled) return;
        AdManager ads = AdManager.Instance;
        if (ads == null || !ads.IsVictoryRewardedReady)
        {
            Debug.LogWarning("[GameOverPanel] Double-coin ad not ready yet.");
            return;
        }
        SetDoubleInteractable(false);
        ads.ShowVictoryRewardedAd(
            onEarned: () =>
            {
                if (doubled) return;
                doubled = true;
                int bonus = pendingGold;
                if (bonus > 0 && UserData.Instance != null)
                    UserData.Instance.AddGold(bonus); // base 1x already claimed in Show()
                if (goldText != null)
                    goldText.text = $"Gold Earned: {pendingGold + bonus} (Doubled!)";
                SetDoubleVisible(false);
            },
            onFinished: () =>
            {
                // Skipped/failed: base 1x is already secured, just re-enable.
                if (!doubled) SetDoubleInteractable(true);
            });
    }

    private void RefreshDoubleButton()
    {
        if (doubleButton == null) return;
        bool canDouble = !doubled && AdManager.Instance != null && AdManager.Instance.IsVictoryRewardedReady;
        // Grey out only when ads exist but aren't loaded; without AdManager
        // (direct scene testing) leave clickable so the path logs its warning.
        doubleButton.interactable = canDouble || AdManager.Instance == null;
        TMP_Text label = doubleButton.GetComponentInChildren<TMP_Text>();
            if (label != null && pendingGold > 0)
                label.text = $"Claim x2 ({pendingGold * 2})";
    }

    private void SetDoubleInteractable(bool value)
    {
        if (doubleButton != null) doubleButton.interactable = value;
    }

    private void SetDoubleVisible(bool value)
    {
        if (doubleButton != null) doubleButton.gameObject.SetActive(value);
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
        ClaimGold(); // never forfeit held gold by leaving

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetIsWin(false);
            GameManager.Instance.SetState(GameState.Playing);
        }

        LoadingSceneController.targetScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("LoadingScene");
    }

    public void BackToHome()
    {
        Time.timeScale = 1f;
        ClaimGold(); // never forfeit held gold by leaving

        if (SceneManager.GetActiveScene().name == "GameTutorial")
        {
            TutorialController.SetTutorialCompleted(true);
        }

        SceneManager.LoadScene("MainMenu");
    }
}