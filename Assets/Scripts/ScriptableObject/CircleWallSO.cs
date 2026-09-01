using UnityEngine;

[CreateAssetMenu(fileName = "CircleWallSO", menuName = "Config/Circle Wall")]
public class CircleWallSO : ScriptableObject
{
    [Header("Activation")]
    [SerializeField] private int activationWave = 2;
    public int ActivationWave => activationWave;

    [Header("Circle Settings")]
    [SerializeField] private float startRadius = 20f;
    public float StartRadius => startRadius;
    [SerializeField] private float endRadius = 3f;
    public float EndRadius => endRadius;
    [SerializeField] private float shrinkDuration = 60f;
    public float ShrinkDuration => shrinkDuration;

    [Header("Enemy Settings")]
    [SerializeField] private int wallEnemyCount = 30;
    public int WallEnemyCount => wallEnemyCount;
    [SerializeField] private string wallEnemyKey = "Enemy1";
    public string WallEnemyKey => wallEnemyKey;
    [SerializeField] private float wallEnemyHP = 250f;
    public float WallEnemyHP => wallEnemyHP;
    [SerializeField] private float wallEnemyDamage = 10f;
    public float WallEnemyDamage => wallEnemyDamage;
}
