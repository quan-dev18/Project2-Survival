using UnityEngine;

[CreateAssetMenu(fileName = "PropDropEntry", menuName = "Props/Drop Entry")]
public class PropDropEntry : ScriptableObject
{
    public string poolKey;
    [Min(1)] public int quantity = 1;
    [Min(0)] public float weight = 1f;
}
