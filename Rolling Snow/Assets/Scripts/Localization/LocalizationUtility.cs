public static class LocalizationUtility
{
    public static GameLanguage GetCurrentLanguage()
    {
        var settings = SettingsManager.Instance;
        return settings != null ? settings.Language : GameLanguage.Korean;
    }

    public static string Resolve(LocalizedString localized, string fallback)
    {
        return localized.HasAny ? localized.Get(GetCurrentLanguage()) : fallback;
    }
}
