using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private Animator _animator;
    [SerializeField] private float deathFallbackDelay = 2f;
    [Header("Burn VFX")]
    [SerializeField] private GameObject burnVFXPrefab;
    private GameObject activeBurnVFX;
    private Coroutine burnVFXRoutine;

    private Coroutine deathWatchdog;
    private GameObject pooledRoot;
    private SpriteFlashEffect cachedFlashEffect;
    private static readonly WaitForSeconds s_WaitOneSecond = new WaitForSeconds(1f);

    public float CurrentHealth => enemyController.currentHealth;

    public float MaxHealth => enemyController != null ? enemyController.maxHealth : 0f;

    /// <summary>Active while <see cref="EnemySpawner.SpawnEnemy"/> prepares pooled boss data
    /// (global bonuses, color, etc.) and <see cref="OnEnable"/> might have already run
    /// with the old 0-health corpse state. Suppresses invokes until <see cref="OnRewardsDone"/>.</summary>
    [HideInInspector] public bool suppressInvokesUntilAwakeDone;
    /// <summary>While true, damage popups are suppressed (avoids fake crit burst on respawn).</summary>
    [HideInInspector] public bool silenceDamagePopups;
    /// <summary>While true, OnDeath / kill / analytics are suppressed.</summary>
    [HideInInspector] public bool silenceDeathEvents;

    /// <summary>Called once bonuses are applied; applies real HP and re-enables events.</summary>
    public void OnRewardsDone()
    {
        enemyController.ResetHealth();
        silenceDamagePopups = false;
        silenceDeathEvents = false;
        suppressInvokesUntilAwakeDone = false;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
        if(_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        cachedFlashEffect = GetComponent<SpriteFlashEffect>();
    }

    private GameObject GetPooledRoot()
    {
        if (pooledRoot != null) return pooledRoot;

        Transform t = transform;
        while (t != null)
        {
            if (t.TryGetComponent(out PooledObject pooled))
                return pooledRoot = t.gameObject;
            t = t.parent;
        }
        return gameObject;
    }

    private void OnEnable()
    {
        pooledRoot = null;
        if (!suppressInvokesUntilAwakeDone)
            enemyController.ResetHealth();
        Collider2D col = GetComponentInParent<Collider2D>();
        if (col != null) col.enabled = true;
        enemyMovement.enabled = true;
        if (deathWatchdog != null)
        {
            StopCoroutine(deathWatchdog);
            deathWatchdog = null;
        }
    }

    private void OnDisable()
    {
        if (deathWatchdog != null)
        {
            StopCoroutine(deathWatchdog);
            deathWatchdog = null;
        }
        if (burnVFXRoutine != null)
        {
            StopCoroutine(burnVFXRoutine);
            burnVFXRoutine = null;
        }
        if (activeBurnVFX != null)
        {
            activeBurnVFX.SetActive(false);
        }
    }

    public void TakeDamage(float amount)
    {
        // Late hit on a despawned enemy: ignore (prevents double death rewards).
        // Pooled respawns may briefly silence health to skip the 0->full "fake heal"
        // OnEnable burst: in that window this is a no-op, lost on purpose (no salt,
        // just a dummy 0-hit from the object's corpse state).
        if (!gameObject.activeInHierarchy) return;
        if (suppressInvokesUntilAwakeDone) return;
        if (CurrentHealth <= 0f) return;
        if (cachedFlashEffect != null && cachedFlashEffect.gameObject.activeInHierarchy)
            cachedFlashEffect.Flash();
        float health = Mathf.Max(0f, CurrentHealth - amount);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);

        // Fake-burst popups are suppressed while pooled respawn is still installing
        // real bonuses (see OnRewardsDone). Without this, pooled bosses flash a
        // spurious 30-70% damage popup immediately after spawn.
        if (!silenceDamagePopups && PopUpManager.Instance != null)
            PopUpManager.Instance.Show(transform.position, amount, PopupType.Damage);

        if (!silenceDeathEvents && health <= 0f)
        {
            Die();
        }
            
    }

    public void Heal(float amount)
    {
        float health = Mathf.Min(CurrentHealth + amount, enemyController.maxHealth);
        enemyController.SetCurrentHealth(health);
        OnHealthChanged?.Invoke(health, enemyController.maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();

        if (enemyMovement != null)
        {
            enemyMovement.enabled = false;
            enemyMovement.ResetKnockback(); // corpses must not slide on banked hits
        }
        Collider2D col = GetComponentInParent<Collider2D>();
        if (col != null) col.enabled = false;
        _animator.SetBool("isDead",true);
        // Hide burn VFX on death
        if (burnVFXRoutine != null) { StopCoroutine(burnVFXRoutine); burnVFXRoutine = null; }
        if (activeBurnVFX != null) { activeBurnVFX.SetActive(false); }
        deathWatchdog = StartCoroutine(DeathWatchdog());
    }

    private IEnumerator DeathWatchdog()
    {
        yield return new WaitForSeconds(deathFallbackDelay);
        deathWatchdog = null;
        if (gameObject.activeInHierarchy)
            OnDeathAnimationEnd();
    }

    public void OnDeathAnimationEnd()
    {
        if (!gameObject.activeInHierarchy) return;

        if (deathWatchdog != null)
        {
            StopCoroutine(deathWatchdog);
            deathWatchdog = null;
        }

        DropXP();
        _animator.SetBool("isDead",false);
        if (ObjectPooling.Instance != null)
        {
            ObjectPooling.Instance.Despawn(GetPooledRoot());
        }
    }

    public void OnAttackAnimEnd()
    {
        if (enemyMovement != null)
            enemyMovement.ResetAttack();
    }

    public void ShowBurnVFX(float duration)
    {
        // Tôn trọng cờ "Hiển thị VFX": tắt thì không spawn hiệu ứng cháy.
        if (GameSettingsManager.Instance != null && !GameSettingsManager.Instance.ShowVFX)
            return;
        if (burnVFXPrefab == null) return;
        if (activeBurnVFX != null)
        {
            if (burnVFXRoutine != null) StopCoroutine(burnVFXRoutine);
        }
        else
        {
            activeBurnVFX = Instantiate(burnVFXPrefab, transform);
            activeBurnVFX.transform.localPosition = Vector3.zero;
            activeBurnVFX.transform.localRotation = Quaternion.identity;
            activeBurnVFX.transform.localScale = Vector3.one;
        }
        // Ensure VFX actually plays
        activeBurnVFX.SetActive(true);
        foreach (var ps in activeBurnVFX.GetComponentsInChildren<ParticleSystem>(true))
        {
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.sortingLayerName = "Enemy";
                rend.sortingOrder = 10;
            }
            ps.Clear(true);
            ps.Play(true);
        }
        foreach (var anim in activeBurnVFX.GetComponentsInChildren<Animator>(true))
        {
            anim.enabled = true;
            anim.Rebind();
            anim.Update(0f);
        }
        burnVFXRoutine = StartCoroutine(BurnVFXTimer(duration));
    }

    private IEnumerator BurnVFXTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (activeBurnVFX != null) activeBurnVFX.SetActive(false);
        burnVFXRoutine = null;
    }

    private void OnDestroy()
    {
        if (activeBurnVFX != null)
        {
            Destroy(activeBurnVFX);
            activeBurnVFX = null;
        }
    }

    private void DropXP()
    {
        if (ObjectPooling.Instance == null || enemyController == null) return;

        GameObject gem = ObjectPooling.Instance.Spawn("XPGem", transform.position, Quaternion.identity);
        if (gem != null && gem.TryGetComponent(out XPGem xpGem))
            xpGem.SetAmount(enemyController.xpReward);
    }
}