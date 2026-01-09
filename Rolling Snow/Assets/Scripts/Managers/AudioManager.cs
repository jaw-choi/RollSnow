using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    [SerializeField] private AudioSettingsSO audioDefaults;

    [Header("#Mixer (assign in Inspector)")] //  NEW
    [SerializeField] private AudioMixer audioMixer;             // GameMixer
    [SerializeField] private AudioMixerGroup musicGroup;        // GameMixer/Music
    [SerializeField] private AudioMixerGroup sfxGroup;          // GameMixer/SFX

    // Exposed parameter names (AudioMixer�� Exposed�� ��Ȯ�� ��ġ)
    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_MUSIC = "MusicVolume";
    private const string PARAM_SFX = "SFXVolume";

    [Header("#BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 1f; // pre-mix gain (����)
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;

    [Header("#SFX")]
    public AudioClip[] sfxClips;
    [Range(0f, 1f)] public float sfxVolume = 1f; // pre-mix gain (����)
    public int channels = 8;
    AudioSource[] sfxPlayers;

    [Header("#Debug")]
    [SerializeField] private bool logBgmVolume = false;
    [SerializeField] private float bgmLogInterval = 1f;
    float bgmLogTimer = 0f;

    int channelIndex;
    public enum Sfx
    {
        Dead = 0,
        Select,
        GetItem,
        Curve,
        Gacha,
        No
    }


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
        // �ʱ� Mixer ���� ����(���ϸ� PlayerPrefs���� �ҷ��� ����)
        if (audioDefaults != null)
        {
            SetMasterVolume01(audioDefaults.master);
            SetMusicVolume01(audioDefaults.music);
            SetSfxVolume01(audioDefaults.sfx);
        }

        // 2) SettingsManager�� ��� ������(��Ʈ��Ʈ�� ����) ���������� �����
        var sm = SettingsManager.Instance;
        if (sm != null)
        {
            SetMasterVolume01(sm.Master);
            SetMusicVolume01(sm.Music);
            SetSfxVolume01(sm.Sfx);
        }
    }
    void Update()
    {
        if (!logBgmVolume || bgmPlayer == null || bgmLogInterval <= 0f)
            return;

        bgmLogTimer -= Time.deltaTime;
        if (bgmLogTimer <= 0f)
        {
            bgmLogTimer = bgmLogInterval;
            float sourceVol = bgmPlayer.volume;
            string mixerInfo = "N/A";
            if (audioMixer != null && audioMixer.GetFloat(PARAM_MUSIC, out float dbValue))
                mixerInfo = $"{dbValue:F1} dB";
            //Debug.Log($"[AudioManager] BGM volume src={sourceVol:F2}, mixer={mixerInfo}");
        }
    }

    void Init()
    {
        // ����� �÷��̾� �ʱ�ȭ
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = bgmClip;
        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();
        // Mixer ����� 
        if (musicGroup != null) bgmPlayer.outputAudioMixerGroup = musicGroup;

        // ī�޶� HighPassFilter�� ���ٸ� null�� �� ����
        var cam = Camera.main;
        if (cam != null) bgmEffect = cam.GetComponent<AudioHighPassFilter>();

        // ȿ���� �÷��̾� �ʱ�ȭ
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            //sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            //sfxPlayers[index].playOnAwake = false;
            //sfxPlayers[index].bypassListenerEffects = true;
            //sfxPlayers[index].volume = sfxVolume;
            var src = sfxObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.bypassListenerEffects = true;  // �������� ����Ʈ ���� ���� (����)
            src.volume = sfxVolume;            // pre-mix
            if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup; // Mixer ����� 
            sfxPlayers[index] = src;

        }
    }
    public void PlayBGM(bool isPlay)
    {
        if (bgmPlayer == null) return;

        if (isPlay)
        {
            if (!bgmPlayer.isPlaying)
                bgmPlayer.Play();

            if (bgmPlayer.isPlaying)
                Debug.Log($"[AudioManager] BGM Playing: {bgmPlayer.clip?.name ?? "Unknown"}");
        }
        else
        {
            if (bgmPlayer.isPlaying)
                Debug.Log("[AudioManager] BGM Stopped");
            bgmPlayer.Stop();
        }
    }
    public void EffectBGM(bool isPlay)
    {
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
            if (src.isPlaying) continue;

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
    // === Volume via AudioMixer (�����̴� 0..1) ===
    public void SetMasterVolume01(float v01) => SetMixer01(PARAM_MASTER, v01);
    public void SetMusicVolume01(float v01) => SetMixer01(PARAM_MUSIC, v01);
    public void SetSfxVolume01(float v01) => SetMixer01(PARAM_SFX, v01);

    private void SetMixer01(string param, float v01)
    {
        if (audioMixer == null) return;
        float dB = Linear01ToDecibel(v01);
        audioMixer.SetFloat(param, dB);
    }

    // 0..1 -> dB(�α� ������). 0�̸� -80dB�� ��ǻ� mute
    private float Linear01ToDecibel(float v)
    {
        if (v <= 0.0001f) return -80f;
        return Mathf.Log10(Mathf.Clamp01(v)) * 20f;
    }

    // (����) pre-mix ������ �����̴��� ���� ������ �ʹٸ�:
    public void SetPreMixBgm(float v01)
    {
        bgmVolume = Mathf.Clamp01(v01);
        if (bgmPlayer != null) bgmPlayer.volume = bgmVolume;
    }
    public void SetPreMixSfx(float v01)
    {
        sfxVolume = Mathf.Clamp01(v01);
        if (sfxPlayers != null)
            for (int i = 0; i < sfxPlayers.Length; i++)
                if (sfxPlayers[i] != null) sfxPlayers[i].volume = sfxVolume;
    }

    public void BoostMusic(float extra01, float duration)
    {
        StartCoroutine(BoostMusicRoutine(extra01, duration));
    }

    IEnumerator BoostMusicRoutine(float extra01, float duration)
    {
        // SettingsManager 등에서 현재 0~1 값을 가져오거나 캐시해 둡니다.
        float baseValue = SettingsManager.Instance?.Music ?? 1f;
        float boostedValue = Mathf.Clamp(baseValue + extra01, 0f, 1.5f);

        SetMixer01(PARAM_MUSIC, boostedValue);
        yield return new WaitForSeconds(duration);
        SetMixer01(PARAM_MUSIC, baseValue);
    }

}
