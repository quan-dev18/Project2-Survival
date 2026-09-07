using System;

[Serializable]
public class GameData
{
    public int playerGold;
    public int selectedHeroIndex;
    public int selectedWeaponIndex;
    public bool[] unlockedHeroes;
    public bool[] unlockedWeapons;

    public GameData()
    {
        playerGold = 9999;
        selectedHeroIndex = 0;
        selectedWeaponIndex = 0;
        unlockedHeroes = new bool[0];
        unlockedWeapons = new bool[0];
    }
}
