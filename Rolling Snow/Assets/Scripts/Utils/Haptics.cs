using UnityEngine;

public static class Haptics
{
    private static float nextAllowedTime;

    // 상황별로 cooldown만 다르게 쓰는 방식이 가장 가볍고 효과가 좋습니다.
    public static void Tap(float cooldownSeconds = 0.12f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + cooldownSeconds;
        Handheld.Vibrate();
#endif
    }
}
