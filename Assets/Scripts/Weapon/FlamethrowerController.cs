using UnityEngine;
using System;

public class FlamethrowerController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponSO weaponStats;
    public WeaponSO WeaponStats => weaponStats;

    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponFront;
    public Transform WeaponFront => weaponFront;
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Flamethrower")]
    [SerializeField] private ParticleSystem fireEffect; // "Fire_effect"
    [SerializeField] private float coneAngle = 45f;
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private float damagePerTick = 5f;
    [Tooltip("If true, adds WeaponSO.BulletCount*2 as flat damage per tick")]
    [SerializeField] private bool useWeaponDamage = false;

    [Header("Sound")]
    [Tooltip("Volume tiếng lửa loop khi phun (0-1). Clip lấy từ WeaponSO.ShootSFX.")]
    [Range(0f, 1f)]
    [SerializeField] private float fireSoundVolume = 1f;
    [Tooltip("Pitch tiếng lửa loop (1 = bình thường).")]
    [SerializeField] private float fireSoundPitch = 1f;
    [Tooltip("Còn bao nhiêu đạn thì bắt đầu fade out tiếng lửa (0 = tắt fade).")]
    [SerializeField] private float fireSoundFadeAmmo = 4f;

    [Header("Hands")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private float handSwitchDeadZone = 0.3f;

    [Header("Target Detection")]
    [SerializeField] private string targetTag = "Target";

    [Header("Fog")]
    [SerializeField] private FogController fogController;

    [Header("Muzzle Flash")]
    [SerializeField] private MuzzleFlashLight muzzleFlash;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer weaponSprite;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Movement")]
    [SerializeField] private PlayerMovement playerMovement;

    #region Base Stats
    private float baseFireRate;
    private float baseFireRange;
    private float baseReloadTime;
    private int baseMagazineSize;
    #endregion

    #region Bonus (same names as WeaponController for LevelUpPanel compatibility)
    public float bonusFireRatePercent { get; private set; }
    public float bonusFireRangePercent { get; private set; }
    public float bonusReloadSpeedPercent { get; private set; }
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

    #region Final stats
    public float tickInterval => Mathf.Max(0.05f, baseFireRate / Mathf.Max(0.01f, 1f + bonusFireRatePercent));
    public float fireRange => baseFireRange * (1f + bonusFireRangePercent);
    public float reloadTime => Mathf.Max(0.1f, baseReloadTime / Mathf.Max(0.01f, 1f + bonusReloadSpeedPercent + killStacks * 0.02f));
    public int magazineSize => Mathf.RoundToInt(baseMagazineSize * (1f + bonusMagazineSizePercent));

    /// <summary>Sát thương nền mỗi tick (gồm cả flat damage từ WeaponSO nếu bật useWeaponDamage).</summary>
    public float BaseDamagePerTick =>
        damagePerTick + (useWeaponDamage && weaponStats != null ? weaponStats.BulletCount * 2f : 0f);

    /// <summary>Sát thương thực tế mỗi tick sau buff % — dùng cho Stat UI.</summary>
    public float EffectiveDamagePerTick => BaseDamagePerTick * (1f + bonusBulletDamagePercent);
    #endregion

    #region Runtime state
    private int currentAmmo;
    private float lastFireTime;
    private bool isReloading;
    private float reloadTimer;
    private Transform currentHand;
    private AudioClip fireLoopClip;
    #endregion

    public event Action<int, int> OnAmmoChanged;
    public event Action OnReloadStart;
    public event Action OnReloadEnd;
    public event Action OnFire;

    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public float ReloadProgress => isReloading ? 1f - (reloadTimer / reloadTime) : 1f;

    private static readonly Collider2D[] s_ConeOverlapBuffer = new Collider2D[32];

    /// <summary>Áp sprite skin đang trang bị lên vũ khí (gọi khi vào game / đổi súng).</summary>
    public void ApplySkinSprite(Sprite sprite)
    {
        if (sprite == null) return;

        if (weaponSprite == null)
            weaponSprite = GetComponentInChildren<SpriteRenderer>(true);

        if (weaponSprite != null)
            weaponSprite.sprite = sprite;
    }

    private void Awake()
    {
        baseFireRate = weaponStats != null ? weaponStats.FireRate : 0.1f;
        baseFireRange = weaponStats != null ? weaponStats.FireRange : 6f;
        baseReloadTime = weaponStats != null ? weaponStats.ReloadTime : 1.5f;
        baseMagazineSize = weaponStats != null ? weaponStats.MagazineSize : 100;
        currentAmmo = magazineSize;
        fireLoopClip = weaponStats != null ? weaponStats.ShootSFX : null;

        if (TryGetComponent(out CircleCollider2D rangeTrigger))
            rangeTrigger.radius = fireRange;
        if (weaponSprite == null)
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
        if (fireEffect == null)
            fireEffect = GetComponentInChildren<ParticleSystem>(true);
        if (fireEffect != null)
        {
            fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var main = fireEffect.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Ensure it starts at muzzle, not at prefab origin
            if (weaponFront != null)
                fireEffect.transform.SetPositionAndRotation(weaponFront.position, weaponFront.rotation);
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null && fireLoopClip != null)
            AudioManager.Instance.StopLoopSFX(fireLoopClip);
    }

    private void Update()
    {
        // Không xử lý khi game không ở trạng thái Playing
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // Keep fire VFX glued to muzzle even in World space
        if (fireEffect != null && weaponFront != null)
            fireEffect.transform.SetPositionAndRotation(weaponFront.position, weaponFront.rotation);

        if (isReloading)
            HandleReloadTimer();

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
        if (hasTarget) Aim();

        // Flamethrower fires whenever not reloading/has ammo, not only when hasTarget - so particles show even without lock
        bool shouldFire = !isReloading && currentAmmo > 0;
        UpdateFireEffect(shouldFire);
        UpdateFireSound(shouldFire);

        if (shouldFire)
        {
            bool canTick = Time.time - lastFireTime >= tickInterval;
            if (canTick)
            {
                // Check standing-still free shot chance before consuming ammo
                bool isFree = false;
                if (bonusFreeShotChanceWhileStill > 0f && playerRigidbody != null && playerRigidbody.velocity.sqrMagnitude < 0.01f)
                    isFree = UnityEngine.Random.value < bonusFreeShotChanceWhileStill;

                if (!isFree)
                {
                    currentAmmo--;
                    OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
                }
                lastFireTime = Time.time;
                OnFire?.Invoke();
                if (fogController != null && weaponFront != null)
                {
                    Vector2 barrelDir = (transform.rotation * weaponFront.localPosition).normalized;
                    fogController.RevealAt(weaponFront.position, barrelDir);
                }
                muzzleFlash?.Flash();
                ConeDamage();
                if (bonusLastAmmoBurst && currentAmmo == 0) FireLastAmmoBurst();
                if (bonusBackShot) FireBackShot();
                if (currentAmmo <= 0) StartReload();
            }
        }

        if (currentAmmo <= 0 && !isReloading)
            StartReload();

        UpdateSortingLayer();
    }

    private void UpdateFireEffect(bool shouldFire)
    {
        if (fireEffect == null) return;
        if (shouldFire && !fireEffect.isPlaying) fireEffect.Play(true);
        else if (!shouldFire && fireEffect.isPlaying) fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>
    /// Bật/tắt tiếng lửa LOOP theo đúng cờ shouldFire (giống UpdateFireEffect).
    /// Kiểm tra qua AudioManager.IsLoopActive để tự phục hồi sau khi audio bị
    /// tắt bởi StopAllSFX (pause), đổi scene... mà không phải bấm lại từ đầu.
    /// Gát thêm Time.timeScale để ngưng tiếng khi dừng game (level up panel...).
    /// </summary>
    private void UpdateFireSound(bool shouldFire)
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null) return;

        AudioClip clip = fireLoopClip;
        if (clip == null) return;

        bool shouldPlay = shouldFire && Time.timeScale > 0f;

        if (shouldPlay)
        {
            float targetVolume = fireSoundVolume;
            if (fireSoundFadeAmmo > 0f && currentAmmo < fireSoundFadeAmmo)
                targetVolume *= Mathf.Clamp01(currentAmmo / fireSoundFadeAmmo);

            if (!manager.IsLoopActive(clip))
                manager.PlayLoopSFX(clip, targetVolume, fireSoundPitch);
            else
                manager.SetLoopVolume(clip, targetVolume);
        }
        else if (manager.IsLoopActive(clip))
        {
            manager.StopLoopSFX(clip);
        }
    }

    private void ConeDamage()
    {
        if (weaponFront == null) return;
        Vector2 origin = weaponFront.position;
        Vector2 barrelDir = (transform.rotation * weaponFront.localPosition).normalized;
        float range = fireRange;

        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, s_ConeOverlapBuffer, enemyMask);
        float halfAngle = coneAngle * 0.5f;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = s_ConeOverlapBuffer[i];
            if (hit == null) continue;
            if (transform.root != null && hit.transform.IsChildOf(transform.root)) continue;
            if (hit.GetComponentInParent<PlayerStats>() != null) continue;

            Vector2 toEnemy = (Vector2)hit.transform.position - origin;
            float dist = toEnemy.magnitude;
            if (dist > range || dist < 0.01f) continue;
            float angle = Vector2.Angle(barrelDir, toEnemy.normalized);
            if (angle > halfAngle) continue;

            IDamageable dmg;
            if (!hit.TryGetComponent(out dmg))
            {
                dmg = hit.GetComponentInChildren<IDamageable>();
                if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
            }
            if (dmg == null) continue;

            float baseDmg = damagePerTick + (useWeaponDamage && weaponStats != null ? weaponStats.BulletCount * 2f : 0f);
            float finalDamage = baseDmg * (1f + bonusBulletDamagePercent);
            dmg.TakeDamage(finalDamage);

            if (bonusBulletExecutePercent > 0f && dmg is EnemyHealth eh && eh.CurrentHealth > 0f && eh.CurrentHealth <= eh.MaxHealth * bonusBulletExecutePercent)
                dmg.TakeDamage(eh.CurrentHealth);

            IKnockbackable kb;
            if (!hit.TryGetComponent(out kb))
            {
                kb = hit.GetComponentInChildren<IKnockbackable>();
                if (kb == null) kb = hit.GetComponentInParent<IKnockbackable>();
            }
            if (kb != null) kb.ApplyKnockback(barrelDir, 3f * (1f + bonusBulletKnockbackPercent));

            if (dmg is EnemyHealth eh2 && eh2.CurrentHealth <= 0f)
                AddKillStack();
        }
    }

    private void FireBackShot()
    {
        Vector2 backDir = -(Vector2)(transform.rotation * weaponFront.localPosition).normalized;
        int hitCount = Physics2D.OverlapCircleNonAlloc(weaponFront.position, fireRange, s_ConeOverlapBuffer, enemyMask);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = s_ConeOverlapBuffer[i];
            Vector2 toEnemy = (Vector2)hit.transform.position - (Vector2)weaponFront.position;
            if (Vector2.Angle(backDir, toEnemy.normalized) > coneAngle * 0.5f) continue;
            var dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;
            dmg.TakeDamage(damagePerTick * 0.5f * (1f + bonusBulletDamagePercent));
        }
    }

    private void FireLastAmmoBurst()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 3f, s_ConeOverlapBuffer, enemyMask);
        for (int j = 0; j < hitCount; j++)
        {
            IDamageable dmg;
            if (!s_ConeOverlapBuffer[j].TryGetComponent(out dmg))
            {
                dmg = s_ConeOverlapBuffer[j].GetComponentInChildren<IDamageable>();
                if (dmg == null) dmg = s_ConeOverlapBuffer[j].GetComponentInParent<IDamageable>();
            }
            if (dmg != null) dmg.TakeDamage(50f);
        }
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
        if (target == null) return false;
        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= fireRange;
    }

    private void Aim()
    {
        Vector2 toTargetRaw = (Vector2)target.position - (Vector2)transform.position;
        Transform desiredHand = toTargetRaw.x >= 0 ? rightHand : leftHand;
        if (currentHand == null) currentHand = desiredHand;
        else if (desiredHand != currentHand && Mathf.Abs(toTargetRaw.x) > handSwitchDeadZone)
            currentHand = desiredHand;
        transform.position = currentHand.position;
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float frontOffset = Mathf.Atan2(weaponFront.localPosition.y, weaponFront.localPosition.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle - frontOffset);
        if (weaponSprite != null)
        {
            bool aimingRight = toTarget.x >= 0f;
            weaponSprite.flipY = spriteFacesRight && !aimingRight;
            weaponSprite.flipX = !spriteFacesRight && aimingRight;
        }
    }

    public void StartReload()
    {
        if (isReloading || currentAmmo == magazineSize) return;
        isReloading = true;
        reloadTimer = reloadTime;
        OnReloadStart?.Invoke();
        if (fireEffect != null) fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
            if (bonusDamageBuffAfterReload > 0f)
            {
                if (damageBuffTimer > 0f) bonusBulletDamagePercent -= appliedDamageBuff;
                appliedDamageBuff = bonusDamageBuffAfterReload;
                bonusBulletDamagePercent += appliedDamageBuff;
                damageBuffTimer = 3f;
            }
            killStacks = 0;
        }
    }

    #region Bonus Stat (mirror WeaponController so LevelUpPanel can upgrade both)
    public void AddFireRatePercent(float amount) => bonusFireRatePercent += amount;
    public void AddFireRangePercent(float amount) { bonusFireRangePercent += amount; if (TryGetComponent(out CircleCollider2D t)) t.radius = fireRange; }
    public void AddReloadSpeedPercent(float amount) => bonusReloadSpeedPercent += amount;
    public void AddMagazineSizePercent(float amount) { int old = magazineSize; bonusMagazineSizePercent += amount; currentAmmo = Mathf.Max(0, currentAmmo + magazineSize - old); }
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
    private void AddKillStack() { if (bonusReloadSpeedStackOnKill > 0f) killStacks = Mathf.Min(killStacks + 1, 25); }
    public void AddBulletInfinitePierceOnKill(float amount) => bonusBulletInfinitePierceOnKill = amount > 0f;
    public void AddBulletExplosionDamagePercent(float amount) => bonusBulletExplosionDamagePercent += amount;
    public void AddBulletExplosionRadius(float amount) => bonusBulletExplosionRadius += amount;
    public void AddAmmo(int amount) { currentAmmo = Mathf.Min(currentAmmo + amount, magazineSize); OnAmmoChanged?.Invoke(currentAmmo, magazineSize); }
    #endregion
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetPlayerMovement(PlayerMovement pm) => playerMovement = pm;
    private void OnTriggerStay2D(Collider2D other) { if (!other.CompareTag(targetTag)) return; if (target == null || Vector2.Distance(other.transform.position, transform.position) < Vector2.Distance(target.position, transform.position)) target = other.transform; }
    private void OnTriggerExit2D(Collider2D other) { if (other.transform == target) target = null; }
}
