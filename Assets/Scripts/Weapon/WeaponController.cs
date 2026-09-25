using UnityEngine;
using System;

public class WeaponController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponSO weaponStats;
    public WeaponSO WeaponStats => weaponStats;

    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponFront;
    public Transform WeaponFront => weaponFront;
    [SerializeField] private Transform target;
    [SerializeField] private string bulletKey = "Bullet";
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Hands")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private float handSwitchDeadZone = 0.3f;

    [Header("Target Detection")]
    [SerializeField] private string targetTag = "Target";

    [Header("Aim")]
    [Tooltip("Rotation responsiveness. Higher = snappier, lower = smoother. Frame-rate independent.")]
    [SerializeField] private float aimLerpSpeed = 15f;

    [Header("Fog")]
    [SerializeField] private FogController fogController;

    [Header("Muzzle Flash")]
    [SerializeField] private MuzzleFlashLight muzzleFlash;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer weaponSprite;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Sorting")]
    [SerializeField] private PlayerMovement playerMovement;

    #region Base Stats (từ SO, không đổi)
    private float baseFireRate;
    private float baseFireRange;
    private float baseReloadTime;
    private int baseMagazineSize;
    private int baseSpread;
    private int baseBulletCount;
    private int basePierce;
    #endregion

    #region Bonus 
    public float bonusFireRatePercent { get; private set; }   // giảm thời gian giữa 2 phát -> cần trừ ngược
    public float bonusFireRangePercent { get; private set; }
    public float bonusReloadSpeedPercent { get; private set; } // giảm reload time
    public float bonusMagazineSizePercent { get; private set; }
    public int bonusBulletCountFlat { get; private set; }
    public int bonusBulletPierce { get; private set; }
    public float bonusBulletSpeedPercent { get; private set; }
    public float bonusBulletDamagePercent { get; private set; }
    public float bonusBulletExecutePercent { get; private set; }
    public float bonusBulletKnockbackPercent { get; private set; }
    public float bonusBulletSizePercent { get; private set; }
    public float bonusBulletSpread { get; private set; }
    public int bonusBulletBounceCount { get; private set; }
    public float bonusFreeShotChanceWhileStill { get; private set; }
    public bool bonusLastAmmoBurst { get; private set; }
    public bool bonusBackShot { get; private set; }
    public float bonusDamageBuffAfterReload { get; private set; }
    private float damageBuffTimer;
    private float appliedDamageBuff;
    public float bonusReloadSpeedStackOnKill { get; private set; }
    private int killStacks;
    public bool bonusBulletInfinitePierceOnKill { get; private set; }
    public float bonusBulletExplosionDamagePercent { get; private set; }
    public float bonusBulletExplosionRadius { get; private set; }
    #endregion


    #region Final stats (tính toán runtime)
    // fireRate là thời gian chờ giữa 2 phát -> bonus % làm bắn NHANH hơn nghĩa là fireRate giảm
    public float fireRate => Mathf.Max(0.1f, baseFireRate / Mathf.Max(0.01f, 1f + bonusFireRatePercent));
    public float fireRange => baseFireRange * (1f + bonusFireRangePercent);
    public float reloadTime => Mathf.Max(0.1f, baseReloadTime / Mathf.Max(0.01f, 1f + bonusReloadSpeedPercent + killStacks * 0.02f));
    public int magazineSize => Mathf.RoundToInt(baseMagazineSize * (1f + bonusMagazineSizePercent));
    private int spread => baseSpread;
    private int bulletCount => Mathf.Clamp(baseBulletCount + bonusBulletCountFlat,1,4);
    public int pierce => basePierce + bonusBulletPierce;
    #endregion

    #region Runtime state
    private int currentAmmo;
    private float lastFireTime;
    private bool isReloading;
    private float reloadTimer;
    private Transform currentHand;
    #endregion

    public event Action<int, int> OnAmmoChanged;
    public event Action OnReloadStart;
    public event Action OnReloadEnd;
    public event Action OnFire;

    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public float ReloadProgress => isReloading ? 1f - (reloadTimer / reloadTime) : 1f;

    private void Awake()
    {
        baseFireRate = weaponStats.FireRate;
        baseFireRange = weaponStats.FireRange;
        baseReloadTime = weaponStats.ReloadTime;
        baseMagazineSize = weaponStats.MagazineSize;
        baseSpread = weaponStats.Spread;
        baseBulletCount = weaponStats.BulletCount;
        basePierce = weaponStats != null ? weaponStats.BasePierce : 0;
        currentAmmo = magazineSize;

        if (TryGetComponent(out CircleCollider2D rangeTrigger))
            rangeTrigger.radius = fireRange;
        if(weaponSprite == null)
            weaponSprite = GetComponentInChildren<SpriteRenderer>();
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponentInParent<Rigidbody2D>();
            if (playerRigidbody == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerRigidbody = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInChildren<Rigidbody2D>();
            }
        }
    }

    /// <summary>Áp sprite skin đang trang bị lên vũ khí (gọi khi vào game / đổi súng).</summary>
    public void ApplySkinSprite(Sprite sprite)
    {
        if (sprite == null) return;

        if (weaponSprite == null)
            weaponSprite = GetComponentInChildren<SpriteRenderer>(true);

        if (weaponSprite != null)
            weaponSprite.sprite = sprite;
    }

    private void Update()
    {
        // Không xử lý khi game không ở trạng thái Playing
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (isReloading)
            HandleReloadTimer();

        // Update damage buff timer
        if (damageBuffTimer > 0f)
        {
            damageBuffTimer -= Time.deltaTime;
            if (damageBuffTimer <= 0f)
            {
                bonusBulletDamagePercent -= appliedDamageBuff;
                damageBuffTimer = 0f;
            }
        }

        bool hasTarget = target != null && IsTargetInRange();

        if (hasTarget)
            Aim();

        if (hasTarget && !isReloading)
        {
            bool canFire = Time.time - lastFireTime >= fireRate;
            if (canFire)
            {
                if (currentAmmo > 0)
                    Fire();
                else
                    StartReload();
            }
        }

        if (currentAmmo <= 0 && !isReloading)
            StartReload();

        UpdateSortingLayer();
    }

    private void UpdateSortingLayer()
    {
        if (weaponSprite == null) return;

        if (playerMovement != null && playerMovement.LastDir == 1)
        {
            weaponSprite.sortingLayerName = "Player";
            weaponSprite.sortingOrder = -1;
        }
        else
        {
            weaponSprite.sortingLayerName = "Weapon";
            weaponSprite.sortingOrder = 0;
        }
    }

    private bool IsTargetInRange()
    {
        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= fireRange;
    }
#region Aim
    private void Aim()
    {
        Vector2 toTargetRaw = (Vector2)target.position - (Vector2)transform.position;
        Transform desiredHand = toTargetRaw.x >= 0 ? rightHand : leftHand;

        if (currentHand == null)
            currentHand = desiredHand;
        else if (desiredHand != currentHand && Mathf.Abs(toTargetRaw.x) > handSwitchDeadZone)
            currentHand = desiredHand;

        transform.position = currentHand.position;

        // Recompute AFTER moving, so direction matches the new position
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

        // Offset so weaponFront's resting angle lines up with 0°
        float frontOffset = Mathf.Atan2(weaponFront.localPosition.y, weaponFront.localPosition.x) * Mathf.Rad2Deg;

        float desiredAngle = targetAngle - frontOffset;
        float smoothedAngle = Mathf.LerpAngle(transform.eulerAngles.z, desiredAngle, 1f - Mathf.Exp(-aimLerpSpeed * Time.deltaTime));
        transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);

        // Mirror sprite so it doesn't appear upside-down on the left side
        if (weaponSprite != null)
        {
            bool aimingRight = toTarget.x >= 0f;
            weaponSprite.flipY = spriteFacesRight && !aimingRight;
            weaponSprite.flipX = !spriteFacesRight && aimingRight;
        }
    }
#endregion
#region Fire
    private void Fire()
    {
        lastFireTime = Time.time;
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
        OnFire?.Invoke();

        if (ObjectPooling.Instance == null || weaponFront == null)
            return;

        Vector2 barrelDir = (Vector2)(transform.rotation * weaponFront.localPosition).normalized;

        if (fogController != null)
            fogController.RevealAt(weaponFront.position, barrelDir);

        muzzleFlash?.Flash();

        for (int i = 0; i < bulletCount; i++)
        {
            float currentSpread = spread + bonusBulletSpread;
            float spreadAngle = currentSpread == 0f || bulletCount == 1
                ? 0f
                : Mathf.Lerp(-currentSpread, currentSpread, (float)i / (bulletCount - 1));
            Vector2 bulletDir = Quaternion.Euler(0, 0, spreadAngle) * barrelDir;

            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, bulletDir);
            GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, weaponFront.position, bulletRotation);

            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                bullet.Init(bulletDir, transform.root,
                    pierce, 1f + bonusBulletSpeedPercent, 1f + bonusBulletDamagePercent,
                    bonusBulletExecutePercent, 1f + bonusBulletKnockbackPercent, 1f + bonusBulletSizePercent,
                    bonusBulletInfinitePierceOnKill,
                    bonusBulletExplosionDamagePercent,
                    bonusBulletBounceCount,
                    () => AddKillStack()); // damage do chính BulletSO quyết định
            }
        }

        // Chance for free shot while standing still
        if (bonusFreeShotChanceWhileStill > 0f && playerRigidbody != null)
        {
            if (playerRigidbody.velocity.sqrMagnitude < 0.01f) // standing still threshold
            {
                if (UnityEngine.Random.value < bonusFreeShotChanceWhileStill)
                {
                    currentAmmo++; // refund the ammo cost
                    OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
                }
            }
        }

        // Last ammo burst: when this shot consumed the last ammo
        if (bonusLastAmmoBurst && currentAmmo == 0)
        {
            FireLastAmmoBurst();
        }

        // Back shot: fire additional bullet behind
        if (bonusBackShot)
        {
            FireBackShot();
        }
    }

    private void FireBackShot()
    {
        if (ObjectPooling.Instance == null) return;

        Vector2 backDir = -(Vector2)(transform.rotation * weaponFront.localPosition).normalized;

        Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, backDir);
        GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, weaponFront.position, bulletRotation);

        if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
        {
            bullet.Init(backDir, transform.root,
                pierce, 1f + bonusBulletSpeedPercent, 1f + bonusBulletDamagePercent,
                bonusBulletExecutePercent, 1f + bonusBulletKnockbackPercent, 1f + bonusBulletSizePercent,
                bonusBulletInfinitePierceOnKill,
                bonusBulletExplosionDamagePercent,
                bonusBulletBounceCount,
                () => AddKillStack());
        }
    }

    private void FireLastAmmoBurst()
    {
        if (ObjectPooling.Instance == null) return;

        int burstCount = 10;
        float burstDamageMultiplier = 0.5f;

        for (int i = 0; i < burstCount; i++)
        {
            float angle = (360f / burstCount) * i;
            Vector2 burstDir = Quaternion.Euler(0, 0, angle) * Vector2.right;

            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, burstDir);
            GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, transform.position, bulletRotation);

            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                bullet.Init(burstDir, transform.root,
                    0, 1f + bonusBulletSpeedPercent, burstDamageMultiplier,
                    0f, 1f + bonusBulletKnockbackPercent, 1f + bonusBulletSizePercent,
                    false,
                    0f,
                    0,
                    () => AddKillStack()); // 50% damage, no pierce/execute/explosion/bounce
            }
        }
    }
#endregion
#region Reload
    public void StartReload()
    {
        if (isReloading || currentAmmo == magazineSize) return;

        isReloading = true;
        reloadTimer = reloadTime;
        OnReloadStart?.Invoke();
    }

    private void HandleReloadTimer()
    {
        reloadTimer -= Time.deltaTime;
        if (reloadTimer <= 0f)
        {
            currentAmmo = magazineSize;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
            OnReloadEnd?.Invoke();

            // Apply damage buff after reload (refresh, don't stack)
            if (bonusDamageBuffAfterReload > 0f)
            {
                if (damageBuffTimer > 0f)
                    bonusBulletDamagePercent -= appliedDamageBuff; // remove previous instance before re-applying
                appliedDamageBuff = bonusDamageBuffAfterReload;
                bonusBulletDamagePercent += appliedDamageBuff;
                damageBuffTimer = 3f;
            }

            // Reset kill stacks after reload
            killStacks = 0;
        }
    }
#endregion
#region Bonus Stat
    public void AddFireRatePercent(float amount) => bonusFireRatePercent += amount;
    public void AddFireRangePercent(float amount)
    {
        bonusFireRangePercent += amount;
        if (TryGetComponent(out CircleCollider2D rangeTrigger))
            rangeTrigger.radius = fireRange;
    }
    public void AddReloadSpeedPercent(float amount) => bonusReloadSpeedPercent += amount;
    public void AddMagazineSizePercent(float amount)
    {
        int oldMagSize = magazineSize;
        bonusMagazineSizePercent += amount;
        currentAmmo = Mathf.Max(0, currentAmmo + magazineSize - oldMagSize); 
    }
    public void AddBulletCount(int amount) => bonusBulletCountFlat += amount;

    public void AddBulletPierce(int amount) => bonusBulletPierce += amount;

    public void AddBulletSpeedPercent(float amount) => bonusBulletSpeedPercent += amount;

    public void AddBulletDamagePercent(float amount) => bonusBulletDamagePercent += amount;

    public void AddBulletExecutePercent(float amount) => bonusBulletExecutePercent = Mathf.Clamp(bonusBulletExecutePercent + amount, 0f, 1f);

    public void AddBulletKnockbackPercent(float amount) => bonusBulletKnockbackPercent += amount;

    public void AddBulletSizePercent(float amount) => bonusBulletSizePercent += amount;

    public void AddBulletSpread(float amount) => bonusBulletSpread += amount;

    public void AddBulletBounceCount(int amount) => bonusBulletBounceCount += amount;

    public void AddFreeShotChanceWhileStill(float amount) => bonusFreeShotChanceWhileStill = Mathf.Clamp01(bonusFreeShotChanceWhileStill + amount);

    public void AddLastAmmoBurst(float amount) => bonusLastAmmoBurst = amount > 0f;

    public void AddBackShot(float amount) => bonusBackShot = amount > 0f;

    public void AddDamageBuffAfterReload(float amount) => bonusDamageBuffAfterReload += amount;

    public void AddReloadSpeedStackOnKill(float amount) => bonusReloadSpeedStackOnKill += amount;

    private void AddKillStack()
    {
        if (bonusReloadSpeedStackOnKill > 0f)
        {
            killStacks = Mathf.Min(killStacks + 1, 25); // 25 stacks * 2% = 50% max
        }
    }

    public void AddBulletInfinitePierceOnKill(float amount) => bonusBulletInfinitePierceOnKill = amount > 0f;

    public void AddBulletExplosionDamagePercent(float amount) => bonusBulletExplosionDamagePercent += amount;
    public void AddBulletExplosionRadius(float amount) => bonusBulletExplosionRadius += amount;

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, magazineSize);
        OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
    }
#endregion
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetPlayerMovement(PlayerMovement pm) => playerMovement = pm;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;

        if (target == null ||
            Vector2.Distance(other.transform.position, transform.position) <
            Vector2.Distance(target.position, transform.position))
        {
            target = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform == target)
            target = null;
    }
}