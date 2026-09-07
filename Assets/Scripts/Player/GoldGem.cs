using UnityEngine;

public class GoldGem : XPGem
{
    [SerializeField] private int goldAmount = 1;

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
            UserData.Instance.AddGold(goldAmount);

        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
    }
}
