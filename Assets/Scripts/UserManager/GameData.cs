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

    public GameData()
    {
        version = 1;
        playerGold = 9999;
        selectedHeroIndex = 0;
        selectedWeaponIndex = 0;
        unlockedHeroes = new bool[0];
        unlockedWeapons = new bool[0];
        perkIds = new List<string>();
        perkLevels = new List<int>();

        stageIds = new List<string>();
        stageBestProgress = new List<float>();
    }
}
