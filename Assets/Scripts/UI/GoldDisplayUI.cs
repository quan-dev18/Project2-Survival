using TMPro;
using UnityEngine;

public class GoldDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private string prefix = "";

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += UpdateGold;
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= UpdateGold;
    }

    private void Start()
    {
        if (GoldManager.Instance != null)
            UpdateGold(GoldManager.Instance.Gold);
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
            goldText.text = $"{prefix}{gold}";
    }
}
