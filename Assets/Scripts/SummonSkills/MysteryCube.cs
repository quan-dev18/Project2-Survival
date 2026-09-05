using UnityEngine;

public class MysteryCube : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitSpeed = 30f; // degrees per second
    
    [Header("Combat Settings")]
    [SerializeField] private float activationDelay = 120f; // 2 minutes
    [SerializeField] private float baseFireRate = 1f; // attacks per second
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float spread = 50f; // degrees
    [SerializeField] private int pierce = 1;
    
    [Header("Stacking Buffs (apply even during inactive)")]
    [SerializeField] private int dmgStackAmount = 6; // per 30s
    [SerializeField] private float aspdStackPercent = 0.1f; // 10% per 30s
    
    [Header("References")]
    [SerializeField] private string bulletKey = "Bullet";
    [SerializeField] private Transform firePoint;
    
    private Transform playerTransform;
    private float currentAngle;
    private bool isActive;
    private float fireTimer;
    private float stackTimer;
private int dmgStacks;
    private float aspdStacks;

    // Synergy multipliers
    private float synergyDamageMultiplier = 1f;
    private float synergyAspdMultiplier = 1f;
    
    private void Awake()
    {
        // Disable collider so it's not targetable
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Ensure SpriteRenderer has a sprite to prevent editor errors
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            // Create a simple 1x1 white sprite if none assigned
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
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
        transform.position = player.position + Vector3.right * orbitRadius;
        currentAngle = 0f;
        isActive = false;
        fireTimer = 0f;
    }

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
        
        // Stacking buffs: apply even during inactive state
        stackTimer += Time.deltaTime;
        if (stackTimer >= 30f)
        {
            stackTimer = 0f;
            dmgStacks += dmgStackAmount;
            aspdStacks += aspdStackPercent;
        }
        
        // Check activation
        if (!isActive)
        {
            activationDelay -= Time.deltaTime;
            if (activationDelay <= 0f)
            {
                isActive = true;
            }
            return;
        }
        
        // Fire logic
        float currentFireRate = baseFireRate * (1f + aspdStacks) * synergyAspdMultiplier;
        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / currentFireRate)
        {
            fireTimer = 0f;
            Fire();
        }
    }
    
private void Fire()
    {
        if (ObjectPooling.Instance == null)
        {
            Debug.LogWarning("[MysteryCube] ObjectPooling.Instance is null");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogWarning("[MysteryCube] firePoint is null");
            return;
        }
        if (string.IsNullOrEmpty(bulletKey))
        {
            Debug.LogWarning("[MysteryCube] bulletKey is empty");
            return;
        }
        
        // Find nearest enemy to aim at
        Vector2 aimDir = FindNearestEnemyDirection();
        if (aimDir == Vector2.zero) return;
        
        // Rotate cube to face aim direction
        float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, aimAngle);
        
        for (int i = 0; i < projectileCount; i++)
        {
            float currentSpread = spread;
            float spreadAngle = currentSpread == 0f || projectileCount == 1
                ? 0f
                : Mathf.Lerp(-currentSpread, currentSpread, (float)i / (projectileCount - 1));
            
            Vector2 fireDir = Quaternion.Euler(0, 0, spreadAngle) * aimDir;
            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, fireDir);
            GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, firePoint.position, bulletRotation);
            
            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                Transform bulletOwner = playerTransform != null ? playerTransform : transform.root;
                float damageMultiplier = (1f + dmgStacks / 20f) * synergyDamageMultiplier; // base damage + stacks + synergy
                bullet.Init(fireDir, bulletOwner,
                    pierce, 1f, damageMultiplier, 0f, 1f, 1f, false, 0f, 0, null);
            }
            else
            {
                Debug.LogWarning($"[MysteryCube] Failed to spawn bullet: bulletObj={bulletObj}, hasBullet={bulletObj?.GetComponent<Bullet>()}");
            }
        }
    }
    
    private Vector2 FindNearestEnemyDirection()
    {
        if (playerTransform == null) return Vector2.zero;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 15f); // search range
        float closestDist = float.MaxValue;
        Transform nearestEnemy = null;
        
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