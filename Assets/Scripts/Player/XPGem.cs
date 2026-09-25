using DG.Tweening;
using UnityEngine;

public class XPGem : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private float xpAmount = 1f;
    [SerializeField] private float magnetSpeed = 16f;
    [SerializeField] private float backDistance = 1f;
    [SerializeField] private float backDuration = 0.15f;
    [Header("Value Tiers")]
    [Tooltip("1 XP and below (matches the classic green look).")]
    [SerializeField] private Color tierSmallColor = new Color(0f, 1f, 0.126f, 1f);
    [Tooltip("2 to 10 XP.")]
    [SerializeField] private Color tierMidColor = Color.yellow;
    [Tooltip("11 XP and up.")]
    [SerializeField] private Color tierBigColor = Color.red;
    
    [Header("Shield Piece")]
    [Tooltip("Chance (0-100%) for this gem to also grant shield in addition to its normal XP.")]
    [SerializeField] private float shieldPieceChance = 5f;
    [Tooltip("Armor amount restored when shield piece is collected.")]
    [SerializeField] private float shieldArmorAmount = 5f;
    [Tooltip("Time in seconds before shield piece despawns if not collected.")]
    [SerializeField] private float shieldDespawnTime = 30f;
    [Tooltip("Sprite to use for shield piece (armor/shield icon).")]
    [SerializeField] private Sprite shieldPieceSprite;
    private const float RetargetThreshold = 0.1f;
    private SpriteRenderer gemRenderer;

    /// <summary>GoldGem opts out (it has its own gold look).</summary>
    protected virtual bool UseValueTiers => true;
    /// <summary>GoldGem opts out (gold should never become a shield piece).</summary>
    protected virtual bool AllowShieldPiece => true;
    protected const float ArrivalTolerance = 0.5f;

    protected Transform target;
    private PlayerStats cachedStats;
    private bool magnetized;
    private bool bouncing;
    private Tween magnetTween;
    private Vector3 aimPoint;

    private float checkTimer;
    private const float CheckInterval = 0.1f;
    
    private bool isShieldPiece;
    private float despawnTimer;
    private Sprite originalSprite;

    private void Awake()
    {
        gemRenderer = GetComponentInChildren<SpriteRenderer>();
        if (gemRenderer != null)
            originalSprite = gemRenderer.sprite;
    }

    public void OnSpawned()
    {
        magnetized = false;
        bouncing = false;
        checkTimer = Random.Range(0f, CheckInterval);
        magnetTween?.Kill();
        cachedStats = null;
        despawnTimer = 0f;
        
        // Determine if this is a shield piece (still grants its normal XP value too)
        isShieldPiece = AllowShieldPiece && Random.value * 100f < shieldPieceChance;
        
        if (isShieldPiece && shieldPieceSprite != null && gemRenderer != null)
        {
            gemRenderer.sprite = shieldPieceSprite;
        }
        else if (!isShieldPiece && originalSprite != null && gemRenderer != null)
        {
            gemRenderer.sprite = originalSprite;
        }
        
        ResolveTarget();
        ApplyTierColor();
    }

    public void SetAmount(float amount)
    {
        xpAmount = amount;
        ApplyTierColor();
    }

    private void ApplyTierColor()
    {
        if (!UseValueTiers || gemRenderer == null) return;
        if (xpAmount < 2f)
            gemRenderer.color = tierSmallColor;
        else if (xpAmount <= 10f)
            gemRenderer.color = tierMidColor;
        else
            gemRenderer.color = tierBigColor;
    }

    private void ResolveTarget()
    {
        if (target == null && PlayerXP.Instance != null)
            target = PlayerXP.Instance.transform;
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }
        if (target == null)
        {
            Debug.LogWarning($"XPGem: no player found at {transform.position}, orb cannot magnetize. Tag the player 'Player' or add PlayerXP to it.");
            return;
        }
        if (cachedStats == null)
            cachedStats = target.GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (target == null)
        {
            ResolveTarget();
            return;
        }

        if (cachedStats == null) return;

        if (isShieldPiece)
        {
            despawnTimer += Time.deltaTime;
            if (despawnTimer >= shieldDespawnTime)
            {
                if (ObjectPooling.Instance != null)
                    ObjectPooling.Instance.Despawn(gameObject);
                return;
            }
        }

        if (!magnetized)
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = CheckInterval;

            float range = cachedStats.CollectRange;
            Vector2 diff = (Vector2)target.position - (Vector2)transform.position;
            if (diff.sqrMagnitude <= range * range)
            {
                magnetized = true;
                BackBounce();
            }
            return;
        }

        if (bouncing) return;

        if (magnetTween == null || !magnetTween.IsActive()
            || Vector2.Distance(aimPoint, target.position) > RetargetThreshold)
        {
            Chase();
        }
    }

    private void BackBounce()
    {
        bouncing = true;
        magnetTween?.Kill();
        Vector3 away = (transform.position - target.position).normalized;
        Vector3 backPos = transform.position + away * backDistance;
        magnetTween = transform.DOMove(backPos, backDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(Chase);
    }

    protected virtual void Chase()
    {
        bouncing = false;
        magnetTween?.Kill();
        aimPoint = target.position;
        float dist = Vector2.Distance(transform.position, aimPoint);
        magnetTween = transform.DOMove(aimPoint, dist / magnetSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(Collect);
    }

    protected virtual void Collect()
    {
        if (target == null)
        {
            ResolveTarget();
            return;
        }

        if (Vector2.Distance(target.position, transform.position) > ArrivalTolerance)
        {
            Chase();
            return;
        }

        if (isShieldPiece)
        {
            if (target.TryGetComponent(out PlayerStats playerStats))
            {
                float armorGain = shieldArmorAmount * (playerStats.bonusDoubleShieldArmor ? 2f : 1f);
                playerStats.AddArmor(armorGain);
            }
        }
        if (target.TryGetComponent(out PlayerXP playerXP))
            playerXP.AddExperience(playerXP.PickupValue(xpAmount));
        PlayCollectSFX();
        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
    }

    /// <summary>
    /// Phát tiếng thu thập. GoldGem ghi đè phương thức này để dùng clip riêng.
    /// Clip được cấu hình tập trung trong AudioManager.
    /// </summary>
    protected virtual void PlayCollectSFX()
    {
        if (isShieldPiece)
            AudioManager.Instance?.PlayShieldCollect();
        else
            AudioManager.Instance?.PlayXPCollect();
    }

    private void OnDisable()
    {
        magnetTween?.Kill();
    }
}