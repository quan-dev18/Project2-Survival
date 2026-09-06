using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroSelectData", menuName = "Selection/Select Hero Data")]
public class HeroSelectSO : ScriptableObject
{
    public Sprite heroIcon;          // Icon Avatar mặt nhân vật
    public GameObject previewPrefab; // Prefab nhân vật (chứa cả Hero + Des)
    public bool isUnlocked;          // true: Đã mở khóa, false: Bị khóa

    [SerializeField] private int goldCost = 100;
    public int GoldCost => goldCost;
    public void SetUnlocked(bool val) => isUnlocked = val;
}
