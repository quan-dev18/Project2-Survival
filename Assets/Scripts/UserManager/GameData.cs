using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public int version;
    public int playerGold;
    public int selectedHeroIndex;
    public int selectedWeaponIndex;
    public bool[] unlockedHeroes;
    public bool[] unlockedWeapons;
    public List<string> perkIds;
    public List<int> perkLevels;

    // Kỷ lục tiến trình cao nhất của từng Stage (song song: stageBestProgress[i] ứng với stageIds[i]).
    public List<string> stageIds;
    public List<float> stageBestProgress;

    // StageID đã nhận thưởng "hoàn thành 100% lần đầu" (mỗi stage chỉ nhận 1 lần).
    public List<string> claimedStageRewards;

    // ──────── Skin Shop ────────
    // Danh sách composite ID (weaponID_skinID) các skin đã mua.
    public List<string> ownedSkins;
    // Dictionary dạng parallel list: equippedSkinIds[i] = skinID đang trang bị của equippedWeaponIds[i].
    public List<string> equippedWeaponIds;
    public List<string> equippedSkinIds;

    // ──────── Ad-Based Unlocking ────────
    public List<string> adWatchKeys;
    public List<int> adWatchCounts;

    // ──────── Tutorial ────────
    public bool tutorialCompleted;

    public GameData()
    {
        version = 1;
        playerGold = 10000;
        selectedHeroIndex = 0;
        selectedWeaponIndex = 0;
        unlockedHeroes = new bool[0];
        unlockedWeapons = new bool[0];
        perkIds = new List<string>();
        perkLevels = new List<int>();

        stageIds = new List<string>();
        stageBestProgress = new List<float>();
        claimedStageRewards = new List<string>();

        ownedSkins = new List<string>();
        equippedWeaponIds = new List<string>();
        equippedSkinIds = new List<string>();

        adWatchKeys = new List<string>();
        adWatchCounts = new List<int>();
    }
}
