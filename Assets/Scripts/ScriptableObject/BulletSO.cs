using UnityEngine;

[CreateAssetMenu(fileName = "BulletSO", menuName = "BulletStats")]
public class BulletSO : ScriptableObject
{
    [SerializeField] private float damage = 10f;
    public float Damage => damage;
    [SerializeField] private float speed = 15f;
    public float Speed => speed;
    [SerializeField] private AnimationCurve speedCurve;
    public AnimationCurve SpeedCurve => speedCurve;
    [SerializeField] private float lifeTime = 3f;
    public float LifeTime => lifeTime;
}
