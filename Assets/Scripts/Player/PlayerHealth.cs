using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerStats playerStats;

    public float CurrentHealth => playerStats != null ? playerStats.CurrentHealth : 0f;

    public void TakeDamage(float amount)
    {
        DealDamage(amount);
    }

    public void DealDamage(float amount)
    {
        if (playerStats == null) return;
        playerStats.TakeDamage(amount);

        CameraShake.Shake(0.3f, 0.2f);

        if (PopUpManager.Instance != null)
            PopUpManager.Instance.Show(transform.position, amount, PopupType.PlayerDamage);

        if (playerStats.CurrentHealth <= 0f)
        {
            // Log player died event
            float timeAlive = GameManager.Instance != null ? GameManager.Instance.TotalElapsedTime : 0f;
            int killCount = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            int highestLevel = PlayerXP.Instance != null ? PlayerXP.Instance.CurrentLevel : 1;
            string stageId = PlayerPrefs.GetString("SelectedMapIndex", "0");
            FirebaseAnalyticsHelper.LogPlayerDied(timeAlive, killCount, highestLevel, stageId);

            Debug.Log("Player died");
            GameManager.Instance?.SetState(GameState.GameOver);
        }
    }

}
