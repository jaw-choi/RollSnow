using UnityEngine;

public static class Haptics
{
    private static float nextAllowedTime;

    // Global cooldown keeps haptics light and avoids spamming.
    public static void Tap(float cooldownSeconds = 0.12f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var settings = SettingsManager.Instance;
        if (settings != null && !settings.Haptics) return;
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + cooldownSeconds;
        Handheld.Vibrate();
#endif
    }
}
