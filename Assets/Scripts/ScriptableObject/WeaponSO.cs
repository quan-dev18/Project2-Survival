using System.Collections.Generic;
using UnityEngine;

// ──────────────────── Dữ liệu Skin của 1 khẩu súng ────────────────────
[System.Serializable]
public class WeaponSkinData
{
    [Tooltip("ID duy nhất của skin trong phạm vi 1 khẩu súng (ví dụ: 'default', 'gold', 'legend').")]
    public string skinID;

    [Tooltip("Tên hiển thị của skin.")]
    public string skinName;

    [Tooltip("Icon skin hiển thị trên UI Shop.")]
    public Sprite skinIcon;

    [Tooltip("Sprite súng khi trang bị skin này trong gameplay.")]
    public Sprite weaponSprite;

    [Tooltip("Giá vàng để mua skin.")]
    public int price;
}

[CreateAssetMenu(fileName = "WeaponSO", menuName = "WeaponStats")]
public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID duy nhất của vũ khí, dùng làm tiền tố cho skin composite ID (vd: 'ak47').")]
    [SerializeField] private string weaponID;
    public string WeaponID => weaponID;

    [Header("UI & Display Info")]
    [SerializeField] private Sprite weaponIcon;
    public Sprite WeaponIcon => weaponIcon;
    [SerializeField] private bool isUnlocked;
    public bool IsUnlocked => isUnlocked;
    public void SetUnlocked(bool val) => isUnlocked = val;

    [SerializeField] private int goldCost = 100;
    public int GoldCost => goldCost;

    [Header("Skins")]
    [Tooltip("Danh sách skin có sẵn cho khẩu súng này. Skin đầu tiên nên là skin mặc định (price = 0).")]
    [SerializeField] private List<WeaponSkinData> skinList = new List<WeaponSkinData>();
    public List<WeaponSkinData> SkinList => skinList;

    [Header("Audio")]
    [Tooltip("Tiếng bắn (SFX) của loại súng này. GunAudio sẽ tự đọc.")]
    [SerializeField] private AudioClip shootSFX;
    public AudioClip ShootSFX => shootSFX;

    [Tooltip("Tiếng lên đạn (SFX) của loại súng này.")]
    [SerializeField] private AudioClip reloadSFX;
    public AudioClip ReloadSFX => reloadSFX;

    [Header("Weapon Stats")]
    [SerializeField] private string weaponName;
    public string WeaponName => weaponName;
    [SerializeField] private float fireRate;
    public float FireRate => fireRate;
    [SerializeField] private float fireRange;
    public float FireRange => fireRange;
    [SerializeField] private float reloadTime;
    public float ReloadTime => reloadTime;
    [SerializeField] private int magazineSize;
    public int MagazineSize => magazineSize;
    [SerializeField] private int bulletCount; // The number of bullets fired per shot
    public int BulletCount => bulletCount;
    [SerializeField] private int spread; // The number of bullets fired in a spread pattern
    public int Spread => spread;

    [Header("Bullet Data")]
    [Tooltip("BulletSO chứa chỉ số đạn (damage, speed...). GunShowcaseUI sẽ đọc ATK từ đây.")]
    [SerializeField] private BulletSO bulletSO;
    public BulletSO BulletSO => bulletSO;
}
