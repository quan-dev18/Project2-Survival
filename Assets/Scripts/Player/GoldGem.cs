using UnityEngine;

public class GoldGem : XPGem
{
    [SerializeField] private int goldAmount = 1;

    protected override bool UseValueTiers => false; // keep the gold look
    protected override bool AllowShieldPiece => false; // gold never becomes a shield piece

    public void SetGoldAmount(int amount) => goldAmount = amount;

    protected override void Collect()
    {
        if (target == null)
        {
            return;
        }

        if (Vector2.Distance(target.position, transform.position) > ArrivalTolerance)
        {
            Chase();
            return;
        }

        if (UserData.Instance != null)
        {
            PlayerStats ps = target != null ? target.GetComponent<PlayerStats>() : null;
            int amount = ps != null ? ps.GetGoldGainAmount(goldAmount) : goldAmount;
            UserData.Instance.AddGold(amount);

            // Log gold earned event
            int totalSessionGold = UserData.Instance.SessionGold;
            FirebaseAnalyticsHelper.LogGoldEarned(amount, "gem", totalSessionGold);
        }

        PlayCollectSFX();

        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
    }

    /// <summary>
    /// Thu thập vàng dùng tiếng GOLD riêng (cấu hình trong AudioManager)
    /// thay vì tiếng exp mặc định của XPGem.
    /// </summary>
    protected override void PlayCollectSFX()
    {
        AudioManager.Instance?.PlayGoldCollect();
    }
}
