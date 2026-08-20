using UnityEngine;
using System;

public class WeaponController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponSO weaponStats;

    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponFront;
    [SerializeField] private Transform target;
    [SerializeField] private string bulletKey = "Bullet";

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

    #region Base Stats (từ SO, không đổi)
    private float baseFireRate;
    private float baseFireRange;
    private float baseReloadTime;
    private int baseMagazineSize;
    private int baseSpread;
    private int baseBulletCount;
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
    #endregion


    #region Final stats (tính toán runtime)
    // fireRate là thời gian chờ giữa 2 phát -> bonus % làm bắn NHANH hơn nghĩa là fireRate giảm
    public float fireRate => Mathf.Max(0.1f, baseFireRate / Mathf.Max(0.01f, 1f + bonusFireRatePercent));
    public float fireRange => baseFireRange * (1f + bonusFireRangePercent);
    public float reloadTime => Mathf.Max(0.1f, baseReloadTime / Mathf.Max(0.01f, 1f + bonusReloadSpeedPercent));
    public int magazineSize => Mathf.RoundToInt(baseMagazineSize * (1f + bonusMagazineSizePercent));
    private int spread => baseSpread;
    private int bulletCount => Mathf.Clamp(baseBulletCount + bonusBulletCountFlat,1,4);
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
        currentAmmo = magazineSize;

        if (TryGetComponent(out CircleCollider2D rangeTrigger))
            rangeTrigger.radius = fireRange;
    }

    private void Update()
    {
        if (isReloading)
            HandleReloadTimer();

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
    }

    private bool IsTargetInRange()
    {
        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= fireRange;
    }
#region Aim
    private void Aim()
    {
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        Transform desiredHand = toTarget.x >= 0 ? rightHand : leftHand;
        if (currentHand == null)
            currentHand = desiredHand;
        else if (desiredHand != currentHand && Mathf.Abs(toTarget.x) > handSwitchDeadZone)
            currentHand = desiredHand;
        transform.position = currentHand.position;
        transform.rotation = Quaternion.FromToRotation(weaponFront.localPosition, toTarget);

        if (weaponSprite != null)
        {
            bool aimingRight = toTarget.x >= 0;
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
            float spreadAngle = spread == 0 || bulletCount == 1
                ? 0f
                : Mathf.Lerp(-spread, spread, (float)i / (bulletCount - 1));
            Vector2 bulletDir = Quaternion.Euler(0, 0, spreadAngle) * barrelDir;

            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, bulletDir);
            GameObject bulletObj = ObjectPooling.Instance.Spawn(bulletKey, weaponFront.position, bulletRotation);

            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                bullet.Init(bulletDir, transform.root, fireRange,
                    bonusBulletPierce, 1f + bonusBulletSpeedPercent, 1f + bonusBulletDamagePercent,
                    bonusBulletExecutePercent, 1f + bonusBulletKnockbackPercent, 1f + bonusBulletSizePercent); // damage do chính BulletSO quyết định
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
#endregion
    public void SetTarget(Transform newTarget) => target = newTarget;

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