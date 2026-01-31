using UnityEngine;

public static class AchievementStorage
{
    public const string DefaultCompletedPrefix = "Achievement.Completed.";
    public const string DefaultClaimedPrefix = "Achievement.Claimed.";

    public static bool IsCompleted(string id, AchievementCatalog catalog, string fallbackPrefix = null)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return PlayerPrefs.GetInt(GetCompletedKey(id, catalog, fallbackPrefix), 0) > 0;
    }

    public static bool IsClaimed(string id, AchievementCatalog catalog, string fallbackPrefix = null)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return PlayerPrefs.GetInt(GetClaimedKey(id, catalog, fallbackPrefix), 0) > 0;
    }

    public static void SetCompleted(string id, AchievementCatalog catalog, string fallbackPrefix = null, bool value = true)
    {
        if (string.IsNullOrEmpty(id))
            return;

        PlayerPrefs.SetInt(GetCompletedKey(id, catalog, fallbackPrefix), value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetClaimed(string id, AchievementCatalog catalog, string fallbackPrefix = null, bool value = true)
    {
        if (string.IsNullOrEmpty(id))
            return;

        PlayerPrefs.SetInt(GetClaimedKey(id, catalog, fallbackPrefix), value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static string GetCompletedKey(string id, AchievementCatalog catalog, string fallbackPrefix = null)
    {
        string prefix = ResolveCompletedPrefix(catalog, fallbackPrefix);
        return string.IsNullOrEmpty(prefix) ? id : $"{prefix}{id}";
    }

    public static string GetClaimedKey(string id, AchievementCatalog catalog, string fallbackPrefix = null)
    {
        string prefix = ResolveClaimedPrefix(catalog, fallbackPrefix);
        return string.IsNullOrEmpty(prefix) ? id : $"{prefix}{id}";
    }

    static string ResolveCompletedPrefix(AchievementCatalog catalog, string fallbackPrefix)
    {
        if (catalog != null && !string.IsNullOrEmpty(catalog.prefsPrefix))
            return catalog.prefsPrefix;
        if (!string.IsNullOrEmpty(fallbackPrefix))
            return fallbackPrefix;
        return DefaultCompletedPrefix;
    }

    static string ResolveClaimedPrefix(AchievementCatalog catalog, string fallbackPrefix)
    {
        if (catalog != null && !string.IsNullOrEmpty(catalog.claimedPrefsPrefix))
            return catalog.claimedPrefsPrefix;
        if (!string.IsNullOrEmpty(fallbackPrefix))
            return fallbackPrefix;
        return DefaultClaimedPrefix;
    }
}
