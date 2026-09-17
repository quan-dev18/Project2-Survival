using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the "Watch ads" Button (MainMenu shop tab). Forwards clicks to
/// the rewarded ad and greys out while no ad is ready.
/// </summary>
[RequireComponent(typeof(Button))]
public class WatchAdsButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    private void Update()
    {
        if (button != null)
            button.interactable = AdManager.Instance != null && AdManager.Instance.IsRewardedReady;
    }

    private void OnClick()
    {
        if (AdManager.Instance != null)
            AdManager.Instance.ShowRewardedAd();
    }
}
