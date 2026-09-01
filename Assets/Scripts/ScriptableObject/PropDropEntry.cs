using UnityEngine;

[CreateAssetMenu(fileName = "PropDropEntry", menuName = "Props/Drop Entry")]
public class PropDropEntry : ScriptableObject
{
    public GameObject prefab;
    [Min(0)] public float weight = 1f;
}
