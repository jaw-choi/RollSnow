using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettingsSO", menuName = "Scriptable Objects/AudioSettingsSO")]
public class AudioSettingsSO : ScriptableObject
{
    [Range(0f, 1f)] public float master = 1f;
    [Range(0f, 1f)] public float music = 0.8f;
    [Range(0f, 1f)] public float sfx = 0.8f;

    public bool haptics = true;
    public bool screenShake = true;
    public bool hitStop = true;
    public int targetFps = 60; // 60 or 120
}
