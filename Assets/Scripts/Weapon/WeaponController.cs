using UnityEngine;
using System;

public class WeaponController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponSO weaponStats;

    [Header("References")]
    [SerializeField] private Transform weaponPivot;   // gốc xoay súng (đặt tại vị trí Rear)
    [SerializeField] private Transform weaponFront;   // nòng súng, nơi đạn bắn ra
    [SerializeField] private Transform target;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Hands")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private float handSwitchDeadZone = 0.3f;

    [Header("Target Detection")]
    [SerializeField] private string targetTag = "Target";

    #region Stats
    private float fireRate;
    private float fireRange;
    private float reloadTime;
    private int magazineSize;
    private int spread; // độ lệch góc tối đa (degree), ví dụ 5 = lệch +-5 độ
    private int bulletCount; // số lượng đạn bắn ra mỗi lần bắn
    #endregion

    #region Runtime state
    private int currentAmmo;
    private float lastFireTime;
    private bool isReloading;
    private float reloadTimer;
    private Transform currentHand;
    #endregion

    public event Action<int, int> OnAmmoChanged;   // (currentAmmo, magazineSize)
    public event Action OnReloadStart;
    public event Action OnReloadEnd;
    public event Action OnFire;

    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public float ReloadProgress => isReloading ? 1f - (reloadTimer / reloadTime) : 1f;

    private void Awake()
    {
        fireRate = weaponStats.FireRate;
        fireRange = weaponStats.FireRange;
        reloadTime = weaponStats.ReloadTime;
        magazineSize = weaponStats.MagazineSize;
        spread = weaponStats.Spread;
        bulletCount = weaponStats.BulletCount;
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
    }

    private void Fire()
    {
        lastFireTime = Time.time;
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
        OnFire?.Invoke();

        if (bulletPrefab == null || weaponFront == null)
            return;

        Vector2 barrelDir = (Vector2)(transform.rotation * weaponFront.localPosition).normalized;

        for (int i = 0; i < bulletCount; i++)
        {
            float spreadAngle = spread == 0 || bulletCount == 1
                ? 0f
                : Mathf.Lerp(-spread, spread, (float)i / (bulletCount - 1));
            Vector2 bulletDir = Quaternion.Euler(0, 0, spreadAngle) * barrelDir;

            Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.right, bulletDir);
            GameObject bulletObj;

            if (ObjectPooling.Instance != null)
                bulletObj = ObjectPooling.Instance.Spawn(bulletPrefab, weaponFront.position, bulletRotation);
            else
                bulletObj = Instantiate(bulletPrefab, weaponFront.position, bulletRotation);

            if (bulletObj != null && bulletObj.TryGetComponent(out Bullet bullet))
            {
                bullet.Init(bulletDir, transform.root);
            }
        }
    }

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
