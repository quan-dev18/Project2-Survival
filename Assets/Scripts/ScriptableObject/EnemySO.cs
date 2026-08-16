using UnityEngine;

[CreateAssetMenu(fileName = "EnemeySO", menuName = "EnemeyStats")]
public class EnemySO : ScriptableObject
{
    [SerializeField] private float maxHealth;
    public float MaxHealth => maxHealth;
    [SerializeField] private float movementSpeed;
    public float MovementSpeed => movementSpeed;
    [SerializeField] private float attackDamage;
    public float AttackDamage => attackDamage;
    [SerializeField] private float attackRange;
    public float AttackRange => attackRange;
    [SerializeField] private float attackSpeed;
    public float AttackSpeed => attackSpeed;
    [SerializeField] private EnemyType enemyType;
    public EnemyType EnemyType => enemyType;
    
}

public enum EnemyType
{
    CloseRange,
    LongRange
}
