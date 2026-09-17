using System.Collections.Generic;
using UnityEngine;

// ──────────────────── Độ hiếm (Tier) của skin ────────────────────
public enum SkinTier
{
    Common,     // Phổ thông
    Rare,       // Hiếm
    Epic,       // Sử thi
    Legendary   // Huyền thoại
}

// ──────────────────── Dữ liệu Skin của 1 khẩu súng ────────────────────
[System.Serializable]
public class WeaponSkinData
{
    [Tooltip("ID duy nhất của skin trong phạm vi 1 khẩu súng (ví dụ: 'default', 'gold', 'legend').")]
    public string skinID;

    [Tooltip("Đánh dấu đây là SKIN MẶC ĐỊNH của súng (luôn được sở hữu sẵn). " +
             "Nếu không có skin nào được đánh dấu, hệ thống tự dùng SPRITE GỐC của vũ khí làm skin mặc định.")]
    public bool isDefault;

    [Tooltip("Tên hiển thị của skin.")]
    public string skinName;

    [Tooltip("Độ hiếm của skin, dùng để quyết định màu nền/khung trong UI.")]
    public SkinTier tier;

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
    /// <summary>ID cố định của skin mặc định (dùng sprite gốc vũ khí).</summary>
    public const string DefaultSkinId = "default";

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
    [Tooltip("Danh sách skin mua được. Skin mặc định (sprite gốc vũ khí) luôn tồn tại sẵn, không cần thêm vào đây.")]
    [SerializeField] private List<WeaponSkinData> skinList = new List<WeaponSkinData>();
    public List<WeaponSkinData> SkinList => skinList;

    /// <summary>
    /// Skin mặc định của súng. Quy tắc:
    ///   1) Nếu có skin authored được đánh dấu <c>isDefault = true</c> → dùng skin đó.
    ///   2) Ngược lại → trả về skin ẢO dùng chính SPRITE GỐC của vũ khí (<c>weaponIcon</c>),
    ///      nên mọi súng luôn có sẵn lựa chọn "Mặc định" mà không cần author trong skinList.
    /// </summary>
    public WeaponSkinData DefaultSkin
    {
        get
        {
            if (skinList != null)
            {
                for (int i = 0; i < skinList.Count; i++)
                {
                    if (skinList[i] != null && skinList[i].isDefault)
                        return skinList[i];
                }
            }

            // Skin mặc định = sprite gốc của vũ khí.
            return new WeaponSkinData
            {
                skinID = DefaultSkinId,
                skinName = "Mặc định",
                isDefault = true,
                skinIcon = weaponIcon,
                weaponSprite = weaponIcon,
                price = 0,
                tier = SkinTier.Common
            };
        }
    }

    /// <summary>
    /// Sprite gameplay của skin đang trang bị. Trả về <c>null</c> khi không có skin authored
    /// tương ứng (skin mặc định → giữ nguyên sprite gốc của prefab vũ khí).
    /// </summary>
    public Sprite GetEquippedGameplaySprite(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || skinList == null) return null;

        for (int i = 0; i < skinList.Count; i++)
        {
            WeaponSkinData s = skinList[i];
            if (s != null && s.skinID == skinId && s.weaponSprite != null)
                return s.weaponSprite;
        }

        return null;
    }

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
    [SerializeField] private int basePierce; // Innate pierce (e.g. sniper rifles)
    public int BasePierce => basePierce;

    [Header("Bullet Data")]
    [Tooltip("BulletSO chứa chỉ số đạn (damage, speed...). GunShowcaseUI sẽ đọc ATK từ đây.")]
    [SerializeField] private BulletSO bulletSO;
    public BulletSO BulletSO => bulletSO;
}
