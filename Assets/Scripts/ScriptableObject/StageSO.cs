using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageSO", menuName = "StageStats")]
public class StageSO : ScriptableObject
{
    [SerializeField] private List<Phase> phases;
    public List<Phase> Phases => phases;

    [Serializable]
    public class Phase
    {
        [SerializeField] private string phaseName = "Phase";
        public string PhaseName => phaseName;
        [SerializeField] private float duration = 20f; // Timer before moving to the next phase. Ignored for the final phase.
        public float Duration => duration;
        [SerializeField] private List<PhaseEnemy> enemies;
        public List<PhaseEnemy> Enemies => enemies;
    }

    [Serializable]
    public class PhaseEnemy
    {
        [SerializeField] private string mobKey = "Enemy";
        public string MobKey => mobKey;
        [SerializeField] private float spawnPerSecond = 1f;
        public float SpawnPerSecond => spawnPerSecond;
        [SerializeField] private int spawnLimit; // Max total spawns during the phase. 0 = endless.
        public int SpawnLimit => spawnLimit;
        [SerializeField] private bool isBoss; // Spawns instantly once when the phase starts instead of on a timer.
        public bool IsBoss => isBoss;
    }
}
