using UnityEngine;
using UnityEngine.UI;

public class CharIconUI : MonoBehaviour
{
    [Header("Character Icons")]
    [SerializeField] private Image charIcon;
    [SerializeField] private Sprite[] heroIcons; // index 0,1,2 ứng với Char1,2,3

    private PlayerEquipment playerEquipment;

    private void Start()
    {
        playerEquipment = FindObjectOfType<PlayerEquipment>();
        UpdateIcon();
    }

    private void Update()
    {
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (charIcon == null || heroIcons == null || heroIcons.Length == 0) return;
        if (playerEquipment == null) return;

        int idx = playerEquipment.ActiveMeshIndex;
        if (idx < 0 || idx >= heroIcons.Length) return;
        if (charIcon.sprite != heroIcons[idx])
            charIcon.sprite = heroIcons[idx];
    }
}
