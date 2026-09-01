using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponController weapon;
    [SerializeField] private SpriteRenderer weaponSprite;

    [Header("Gun Kickback (súng lùi ngược hướng front)")]
    [SerializeField] private float gunKickbackDistance = 0.2f;
    [SerializeField] private float gunKickbackDuration = 0.08f;
    [SerializeField] private AnimationCurve gunKickbackCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Gun Shake (súng rung khi bắn)")]
    [SerializeField] private float gunShakeIntensity = 0.03f;
    [SerializeField] private float gunShakeDuration = 0.06f;
    [SerializeField] private AnimationCurve gunShakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float gunKickbackTimer;
    private float gunShakeTimer;
    private Vector3 gunKickbackDir;
    private Vector3 originalLocalPos;

    private void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<WeaponController>();
        if (weaponSprite == null)
            weaponSprite = GetComponentInChildren<SpriteRenderer>();

        if (weaponSprite != null)
            originalLocalPos = weaponSprite.transform.localPosition;
    }

    private void OnEnable()
    {
        if (weapon != null)
            weapon.OnFire += OnWeaponFire;
    }

    private void OnDisable()
    {
        if (weapon != null)
            weapon.OnFire -= OnWeaponFire;
    }

    private void OnWeaponFire()
    {
        // Hướng kickback = ngược hướng front (local)
        if (weapon.WeaponFront != null)
        {
            Vector3 frontDir = weapon.WeaponFront.localPosition;
            gunKickbackDir = -frontDir.normalized * gunKickbackDistance;
        }
        else
        {
            gunKickbackDir = Vector3.left * gunKickbackDistance;
        }

        gunKickbackTimer = gunKickbackDuration;
        gunShakeTimer = gunShakeDuration;
    }

    private void LateUpdate()
    {
        if (weaponSprite == null) return;

        Vector3 offset = Vector3.zero;

        // Gun kickback: sprite lùi lại theo hướng ngược front
        if (gunKickbackTimer > 0f)
        {
            gunKickbackTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(gunKickbackTimer / gunKickbackDuration);
            offset += gunKickbackDir * gunKickbackCurve.Evaluate(t);
        }

        // Gun shake: rung ngẫu nhiên
        if (gunShakeTimer > 0f)
        {
            gunShakeTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(gunShakeTimer / gunShakeDuration);
            float intensity = gunShakeIntensity * gunShakeCurve.Evaluate(t);
            offset += new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0f);
        }

        weaponSprite.transform.localPosition = originalLocalPos + offset;
    }
}
