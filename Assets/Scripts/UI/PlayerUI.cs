using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image reloadFill;
    [SerializeField] private GameObject reloadPanel;
    [SerializeField] private int lowAmmoPercent = 30;
    [SerializeField] private Color lowColor = Color.red;

    private void Start()
    {
        if (weapon != null)
            SubscribeEvents();

        if (reloadPanel != null)
            reloadPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (weapon != null)
            UnsubscribeEvents();
    }

    public void SetWeapon(WeaponController newWeapon)
    {
        if (weapon == newWeapon) return;

        if (weapon != null)
            UnsubscribeEvents();

        weapon = newWeapon;

        if (weapon != null)
        {
            SubscribeEvents();
            OnAmmoChanged(weapon.CurrentAmmo, weapon.MagazineSize);
        }
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

    private void Update()
    {
        if (weapon != null && weapon.IsReloading)
        {
            reloadFill.fillAmount = weapon.ReloadProgress;
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
