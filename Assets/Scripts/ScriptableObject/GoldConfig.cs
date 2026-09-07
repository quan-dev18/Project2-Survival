using UnityEngine;

[CreateAssetMenu(fileName = "GoldConfig", menuName = "GameConfig/GoldConfig")]
public class GoldConfig : ScriptableObject
{
    [Header("Gold Earned Per Game Over")]
    public int goldPerKill = 10;
    public int goldPerSecond = 1;
}
