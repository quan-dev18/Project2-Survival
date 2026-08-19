using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    public float CurrentHealth => playerStats != null ? playerStats.CurrentHealth : 0f;

    public void DealDamage(float amount)
    {
        if (playerStats == null) return;
        playerStats.TakeDamage(amount);

        if (PopUpManager.Instance != null)
            PopUpManager.Instance.Show(transform.position, amount, PopupType.PlayerDamage);

        if (playerStats.CurrentHealth <= 0f)
        {
            Debug.Log("Player died");
            GameManager.Instance?.SetState(GameState.GameOver);
        }
    }

}
