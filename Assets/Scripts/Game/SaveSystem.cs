using System;
using UnityEngine;

/// <summary>
/// Loads and saves the <see cref="SaveData"/> as one JSON string in PlayerPrefs.
/// PlayerPrefs is used (not a file in persistentDataPath) because on WebGL it is written to the browser's storage by Unity itself;
/// files there are written asynchronously and can be lost when the tab closes. On Windows and Android it just works too.
/// </summary>
public static class SaveSystem
{
    private const string Key = "SaveData";

    // The score used to be stored alone under this key before SaveData existed.
    private const string LegacyBestScoreKey = "BestScore";

    private static SaveData data;

    /// <summary>The loaded save. Change its fields, then call <see cref="Save"/>.</summary>
    public static SaveData Data
    {
        get
        {
            data ??= Load();
            return data;
        }
    }

    /// <summary>Writes the current <see cref="Data"/> to storage.</summary>
    public static void Save()
    {
        try
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            // Storage can be blocked (for example some private browser modes). The game keeps working with the in-memory data.
            Debug.LogWarning($"Could not save the game data: {exception.Message}");
        }
    }

    /// <summary>Forgets everything that was saved.</summary>
    public static void Clear()
    {
        data = new SaveData();
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(LegacyBestScoreKey);
        PlayerPrefs.Save();
    }

    private static SaveData Load()
    {
        if (PlayerPrefs.HasKey(Key))
        {
            return Parse(PlayerPrefs.GetString(Key));
        }

        if (PlayerPrefs.HasKey(LegacyBestScoreKey))
        {
            return MigrateLegacyBestScore();
        }

        return new SaveData();
    }

    private static SaveData Parse(string json)
    {
        try
        {
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded != null)
            {
                loaded.bestScore = Mathf.Max(0, loaded.bestScore);
                loaded.masterVolume = Mathf.Clamp01(loaded.masterVolume);
                return loaded;
            }
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"The saved game data is unreadable and is ignored: {exception.Message}");
        }

        // Not overwritten here; the next Save replaces it.
        return new SaveData();
    }

    private static SaveData MigrateLegacyBestScore()
    {
        var migrated = new SaveData { bestScore = Mathf.Max(0, PlayerPrefs.GetInt(LegacyBestScoreKey, 0)) };
        data = migrated;
        Save();
        PlayerPrefs.DeleteKey(LegacyBestScoreKey);
        PlayerPrefs.Save();
        return migrated;
    }
}
