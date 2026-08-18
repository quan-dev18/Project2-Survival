using UnityEngine;
using UnityEngine.UI;

public class LevelUpPanel : MonoBehaviour
{
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        GameManager.OnStateChanged += OnGameStateChanged;
        if (continueButton != null)
            continueButton.onClick.AddListener(Continue);
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.LevelUp)
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);
    }

    public void Continue()
    {
        GameManager.Instance.SetState(GameState.Playing);
    }
}