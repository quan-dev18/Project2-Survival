using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSO", menuName = "CharacterStats")]
public class CharacterSO : ScriptableObject
{
    [SerializeField] private float maxHealth;
    public float MaxHealth => maxHealth;
    [SerializeField] private float maxArmor;
    public float MaxArmor => maxArmor;
    [SerializeField] private float recoveryRate; // The rate at which health recovers over time
    public float RecoveryRate => recoveryRate;
    [SerializeField] private float movementSpeed;
    public float MovementSpeed => movementSpeed;
    [SerializeField] private float collectRange;
    public float CollectRange => collectRange;
    [SerializeField] private float growthRate; // The rate at which the character levels up
    public float GrowthRate => growthRate;

}
