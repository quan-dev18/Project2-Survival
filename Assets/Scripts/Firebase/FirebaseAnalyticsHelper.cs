#if !UNITY_WEBGL
using Firebase.Analytics;
#endif
using UnityEngine;

/// <summary>
/// Centralized Firebase Analytics event logging.
/// All methods are static — call from game scripts with: FirebaseAnalyticsHelper.LogXxx(...)
/// </summary>
public static class FirebaseAnalyticsHelper
{
#if !UNITY_WEBGL
    // ──────────────────── P1: Core Gameplay ────────────────────

    public static void LogGameSessionStart(string heroId, string weaponId, string stageId, string skinId)
    {
        FirebaseAnalytics.LogEvent("game_session_start",
            new Parameter("hero_id", heroId),
            new Parameter("weapon_id", weaponId),
            new Parameter("stage_id", stageId),
            new Parameter("skin_id", skinId));
    }

    public static void LogGameSessionEnd(string result, float timeAlive, int killCount, int goldEarned,
        string heroId, string weaponId, string stageId, int highestLevel)
    {
        FirebaseAnalytics.LogEvent("game_session_end",
            new Parameter("result", result),
            new Parameter("time_alive", (long)timeAlive),
            new Parameter("kill_count", killCount),
            new Parameter("gold_earned", goldEarned),
            new Parameter("hero_id", heroId),
            new Parameter("weapon_id", weaponId),
            new Parameter("stage_id", stageId),
            new Parameter("highest_level", highestLevel));
    }

    public static void LogLevelUp(int newLevel, float timeAlive, int totalUpgrades)
    {
        FirebaseAnalytics.LogEvent("level_up",
            new Parameter("new_level", newLevel),
            new Parameter("time_alive", (long)timeAlive),
            new Parameter("total_upgrades_owned", totalUpgrades));
    }

    public static void LogUpgradeChosen(string upgradeName, int upgradeTier, int choiceSlot, int totalUpgrades)
    {
        FirebaseAnalytics.LogEvent("upgrade_chosen",
            new Parameter("upgrade_name", upgradeName),
            new Parameter("upgrade_tier", upgradeTier),
            new Parameter("choice_slot", choiceSlot),
            new Parameter("total_upgrades", totalUpgrades));
    }

    public static void LogEnemyKilled(string enemyType, int playerLevel, float timeAlive)
    {
        FirebaseAnalytics.LogEvent("enemy_killed",
            new Parameter("enemy_type", enemyType),
            new Parameter("player_level", playerLevel),
            new Parameter("time_alive", (long)timeAlive));
    }

    public static void LogBossKilled(string bossType, float timeAlive, int playerLevel, int killCount)
    {
        FirebaseAnalytics.LogEvent("boss_killed",
            new Parameter("boss_type", bossType),
            new Parameter("time_alive", (long)timeAlive),
            new Parameter("player_level", playerLevel),
            new Parameter("kill_count", killCount));
    }

    public static void LogPlayerDied(float timeAlive, int killCount, int highestLevel, string stageId)
    {
        FirebaseAnalytics.LogEvent("player_died",
            new Parameter("time_alive", (long)timeAlive),
            new Parameter("kill_count", killCount),
            new Parameter("highest_level", highestLevel),
            new Parameter("stage_id", stageId));
    }

    public static void LogPlayerDamaged(float damageAmount, float remainingHealth, string enemyType)
    {
        FirebaseAnalytics.LogEvent("player_damaged",
            new Parameter("damage_amount", damageAmount),
            new Parameter("remaining_health", remainingHealth),
            new Parameter("enemy_type", enemyType));
    }

    // ──────────────────── P2: Economy ────────────────────

    public static void LogGoldEarned(int amount, string source, int totalSessionGold)
    {
        FirebaseAnalytics.LogEvent("gold_earned",
            new Parameter("amount", amount),
            new Parameter("source", source),
            new Parameter("total_session_gold", totalSessionGold));
    }

    public static void LogGoldSpent(int amount, string itemType, string itemId, int remainingGold)
    {
        FirebaseAnalytics.LogEvent("gold_spent",
            new Parameter("amount", amount),
            new Parameter("item_type", itemType),
            new Parameter("item_id", itemId),
            new Parameter("remaining_gold", remainingGold));
    }

    public static void LogDailyRewardClaimed(int dayNumber, int streakCount, string rewardType, int rewardAmount, string rewardName)
    {
        FirebaseAnalytics.LogEvent("daily_reward_claimed",
            new Parameter("day_number", dayNumber),
            new Parameter("streak_count", streakCount),
            new Parameter("reward_type", rewardType),
            new Parameter("reward_amount", rewardAmount),
            new Parameter("reward_name", rewardName));
    }

    public static void LogDailyRewardStreakReset(int previousStreak, string reason)
    {
        FirebaseAnalytics.LogEvent("daily_reward_streak_reset",
            new Parameter("previous_streak", previousStreak),
            new Parameter("reason", reason));
    }

    public static void LogAdRewardedShown(string adType)
    {
        FirebaseAnalytics.LogEvent("ad_rewarded_shown",
            new Parameter("ad_type", adType));
    }

    public static void LogAdRewardedCompleted(string adType, int rewardAmount)
    {
        FirebaseAnalytics.LogEvent("ad_rewarded_completed",
            new Parameter("ad_type", adType),
            new Parameter("reward_amount", rewardAmount));
    }

    public static void LogAdRewardedFailed(string adType, string reason)
    {
        FirebaseAnalytics.LogEvent("ad_rewarded_failed",
            new Parameter("ad_type", adType),
            new Parameter("reason", reason));
    }

    // ──────────────────── P3: Meta-Game ────────────────────

    public static void LogHeroSelected(string heroId, string heroName)
    {
        FirebaseAnalytics.LogEvent("hero_selected",
            new Parameter("hero_id", heroId),
            new Parameter("hero_name", heroName));
    }

    public static void LogHeroUnlocked(string heroId, string heroName, int goldCost)
    {
        FirebaseAnalytics.LogEvent("hero_unlocked",
            new Parameter("hero_id", heroId),
            new Parameter("hero_name", heroName),
            new Parameter("gold_cost", goldCost));
    }

    public static void LogWeaponSelected(string weaponId, string weaponName)
    {
        FirebaseAnalytics.LogEvent("weapon_selected",
            new Parameter("weapon_id", weaponId),
            new Parameter("weapon_name", weaponName));
    }

    public static void LogWeaponUnlocked(string weaponId, string weaponName, int goldCost)
    {
        FirebaseAnalytics.LogEvent("weapon_unlocked",
            new Parameter("weapon_id", weaponId),
            new Parameter("weapon_name", weaponName),
            new Parameter("gold_cost", goldCost));
    }

    public static void LogSkinPurchased(string weaponId, string skinId, string skinTier, int price)
    {
        FirebaseAnalytics.LogEvent("skin_purchased",
            new Parameter("weapon_id", weaponId),
            new Parameter("skin_id", skinId),
            new Parameter("skin_tier", skinTier),
            new Parameter("price", price));
    }

    public static void LogSkinEquipped(string weaponId, string skinId, string skinTier)
    {
        FirebaseAnalytics.LogEvent("skin_equipped",
            new Parameter("weapon_id", weaponId),
            new Parameter("skin_id", skinId),
            new Parameter("skin_tier", skinTier));
    }

    public static void LogPerkUpgraded(string perkId, string perkName, int newLevel, int goldCost)
    {
        FirebaseAnalytics.LogEvent("perk_upgraded",
            new Parameter("perk_id", perkId),
            new Parameter("perk_name", perkName),
            new Parameter("new_level", newLevel),
            new Parameter("gold_cost", goldCost));
    }

    public static void LogMapSelected(string mapId, string stageId, int mapIndex)
    {
        FirebaseAnalytics.LogEvent("map_selected",
            new Parameter("map_id", mapId),
            new Parameter("stage_id", stageId),
            new Parameter("map_index", mapIndex));
    }

    public static void LogMapUnlocked(string mapId, string stageId)
    {
        FirebaseAnalytics.LogEvent("map_unlocked",
            new Parameter("map_id", mapId),
            new Parameter("stage_id", stageId));
    }

    // ──────────────────── P4: Progression ────────────────────

    public static void LogSynergyActivated(string synergyName, string ownedUpgrades)
    {
        FirebaseAnalytics.LogEvent("synergy_activated",
            new Parameter("synergy_name", synergyName),
            new Parameter("owned_upgrades", ownedUpgrades));
    }

    public static void LogStageProgressRecord(string stageId, float progress, bool isNewBest)
    {
        FirebaseAnalytics.LogEvent("stage_progress_record",
            new Parameter("stage_id", stageId),
            new Parameter("progress", (double)progress),
            new Parameter("is_new_best", isNewBest ? "true" : "false"));
    }

    public static void LogFirstClearRewardClaimed(string stageId, int rewardAmount)
    {
        FirebaseAnalytics.LogEvent("first_clear_reward_claimed",
            new Parameter("stage_id", stageId),
            new Parameter("reward_amount", rewardAmount));
    }

    public static void LogPropBroken(string propType, string dropType)
    {
        FirebaseAnalytics.LogEvent("prop_broken",
            new Parameter("prop_type", propType),
            new Parameter("drop_type", dropType));
    }

    public static void LogTutorialStepCompleted(int stepNumber, string stepName)
    {
        FirebaseAnalytics.LogEvent("tutorial_step_completed",
            new Parameter("step_number", stepNumber),
            new Parameter("step_name", stepName));
    }

    public static void LogTutorialCompleted()
    {
        FirebaseAnalytics.LogEvent("tutorial_completed");
    }

    public static void LogSettingChanged(string settingName, bool newValue)
    {
        FirebaseAnalytics.LogEvent("setting_changed",
            new Parameter("setting_name", settingName),
            new Parameter("new_value", newValue ? "true" : "false"));
    }

    // ──────────────────── P5: First-Time Events ────────────────────

    public static void LogFirstGameStart()
    {
        FirebaseAnalytics.LogEvent("first_game_start");
    }

    public static void LogFirstGameComplete(string result)
    {
        FirebaseAnalytics.LogEvent("first_game_complete",
            new Parameter("result", result));
    }

    public static void LogFirstHeroUnlocked(string heroId)
    {
        FirebaseAnalytics.LogEvent("first_hero_unlocked",
            new Parameter("hero_id", heroId));
    }

    public static void LogFirstWeaponUnlocked(string weaponId)
    {
        FirebaseAnalytics.LogEvent("first_weapon_unlocked",
            new Parameter("weapon_id", weaponId));
    }

    public static void LogFirstSkinPurchased(string skinId)
    {
        FirebaseAnalytics.LogEvent("first_skin_purchased",
            new Parameter("skin_id", skinId));
    }

    public static void LogFirstPerkUpgraded(string perkId)
    {
        FirebaseAnalytics.LogEvent("first_perk_upgraded",
            new Parameter("perk_id", perkId));
    }

    // ──────────────────── P6: Remote Config & Additional Ads ────────────────────

    public static void LogDebugModeActivated()
    {
        FirebaseAnalytics.LogEvent("debug_mode_activated");
    }

    public static void LogDebugModeDeactivated()
    {
        FirebaseAnalytics.LogEvent("debug_mode_deactivated");
    }

    public static void LogInterstitialAdShown()
    {
        FirebaseAnalytics.LogEvent("interstitial_ad_shown");
    }

    public static void LogInterstitialAdClicked()
    {
        FirebaseAnalytics.LogEvent("interstitial_ad_clicked");
    }

    public static void LogAppOpenAdShown()
    {
        FirebaseAnalytics.LogEvent("app_open_ad_shown");
    }

    public static void LogHeroUnlockedByAd(string heroId, int adWatchCount)
    {
        FirebaseAnalytics.LogEvent("hero_unlocked_by_ad",
            new Parameter("hero_id", heroId),
            new Parameter("ad_watch_count", adWatchCount));
    }

    public static void LogSkinUnlockedByAd(string weaponId, string skinId, int adWatchCount)
    {
        FirebaseAnalytics.LogEvent("skin_unlocked_by_ad",
            new Parameter("weapon_id", weaponId),
            new Parameter("skin_id", skinId),
            new Parameter("ad_watch_count", adWatchCount));
    }

    public static void LogMaintenanceModeTriggered()
    {
        FirebaseAnalytics.LogEvent("maintenance_mode_triggered");
    }

    public static void LogForceUpdateTriggered(string requiredVersion, string currentVersion)
    {
        FirebaseAnalytics.LogEvent("force_update_triggered",
            new Parameter("required_version", requiredVersion),
            new Parameter("current_version", currentVersion));
    }

    public static void LogABTestGroupAssigned(string groupName)
    {
        FirebaseAnalytics.LogEvent("ab_test_group_assigned",
            new Parameter("group_name", groupName));
    }
#else
    // ──────────────────── WebGL Stubs (Firebase is not supported on WebGL) ────────────────────
    public static void LogGameSessionStart(string heroId, string weaponId, string stageId, string skinId) { }
    public static void LogGameSessionEnd(string result, float timeAlive, int killCount, int goldEarned,
        string heroId, string weaponId, string stageId, int highestLevel) { }
    public static void LogLevelUp(int newLevel, float timeAlive, int totalUpgrades) { }
    public static void LogUpgradeChosen(string upgradeName, int upgradeTier, int choiceSlot, int totalUpgrades) { }
    public static void LogEnemyKilled(string enemyType, int playerLevel, float timeAlive) { }
    public static void LogBossKilled(string bossType, float timeAlive, int playerLevel, int killCount) { }
    public static void LogPlayerDied(float timeAlive, int killCount, int highestLevel, string stageId) { }
    public static void LogPlayerDamaged(float damageAmount, float remainingHealth, string enemyType) { }

    public static void LogGoldEarned(int amount, string source, int totalSessionGold) { }
    public static void LogGoldSpent(int amount, string itemType, string itemId, int remainingGold) { }
    public static void LogDailyRewardClaimed(int dayNumber, int streakCount, string rewardType, int rewardAmount, string rewardName) { }
    public static void LogDailyRewardStreakReset(int previousStreak, string reason) { }
    public static void LogAdRewardedShown(string adType) { }
    public static void LogAdRewardedCompleted(string adType, int rewardAmount) { }
    public static void LogAdRewardedFailed(string adType, string reason) { }

    public static void LogHeroSelected(string heroId, string heroName) { }
    public static void LogHeroUnlocked(string heroId, string heroName, int goldCost) { }
    public static void LogWeaponSelected(string weaponId, string weaponName) { }
    public static void LogWeaponUnlocked(string weaponId, string weaponName, int goldCost) { }
    public static void LogSkinPurchased(string weaponId, string skinId, string skinTier, int price) { }
    public static void LogSkinEquipped(string weaponId, string skinId, string skinTier) { }
    public static void LogPerkUpgraded(string perkId, string perkName, int newLevel, int goldCost) { }
    public static void LogMapSelected(string mapId, string stageId, int mapIndex) { }
    public static void LogMapUnlocked(string mapId, string stageId) { }

    public static void LogSynergyActivated(string synergyName, string ownedUpgrades) { }
    public static void LogStageProgressRecord(string stageId, float progress, bool isNewBest) { }
    public static void LogFirstClearRewardClaimed(string stageId, int rewardAmount) { }
    public static void LogPropBroken(string propType, string dropType) { }
    public static void LogTutorialStepCompleted(int stepNumber, string stepName) { }
    public static void LogTutorialCompleted() { }
    public static void LogSettingChanged(string settingName, bool newValue) { }

    public static void LogFirstGameStart() { }
    public static void LogFirstGameComplete(string result) { }
    public static void LogFirstHeroUnlocked(string heroId) { }
    public static void LogFirstWeaponUnlocked(string weaponId) { }
    public static void LogFirstSkinPurchased(string skinId) { }
    public static void LogFirstPerkUpgraded(string perkId) { }

    public static void LogDebugModeActivated() { }
    public static void LogDebugModeDeactivated() { }
    public static void LogInterstitialAdShown() { }
    public static void LogInterstitialAdClicked() { }
    public static void LogAppOpenAdShown() { }
    public static void LogHeroUnlockedByAd(string heroId, int adWatchCount) { }
    public static void LogSkinUnlockedByAd(string weaponId, string skinId, int adWatchCount) { }
    public static void LogMaintenanceModeTriggered() { }
    public static void LogForceUpdateTriggered(string requiredVersion, string currentVersion) { }
    public static void LogABTestGroupAssigned(string groupName) { }
#endif
}
