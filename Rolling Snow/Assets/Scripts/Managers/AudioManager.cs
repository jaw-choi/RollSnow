using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSettingsSO audioDefaults;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    private const string PARAM_SFX = "SFXVolume";
    private const string PREF_BGM = "BGM_VOLUME";
    private const string PREF_SFX = "SFX";

    [Header("BGM Clips")]
    [SerializeField] private AudioClip menuBgmClip;
    [SerializeField] private AudioClip gameBgmClip;
    [Header("BGM Base Volume")]
    [SerializeField, Range(0f, 1f)] private float menuBaseVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float gameBaseVolume = 1f;
    [SerializeField] private float bgmFadeSeconds = 0.8f;
    [SerializeField, Range(0f, 1f)] private float userBgmVolume = 1f;
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;
    float bgmFade = 1f;
    float currentBaseVolume = 1f;
    Coroutine bgmFadeRoutine;

    [Header("SFX")]
    public AudioClip[] sfxClips;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public int channels = 8;
    AudioSource[] sfxPlayers;

    [Header("Scenes")]
    [SerializeField] private string logoSceneName = "00_Logo";
    [SerializeField] private string gameSceneName = "04_GameScene";
    [SerializeField] private string[] menuSceneNames = new string[]
    {
        "01_MainMenu",
        "02_Achievement",
        "03_Shop",
        "05_Inventory",
        "06_Settings"
    };

    int channelIndex;
    public enum Sfx
    {
        Dead = 0,
        Select,
        GetItem,
        Curve,
        Gacha,
        No,
        SpeedUp,
        ObstacleHit
    }

    enum BgmMode
    {
        None,
        Menu,
        Game
    }

    BgmMode currentBgmMode = BgmMode.None;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
        LoadSavedVolumes();

        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Init()
    {
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        ApplyBgmVolume();
        if (musicGroup != null)
            bgmPlayer.outputAudioMixerGroup = musicGroup;

        var cam = Camera.main;
        if (cam != null)
            bgmEffect = cam.GetComponent<AudioHighPassFilter>();

        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            var src = sfxObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.bypassListenerEffects = true;
            src.volume = sfxVolume;
            if (sfxGroup != null)
                src.outputAudioMixerGroup = sfxGroup;
            sfxPlayers[index] = src;
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BgmMode targetMode = ResolveBgmMode(scene.name);
        if (targetMode == BgmMode.None)
        {
            StartBgmFade(null, targetMode, bgmFadeSeconds);
            return;
        }

        AudioClip targetClip = targetMode == BgmMode.Menu ? menuBgmClip : gameBgmClip;
        if (targetClip == null)
        {
            StartBgmFade(null, BgmMode.None, bgmFadeSeconds);
            return;
        }

        bool sameClip = bgmPlayer != null && bgmPlayer.clip == targetClip;
        bool keepPlaying = sameClip && bgmPlayer.isPlaying;
        if (currentBgmMode == targetMode && keepPlaying)
            return;

        StartBgmFade(targetClip, targetMode, bgmFadeSeconds);
    }

    BgmMode ResolveBgmMode(string sceneName)
    {
        if (!string.IsNullOrEmpty(logoSceneName) && sceneName == logoSceneName)
            return BgmMode.None;

        if (!string.IsNullOrEmpty(gameSceneName) && sceneName == gameSceneName)
            return BgmMode.Game;

        if (menuSceneNames != null)
        {
            for (int i = 0; i < menuSceneNames.Length; i++)
            {
                string menuScene = menuSceneNames[i];
                if (!string.IsNullOrEmpty(menuScene) && sceneName == menuScene)
                    return BgmMode.Menu;
            }
        }

        return BgmMode.None;
    }

    public void PlayBGM(bool isPlay)
    {
        if (bgmPlayer == null)
            return;

        if (!isPlay)
            return;

        if (!bgmPlayer.isPlaying && bgmPlayer.clip != null)
            bgmPlayer.Play();
    }

    public void EffectBGM(bool isPlay)
    {
        if (bgmEffect != null)
            bgmEffect.enabled = isPlay;
    }

    public void PlaySfx(Sfx sfx)
    {
        if (sfxPlayers == null || sfxPlayers.Length == 0)
            return;
        if (sfxClips == null || sfxClips.Length == 0)
            return;

        int clipIndex = (int)sfx;
        if (clipIndex < 0 || clipIndex >= sfxClips.Length)
            return;

        var clip = sfxClips[clipIndex];
        if (clip == null)
            return;

        bool played = false;
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + channelIndex) % sfxPlayers.Length;

            var src = sfxPlayers[loopIndex];
            if (src.isPlaying)
                continue;

            channelIndex = loopIndex;
            src.clip = clip;
            src.Play();
            played = true;
            break;
        }

        if (!played)
        {
            channelIndex = (channelIndex + 1) % sfxPlayers.Length;
            var src = sfxPlayers[channelIndex];
            src.PlayOneShot(clip);
        }
    }

    public void ApplyBgmVolume01(float v01)
    {
        userBgmVolume = Mathf.Clamp01(v01);
        ApplyBgmVolume();
    }

    public void ApplySfxVolume01(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (audioMixer != null)
            SetMixer01(PARAM_SFX, v01);
        else
            SetPreMixSfx(v01);
    }

    public float GetSavedBgmVolume01()
    {
        if (PlayerPrefs.HasKey(PREF_BGM))
            return Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_BGM));

        return 1f;
    }

    void LoadSavedVolumes()
    {
        float bgmValue = PlayerPrefs.HasKey(PREF_BGM) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_BGM)) : GetDefaultBgm();
        float sfxValue = PlayerPrefs.HasKey(PREF_SFX) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX)) : GetDefaultSfx();

        ApplyBgmVolume01(bgmValue);
        ApplySfxVolume01(sfxValue);

        if (!PlayerPrefs.HasKey(PREF_BGM))
            PlayerPrefs.SetFloat(PREF_BGM, bgmValue);
        if (!PlayerPrefs.HasKey(PREF_SFX))
            PlayerPrefs.SetFloat(PREF_SFX, sfxValue);
    }

    float GetDefaultBgm()
    {
        if (audioDefaults != null)
            return Mathf.Clamp01(audioDefaults.music);
        return 1f;
    }

    float GetDefaultSfx()
    {
        if (audioDefaults != null)
            return Mathf.Clamp01(audioDefaults.sfx);
        return 1f;
    }

    void StartBgmFade(AudioClip targetClip, BgmMode targetMode, float duration)
    {
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        bgmFadeRoutine = StartCoroutine(FadeToClipRoutine(targetClip, targetMode, duration));
    }

    IEnumerator FadeToClipRoutine(AudioClip targetClip, BgmMode targetMode, float duration)
    {
        if (bgmPlayer == null)
            yield break;

        bool sameClip = bgmPlayer.clip == targetClip;

        if (targetClip == null)
        {
            yield return FadeBgm(0f, duration);
            bgmPlayer.Stop();
            bgmPlayer.clip = null;
            currentBgmMode = BgmMode.None;
            yield break;
        }

        if (!sameClip && bgmPlayer.isPlaying)
            yield return FadeBgm(0f, duration);

        if (!sameClip || currentBgmMode != targetMode)
        {
            bgmPlayer.Stop();
            bgmPlayer.clip = targetClip;
            currentBgmMode = targetMode;
            currentBaseVolume = GetBaseVolumeForMode(targetMode);
            bgmPlayer.Play();
            bgmFade = 0f;
            ApplyBgmVolume();
        }
        else if (!bgmPlayer.isPlaying)
        {
            bgmPlayer.Play();
        }

        yield return FadeBgm(1f, duration);
    }

    IEnumerator FadeBgm(float target, float duration)
    {
        float start = bgmFade;
        if (duration <= 0f)
        {
            bgmFade = target;
            ApplyBgmVolume();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgmFade = Mathf.Lerp(start, target, t);
            ApplyBgmVolume();
            yield return null;
        }

        bgmFade = target;
        ApplyBgmVolume();
    }

    void ApplyBgmVolume()
    {
        if (bgmPlayer != null)
            bgmPlayer.volume = Mathf.Clamp01(currentBaseVolume * userBgmVolume) * bgmFade;
    }

    void SetMixer01(string param, float v01)
    {
        if (audioMixer == null)
            return;

        float dB = Linear01ToDecibel(v01);
        audioMixer.SetFloat(param, dB);
    }

    float Linear01ToDecibel(float v)
    {
        if (v <= 0.0001f)
            return -80f;
        return Mathf.Log10(Mathf.Clamp01(v)) * 20f;
    }

    public void SetPreMixBgm(float v01)
    {
        userBgmVolume = Mathf.Clamp01(v01);
        ApplyBgmVolume();
    }

    public void SetPreMixSfx(float v01)
    {
        sfxVolume = Mathf.Clamp01(v01);
        if (sfxPlayers == null)
            return;

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            if (sfxPlayers[i] != null)
                sfxPlayers[i].volume = sfxVolume;
        }
    }

    public void BoostMusic(float extra01, float duration)
    {
        StartCoroutine(BoostMusicRoutine(extra01, duration));
    }

    IEnumerator BoostMusicRoutine(float extra01, float duration)
    {
        float baseValue = GetSavedBgmVolume01();
        float boostedValue = Mathf.Clamp(baseValue + extra01, 0f, 1.5f);

        ApplyBgmVolume01(boostedValue);
        yield return new WaitForSecondsRealtime(duration);
        ApplyBgmVolume01(baseValue);
    }

    float GetBaseVolumeForMode(BgmMode mode)
    {
        if (mode == BgmMode.Game)
            return Mathf.Clamp01(gameBaseVolume);
        if (mode == BgmMode.Menu)
            return Mathf.Clamp01(menuBaseVolume);
        return 0f;
    }
}
