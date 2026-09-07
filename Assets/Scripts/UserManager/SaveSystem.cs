using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string fileName = "gamedata.json";

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetPath();
        File.WriteAllText(path, json);
    }

    public static GameData Load()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            GameData data = JsonUtility.FromJson<GameData>(json);
            return data;
        }

        return new GameData();
    }

    public static void Delete()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
