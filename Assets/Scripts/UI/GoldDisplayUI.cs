using TMPro;
using UnityEngine;

public class GoldDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private string prefix = "";
    [SerializeField] private bool showSessionGold = false;

    private void OnEnable()
    {
        if (UserData.Instance != null)
        {
            if (showSessionGold)
                UserData.Instance.OnSessionGoldChanged += UpdateGold;
            else
                UserData.Instance.OnGoldChanged += UpdateGold;
        }
    }

    private void OnDisable()
    {
        if (UserData.Instance != null)
        {
            if (showSessionGold)
                UserData.Instance.OnSessionGoldChanged -= UpdateGold;
            else
                UserData.Instance.OnGoldChanged -= UpdateGold;
        }
    }

    private void Start()
    {
        if (UserData.Instance != null)
        {
            if (showSessionGold)
                UpdateGold(UserData.Instance.SessionGold);
            else
                UpdateGold(UserData.Instance.Gold);
        }
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
            goldText.text = $"{prefix}{FormatHelper.FormatGold(gold)}";
    }
}
