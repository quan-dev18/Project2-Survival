using UnityEngine;

[CreateAssetMenu(fileName = "GoldConfig", menuName = "GameConfig/GoldConfig")]
public class GoldConfig : ScriptableObject
{
    [Header("Gold Earned Per Game Over")]
    public int goldPerKill = 10;
    public int goldPerSecond = 1;

    public float GetGoldPerKill()
    {
        float rate = FirebaseRemoteConfigHelper.Instance != null ? FirebaseRemoteConfigHelper.Instance.GoldKillRate : 1.0f;
        return goldPerKill * rate;
    }

    public float GetGoldPerSecond()
    {
        float rate = FirebaseRemoteConfigHelper.Instance != null ? FirebaseRemoteConfigHelper.Instance.GoldTimeRate : 1.0f;
        return goldPerSecond * rate;
    }
}
