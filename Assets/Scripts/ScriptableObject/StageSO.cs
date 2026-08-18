using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageSO", menuName = "StageStats")]
public class StageSO : ScriptableObject
{
    [SerializeField] private List<StageEntry> stages;
    public List<StageEntry> Stages => stages;

    [Serializable]
    public class StageEntry
    {
        [SerializeField] private string mobKey;
        public string MobKey => mobKey;
        [SerializeField] private float duration; // How long this entry lasts before moving to the next
        public float Duration => duration;
        [SerializeField] private float spawnPerSecond;
        public float SpawnPerSecond => spawnPerSecond;
        [SerializeField] private float spawnIncrementAmount; // How much the spawn rate increases each interval
        public float SpawnIncrementAmount => spawnIncrementAmount;
        [SerializeField] private float spawnIncrementInterval; // How often (in seconds) the spawn rate increases
        public float SpawnIncrementInterval => spawnIncrementInterval;
    }
}
