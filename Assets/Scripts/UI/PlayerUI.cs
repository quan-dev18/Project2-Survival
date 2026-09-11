using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private FlamethrowerController flamethrower;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image reloadFill;
    [SerializeField] private GameObject reloadPanel;
    [SerializeField] private int lowAmmoPercent = 30;
    [SerializeField] private Color lowColor = Color.red;

    private void Start()
    {
        if (weapon != null) SubscribeEvents();
        if (flamethrower != null) SubscribeFlame();
        if (reloadPanel != null) reloadPanel.SetActive(false);
        RefreshAmmo();
    }

    private void OnDestroy()
    {
        if (weapon != null) UnsubscribeEvents();
        if (flamethrower != null) UnsubscribeFlame();
    }

    public void SetWeapon(WeaponController newWeapon)
    {
        if (weapon == newWeapon) return;
        if (weapon != null) UnsubscribeEvents();
        if (flamethrower != null) { UnsubscribeFlame(); flamethrower = null; }
        weapon = newWeapon;
        if (weapon != null)
        {
            SubscribeEvents();
            OnAmmoChanged(weapon.CurrentAmmo, weapon.MagazineSize);
        }
        else RefreshAmmo();
    }

    public void SetFlamethrower(FlamethrowerController newFlame)
    {
        if (flamethrower == newFlame) return;
        if (flamethrower != null) UnsubscribeFlame();
        if (weapon != null) { UnsubscribeEvents(); weapon = null; }
        flamethrower = newFlame;
        if (flamethrower != null)
        {
            SubscribeFlame();
            OnAmmoChanged(flamethrower.CurrentAmmo, flamethrower.MagazineSize);
        }
        else RefreshAmmo();
    }

    private void RefreshAmmo()
    {
        if (weapon != null) OnAmmoChanged(weapon.CurrentAmmo, weapon.MagazineSize);
        else if (flamethrower != null) OnAmmoChanged(flamethrower.CurrentAmmo, flamethrower.MagazineSize);
        else OnAmmoChanged(0, 0);
    }

    private void SubscribeEvents()
    {
        weapon.OnAmmoChanged += OnAmmoChanged;
        weapon.OnReloadStart += OnReloadStart;
        weapon.OnReloadEnd += OnReloadEnd;
    }
    private void UnsubscribeEvents()
    {
        weapon.OnAmmoChanged -= OnAmmoChanged;
        weapon.OnReloadStart -= OnReloadStart;
        weapon.OnReloadEnd -= OnReloadEnd;
    }
    private void SubscribeFlame()
    {
        flamethrower.OnAmmoChanged += OnAmmoChanged;
        flamethrower.OnReloadStart += OnReloadStart;
        flamethrower.OnReloadEnd += OnReloadEnd;
    }
    private void UnsubscribeFlame()
    {
        flamethrower.OnAmmoChanged -= OnAmmoChanged;
        flamethrower.OnReloadStart -= OnReloadStart;
        flamethrower.OnReloadEnd -= OnReloadEnd;
    }

    private void Update()
    {
        bool reloading = (weapon != null && weapon.IsReloading) || (flamethrower != null && flamethrower.IsReloading);
        float progress = weapon != null ? weapon.ReloadProgress : flamethrower != null ? flamethrower.ReloadProgress : 0f;
        if (reloading)
        {
            reloadFill.fillAmount = progress;
            if (reloadFill.fillAmount >= 1f && reloadPanel.activeSelf)
                OnReloadEnd();
        }
    }

    private void OnAmmoChanged(int current, int max)
    {
        int lowThreshold = Mathf.CeilToInt(max * lowAmmoPercent / 100f);
        string ammo = current <= lowThreshold
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(lowColor)}>{current}</color>"
            : current.ToString();
        ammoText.text = $"{ammo}/{max}";
    }

    private void OnReloadStart()
    {
        reloadPanel.SetActive(true);
        reloadFill.fillAmount = 0f;
        ammoText.gameObject.SetActive(false);
    }

    private void OnReloadEnd()
    {
        reloadPanel.SetActive(false);
        ammoText.gameObject.SetActive(true);
    }
}
