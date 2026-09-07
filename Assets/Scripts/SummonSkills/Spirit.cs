using UnityEngine;
using System.Collections.Generic;

public class Spirit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 3f;
    [SerializeField] private float orbitSpeed = 45f; // degrees per second
    
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireRate = 1f; // attacks per second
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spread = 0f; // degrees
    [SerializeField] private int pierce = 3;
    
    [Header("Holy Upgrades")]
    [SerializeField] private float healPerSecond = 2f;
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float empoweredDamagePercent = 0.15f; // 15%
    [SerializeField] private int empoweredExtraProjectiles = 1;
    [SerializeField] private float empoweredExtraSpread = 5f;
    
    private bool holyHealEnabled;
    private bool holyBurnEnabled;
    private bool empoweredEnabled;

    // Synergy multipliers
    private float synergyDamageMultiplier = 1f;
    private float synergyAspdMultiplier = 1f;
    
    [Header("References")]
    [SerializeField] private string bulletKey = "Bullet";
    [SerializeField] private Transform firePoint;
    
    private Transform playerTransform;
    private float currentAngle;
    private float fireTimer;
    
    // Track active burns to refresh duration instead of stacking
    private static Dictionary<EnemyHealth, float> activeBurns = new Dictionary<EnemyHealth, float>();
    
    private void Awake()
    {
        // Disable collider so it's not targetable
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Ensure SpriteRenderer has a sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.cyan);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sr.color = new Color(0.5f, 1f, 1f, 0.8f); // translucent cyan
        }
    }
    
    private void OnValidate()
    {
        if (firePoint == null)
            firePoint = transform;
    }
    
    public void Initialize(Transform player)
    {
        playerTransform = player;
        transform.position = player.position + Vector3.up * orbitRadius;
        currentAngle = 90f;
        fireTimer = 0f;
    }
    
    public void EnableHolyHeal() => holyHealEnabled = true;
    public void EnableHolyBurn() => holyBurnEnabled = true;
    public void EnableEmpowered() => empoweredEnabled = true;

    public void ApplySynergyMultipliers(float damageMultiplier, float aspdMultiplier)
    {
        synergyDamageMultiplier = damageMultiplier;
        synergyAspdMultiplier = aspdMultiplier;
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // Orbit around player
        currentAngle += orbitSpeed * Time.deltaTime;
        Vector2 orbitPos = new Vector2(
            Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        ) * orbitRadius;
        transform.position = (Vector2)playerTransform.position + orbitPos;
        
        // Holy Heal: regenerate player HP
        if (holyHealEnabled && playerTransform != null)
        {
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null && playerStats.CurrentHealth < playerStats.MaxHealth)
            {
                playerStats.Heal(healPerSecond * Time.deltaTime);
            }
        }
        
        // Fire logic with synergy fire rate
        float currentFireRate = fireRate * synergyAspdMultiplier;
        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / currentFireRate)
        {
            fireTimer = 0f;
            Fire();
        }
    }
    
    private void Fire()
    {
        if (ObjectPooling.Instance == null || firePoint == null) return;
        
        // Find nearest enemy in range
        Vector2 aimDir = FindNearestEnemyDirection(out Transform targetEnemy);
        if (aimDir == Vector2.zero) return;
        
        // Rotate only firePart, not the whole summon
        float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        if (firePoint != null && firePoint != transform)
            firePoint.rotation = Quaternion.Euler(0, 0, aimAngle);
        
        int currentProjectileCount = projectileCount + (empoweredEnabled ? empoweredExtraProjectiles : 0);
        float currentSpread = spread + (empoweredEnabled ? empoweredExtraSpread : 0f);
        
        // Safety: ensure synergy multipliers are positive
        float safeDamageMultiplier = Mathf.Max(0.01f, synergyDamageMultiplier);
        float safeAspdMultiplier = Mathf.Max(0.01f, synergyAspdMultiplier);
        
        float damageMultiplier = (empoweredEnabled ? (1f + empoweredDamagePercent) : 1f) * safeDamageMultiplier;
        
        // Apply synergy to fire rate
        float currentFireRate = fireRate * safeAspdMultiplier;
        
        for (int i = 0; i < currentProjectileCount; i++)
        {
            float currentSpreadVal = currentSpread;
            float spreadAngle = currentSpreadVal == 0f || currentProjectileCount == 1
                ? 0f
                : Mathf.Lerp(-currentSpreadVal, currentSpreadVal, (float)i / (currentProjectileCount - 1));
            
            Vector2 fireDir = Quaternion.Euler(0, 0, spreadAngle) * aimDir;
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, fireDir);
            GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, firePoint.position, bulletRotation);
            
            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                Transform bulletOwner = playerTransform != null ? playerTransform : transform.root;
                bullet.Init(fireDir, bulletOwner,
                    pierce, 1f, damageMultiplier, 0f, 1f, 1f, false, 0f, 0, null);
            }
        }
        
        // Apply burn to targeted enemy immediately on hit (not on kill)
        if (holyBurnEnabled && targetEnemy != null)
        {
            EnemyHealth enemyHealth = targetEnemy.GetComponentInChildren<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = targetEnemy.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.StartCoroutine(BurnEnemy(enemyHealth));
        }
    }
    
    private System.Collections.IEnumerator BurnEnemy(EnemyHealth enemy)
    {
        // If burn already active on this enemy, refresh duration
        if (activeBurns.ContainsKey(enemy))
        {
            activeBurns[enemy] = burnDuration;
            yield break;
        }
        
        activeBurns[enemy] = burnDuration;
        
        float timer = 0f;
        float tickInterval = 1f; // ~6 ticks per second = 6 dmg/s
        float nextTick = 0f;
        
        while (timer < burnDuration && enemy != null && enemy.CurrentHealth > 0f)
        {
            timer += Time.deltaTime;
            nextTick -= Time.deltaTime;
            
            if (nextTick <= 0f)
            {
                nextTick = tickInterval;
                if (enemy != null && enemy.CurrentHealth > 0f)
                    enemy.TakeDamage(6f); // 1 damage per tick = 6 dmg/s
            }
            yield return null;
        }
        
        // Clean up when done
        activeBurns.Remove(enemy);
    }
    
    private Vector2 FindNearestEnemyDirection(out Transform nearestEnemy)
    {
        nearestEnemy = null;
        if (playerTransform == null) return Vector2.zero;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        float closestDist = float.MaxValue;
        
        foreach (Collider2D hit in hits)
        {
            if (hit.transform.IsChildOf(playerTransform)) continue;
            EnemyHealth enemy = hit.GetComponentInChildren<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null) continue;
            
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearestEnemy = hit.transform;
            }
        }
        
        if (nearestEnemy != null)
        {
            return (nearestEnemy.position - transform.position).normalized;
        }
        
        return Vector2.zero;
    }
}