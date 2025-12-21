using UnityEngine;
using UnityEngine.Audio;

public sealed class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Data/Defaults")]
    [SerializeField] private AudioSettingsSO defaults;

    [Header("Mixers")]
    [SerializeField] private AudioMixer audioMixer;


    private const string KeyMaster = "vol_master";
    private const string KeyMusic = "vol_music";
    private const string KeySfx = "vol_sfx";
    private const string KeyHap = "opt_haptics";
    private const string KeyShake = "opt_shake";
    private const string KeyHit = "opt_hitstop";
    private const string KeyFPS = "opt_fps";

    public float Master { get; private set; }
    public float Music { get; private set; }
    public float Sfx { get; private set; }
    public bool Haptics { get; private set; }
    public bool ScreenShake { get; private set; }
    public bool HitStop { get; private set; }
    public int TargetFps { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOrInit();
        ApplyAll();
    }

    public void SetMaster(float v) { Master = Mathf.Clamp01(v); ApplyVolume("MasterVolume", Master); SaveFloat(KeyMaster, Master); }
    public void SetMusic(float v) { Music = Mathf.Clamp01(v); ApplyVolume("MusicVolume", Music); SaveFloat(KeyMusic, Music); }
    public void SetSfx(float v) { Sfx = Mathf.Clamp01(v); ApplyVolume("SFXVolume", Sfx); SaveFloat(KeySfx, Sfx); }

    public void SetHaptics(bool on) { Haptics = on; PlayerPrefs.SetInt(KeyHap, on ? 1 : 0); }
    public void SetScreenShake(bool on) { ScreenShake = on; PlayerPrefs.SetInt(KeyShake, on ? 1 : 0); }
    public void SetHitStop(bool on) { HitStop = on; PlayerPrefs.SetInt(KeyHit, on ? 1 : 0); }
    public void SetTargetFps(int fps) { TargetFps = (fps >= 120) ? 120 : 60; PlayerPrefs.SetInt(KeyFPS, TargetFps); ApplyFps(); }

    public void ApplyAll()
    {
        ApplyVolume("MasterVolume", Master);
        ApplyVolume("MusicVolume", Music);
        ApplyVolume("SFXVolume", Sfx);
        ApplyFps();
    }

    private void ApplyFps()
    {
        Application.targetFrameRate = TargetFps;
        QualitySettings.vSyncCount = 0; // Force targetFrameRate to take effect
    }

    private void ApplyVolume(string exposedName, float linear01)
    {
        // Convert [0..1] to decibels. 0 -> -80dB (mute), 1 -> 0dB
        float dB = (linear01 <= 0.0001f) ? -80f : Mathf.Log10(linear01) * 20f;
        audioMixer.SetFloat(exposedName, dB);
    }

    private void LoadOrInit()
    {
        Master = PlayerPrefs.HasKey(KeyMaster) ? PlayerPrefs.GetFloat(KeyMaster) : defaults.master;
        Music = PlayerPrefs.HasKey(KeyMusic) ? PlayerPrefs.GetFloat(KeyMusic) : defaults.music;
        Sfx = PlayerPrefs.HasKey(KeySfx) ? PlayerPrefs.GetFloat(KeySfx) : defaults.sfx;

        Haptics = PlayerPrefs.GetInt(KeyHap, defaults.haptics ? 1 : 0) == 1;
        ScreenShake = PlayerPrefs.GetInt(KeyShake, defaults.screenShake ? 1 : 0) == 1;
        HitStop = PlayerPrefs.GetInt(KeyHit, defaults.hitStop ? 1 : 0) == 1;
        TargetFps = PlayerPrefs.GetInt(KeyFPS, defaults.targetFps);
    }

    private void SaveFloat(string key, float v) { PlayerPrefs.SetFloat(key, v); }
}
