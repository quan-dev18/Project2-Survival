using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string fileName = "gamedata.json";
    private const int CURRENT_VERSION = 1;

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    private static void EnsureDirectory()
    {
        string dir = Path.GetDirectoryName(GetPath());
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void Save(GameData data)
    {
        if (data == null) return;
        try
        {
            data.version = CURRENT_VERSION;
            string json = JsonUtility.ToJson(data, true);
            string path = GetPath();
            EnsureDirectory();
            // atomic write via temp file
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    public static GameData Load()
    {
        string path = GetPath();
        if (!File.Exists(path))
            return new GameData();

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new GameData();

            GameData data = JsonUtility.FromJson<GameData>(json);
            if (data == null)
                return new GameData();

            // ensure arrays never null (handles old saves)
            data.unlockedHeroes ??= new bool[0];
            data.unlockedWeapons ??= new bool[0];

            // version migration placeholder
            if (data.version == 0)
                data.version = CURRENT_VERSION;

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed, using defaults: {e.Message}");
            try
            {
                string backup = GetPath() + ".corrupt." + System.DateTime.Now.ToString("yyyyMMddHHmmss");
                if (File.Exists(path)) File.Copy(path, backup, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to create corrupt file backup: {ex.Message}");
            }
            return new GameData();
        }
    }

    public static void Delete()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch (System.Exception e) { Debug.LogError($"[SaveSystem] Delete failed: {e.Message}"); }
        }
    }

    public static bool Exists() => File.Exists(GetPath());
}
