using UnityEngine;

public static class SkinStorage
{
    public const string DefaultUnlockPrefix = "Skin.Unlocked.";
    public const string DefaultEquippedKey = "Skin.Equipped";

    public static string GetSkinId(SkinEntry entry, int index)
    {
        if (entry != null)
        {
            if (!string.IsNullOrEmpty(entry.id))
                return entry.id;
            if (entry.sprite != null && !string.IsNullOrEmpty(entry.sprite.name))
                return entry.sprite.name;
        }

        return $"skin_{index}";
    }

    public static string GetSkinDisplayName(SkinEntry entry)
    {
        if (entry == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(entry.displayName))
            return entry.displayName;

        if (!string.IsNullOrEmpty(entry.id))
            return entry.id;

        if (entry.sprite != null && !string.IsNullOrEmpty(entry.sprite.name))
            return entry.sprite.name;

        return string.Empty;
    }

    public static bool IsUnlocked(string skinId, string defaultSkinId, string unlockPrefix)
    {
        if (string.IsNullOrEmpty(skinId))
            return false;

        if (!string.IsNullOrEmpty(defaultSkinId) && skinId == defaultSkinId)
            return true;

        string prefix = string.IsNullOrEmpty(unlockPrefix) ? DefaultUnlockPrefix : unlockPrefix;
        return PlayerPrefs.GetInt($"{prefix}{skinId}", 0) > 0;
    }

    public static void Unlock(string skinId, string unlockPrefix)
    {
        if (string.IsNullOrEmpty(skinId))
            return;

        string prefix = string.IsNullOrEmpty(unlockPrefix) ? DefaultUnlockPrefix : unlockPrefix;
        PlayerPrefs.SetInt($"{prefix}{skinId}", 1);
    }

    public static string GetEquippedSkinId(string defaultSkinId, string equippedKey)
    {
        string key = string.IsNullOrEmpty(equippedKey) ? DefaultEquippedKey : equippedKey;
        string stored = PlayerPrefs.GetString(key, string.Empty);
        return string.IsNullOrEmpty(stored) ? defaultSkinId : stored;
    }

    public static void SetEquippedSkinId(string skinId, string equippedKey)
    {
        if (string.IsNullOrEmpty(skinId))
            return;

        string key = string.IsNullOrEmpty(equippedKey) ? DefaultEquippedKey : equippedKey;
        PlayerPrefs.SetString(key, skinId);
        PlayerPrefs.Save();
    }
}
