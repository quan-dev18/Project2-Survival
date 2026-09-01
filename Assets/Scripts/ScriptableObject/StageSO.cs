using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageSO", menuName = "StageStats")]
public class StageSO : ScriptableObject
{
    [Header("Global Stat Scaling (applied over entire stage)")]
    [SerializeField] private float attackIncrementPercent = 0f;
    [SerializeField] private float speedIncrementPercent = 0f;
    [SerializeField] private float healthIncrementPercent = 0f;
    [SerializeField] private float statsIncrementInterval = 90f;

    [SerializeField] private List<Phase> phases;
    public List<Phase> Phases => phases;

    public float AttackIncrementPercent => attackIncrementPercent;
    public float SpeedIncrementPercent => speedIncrementPercent;
    public float HealthIncrementPercent => healthIncrementPercent;
    public float StatsIncrementInterval => statsIncrementInterval;

    [Serializable]
    public class Phase
    {
        [SerializeField] private string phaseName = "Phase";
        public string PhaseName => phaseName;
        [SerializeField] private float duration = 20f;
        public float Duration => duration;
        [SerializeField] private List<PhaseEnemy> enemies;
        public List<PhaseEnemy> Enemies => enemies;
        [SerializeField] private CircleWallConfig circleWall;
        public CircleWallConfig CircleWall => circleWall;
    }

    [Serializable]
    public class PhaseEnemy
    {
        [SerializeField] private string mobKey = "Enemy";
        public string MobKey => mobKey;
        [SerializeField] private float spawnPerSecond = 1f;
        public float SpawnPerSecond => spawnPerSecond;
        [SerializeField] private int spawnLimit;
        public int SpawnLimit => spawnLimit;
        [SerializeField] private bool isBoss;
        public bool IsBoss => isBoss;
    }

    [Serializable]
    public class CircleWallConfig
    {
        [SerializeField] private bool enabled = false;
        public bool Enabled => enabled;
        [SerializeField] private float startRadius = 20f;
        public float StartRadius => startRadius;
        [SerializeField] private float endRadius = 3f;
        public float EndRadius => endRadius;
        [SerializeField] private float shrinkDuration = 60f;
        public float ShrinkDuration => shrinkDuration;
        [SerializeField] private int wallEnemyCount = 30;
        public int WallEnemyCount => wallEnemyCount;
        [SerializeField] private string wallEnemyKey = "Enemy1";
        public string WallEnemyKey => wallEnemyKey;
        [SerializeField] private float wallEnemyHP = 250f;
        public float WallEnemyHP => wallEnemyHP;
        [SerializeField] private float wallEnemyDamage = 10f;
        public float WallEnemyDamage => wallEnemyDamage;
    }
}
