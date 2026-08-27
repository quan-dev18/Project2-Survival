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

    public int ActiveMeshIndex => activeMeshIndex;
    public int ActiveWeaponIndex => activeWeaponIndex;

    private void Awake()
    {
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
        if (characterMeshes.Count > 0)
        {
            int heroIdx = Mathf.Clamp(SelectedHeroIndex, 0, characterMeshes.Count - 1);
            ActivateMesh(heroIdx);
        }

        if (weapons.Count > 0)
        {
            int weaponIdx = GetActiveWeaponIndex();
            ActivateWeapon(weaponIdx);
        }
    }

    private int GetActiveWeaponIndex()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].activeSelf)
                return i;
        }
        return 0;
    }

    private void AutoCollectMeshes()
    {
        if (characterMeshes.Count > 0) return;

        Transform meshesContainer = transform.Find("Meshes");
        if (meshesContainer == null) return;

        foreach (Transform child in meshesContainer)
        {
            if (child.gameObject.activeSelf || !child.gameObject.activeSelf)
                characterMeshes.Add(child.gameObject);
        }
    }

    private void AutoCollectWeapons()
    {
        if (weapons.Count > 0) return;

        Transform weaponsContainer = transform.Find("Weapons");
        if (weaponsContainer == null) return;

        foreach (Transform child in weaponsContainer)
        {
            weapons.Add(child.gameObject);
        }
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

        WeaponController weaponCtrl = weapons[index].GetComponentInChildren<WeaponController>();
        if (weaponCtrl == null)
            weaponCtrl = weapons[index].GetComponent<WeaponController>();

        if (playerStats != null)
            playerStats.SetWeapon(weaponCtrl);

        if (playerUI != null)
            playerUI.SetWeapon(weaponCtrl);

        if (playerMovement != null && weaponCtrl != null)
            weaponCtrl.SetPlayerMovement(playerMovement);
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
