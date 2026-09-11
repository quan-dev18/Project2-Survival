using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Character Meshes")]
    [SerializeField] private List<GameObject> characterMeshes = new List<GameObject>();

    [Header("Weapons")]
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerUI playerUI;

    private int activeMeshIndex = -1;
    private int activeWeaponIndex = -1;

    public static int SelectedHeroIndex = 0;
    public static int SelectedWeaponIndex = 0;

    public int ActiveMeshIndex => activeMeshIndex;
    public int ActiveWeaponIndex => activeWeaponIndex;

    private void Awake()
    {
        if (UserData.Instance != null)
        {
            SelectedHeroIndex = UserData.Instance.SelectedHeroIndex;
            SelectedWeaponIndex = UserData.Instance.SelectedWeaponIndex;
        }

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        if (playerUI == null)
            playerUI = GetComponentInChildren<PlayerUI>();

        AutoCollectMeshes();
        AutoCollectWeapons();
    }

    private void Start()
    {
        if (playerStats != null)
            playerStats.RegisterAllWeapons();

        if (characterMeshes.Count > 0)
        {
            int heroIdx = Mathf.Clamp(SelectedHeroIndex, 0, characterMeshes.Count - 1);
            ActivateMesh(heroIdx);
        }

        if (weapons.Count > 0)
        {
            int weaponIdx = ResolveWeaponIndex();
            ActivateWeapon(weaponIdx);
        }
    }

    private int ResolveWeaponIndex()
    {
        string savedName = PlayerPrefs.GetString("SelectedWeaponName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponController ctrl = weapons[i].GetComponentInChildren<WeaponController>(true);
                if (ctrl != null && ctrl.WeaponStats != null && ctrl.WeaponStats.name == savedName)
                    return i;
                FlamethrowerController flame = weapons[i].GetComponentInChildren<FlamethrowerController>(true);
                if (flame != null && flame.WeaponStats != null && flame.WeaponStats.name == savedName)
                    return i;
            }
        }

        return Mathf.Clamp(SelectedWeaponIndex, 0, weapons.Count - 1);
    }

    private void AutoCollectMeshes()
    {
        if (characterMeshes.Count > 0) return;

        Transform meshesContainer = transform.Find("Meshes");
        if (meshesContainer == null) return;

        foreach (Transform child in meshesContainer)
        {
            characterMeshes.Add(child.gameObject);
        }
    }

    private void AutoCollectWeapons()
    {
        Transform weaponsContainer = transform.Find("Weapons");
        if (weaponsContainer == null) return;

        // Sync with hierarchy: add any new weapon GameObjects not yet in list (e.g., Flamethrower added after)
        var seen = new System.Collections.Generic.HashSet<GameObject>(weapons);
        foreach (Transform child in weaponsContainer)
        {
            if (!seen.Contains(child.gameObject))
                weapons.Add(child.gameObject);
        }
        // Remove destroyed/null entries
        weapons.RemoveAll(w => w == null);
    }

    public void ActivateMesh(int index)
    {
        if (index < 0 || index >= characterMeshes.Count) return;

        for (int i = 0; i < characterMeshes.Count; i++)
            characterMeshes[i].SetActive(i == index);

        activeMeshIndex = index;

        if (playerMovement != null)
        {
            Animator anim = characterMeshes[index].GetComponentInChildren<Animator>();
            if (anim != null)
                playerMovement.SetAnimator(anim);

            playerMovement.SetMesh(characterMeshes[index].transform);
        }
    }

    public void ActivateWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count) return;

        for (int i = 0; i < weapons.Count; i++)
            weapons[i].SetActive(i == index);

        activeWeaponIndex = index;

        WeaponController weaponCtrl = weapons[index].GetComponentInChildren<WeaponController>(true);
        if (weaponCtrl == null)
            weaponCtrl = weapons[index].GetComponent<WeaponController>();
        FlamethrowerController flameCtrl = weapons[index].GetComponentInChildren<FlamethrowerController>(true);
        if (flameCtrl == null)
            flameCtrl = weapons[index].GetComponent<FlamethrowerController>();

        if (playerStats != null)
            playerStats.SetActiveWeaponIndex(index);

        if (playerUI != null)
        {
            if (weaponCtrl != null) playerUI.SetWeapon(weaponCtrl);
            else if (flameCtrl != null) playerUI.SetFlamethrower(flameCtrl);
            else playerUI.SetWeapon(null);
        }

        if (playerMovement != null)
        {
            if (weaponCtrl != null) weaponCtrl.SetPlayerMovement(playerMovement);
            if (flameCtrl != null) flameCtrl.SetPlayerMovement(playerMovement);
        }
    }

    public void ActivateMeshByName(string meshName)
    {
        for (int i = 0; i < characterMeshes.Count; i++)
        {
            if (characterMeshes[i].name == meshName)
            {
                ActivateMesh(i);
                return;
            }
        }
    }

    public void ActivateWeaponByName(string weaponName)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].name == weaponName)
            {
                ActivateWeapon(i);
                return;
            }
        }
    }
}
