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

    // Exposed parameter names (AudioMixer의 Exposed와 정확히 일치)
    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_MUSIC = "MusicVolume";
    private const string PARAM_SFX = "SFXVolume";

    [Header("#BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 1f; // pre-mix gain (선택)
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;

    [Header("#SFX")]
    public AudioClip[] sfxClips;
    [Range(0f, 1f)] public float sfxVolume = 1f; // pre-mix gain (선택)
    public int channels = 8;
    AudioSource[] sfxPlayers;
    int channelIndex;
    public enum Sfx { Dead, Hit, LevelUp = 3, Lose, Melee, Range = 7, Select, Win, GetItem}


    void Awake()
    {
        instance = this;
        Init();
        // 초기 Mixer 볼륨 적용(원하면 PlayerPrefs에서 불러와 적용)
        if (audioDefaults != null)
        {
            SetMasterVolume01(audioDefaults.master);
            SetMusicVolume01(audioDefaults.music);
            SetSfxVolume01(audioDefaults.sfx);
        }

        // 2) SettingsManager가 살아 있으면(부트스트랩 이후) 최종값으로 덮어쓰기
        var sm = SettingsManager.Instance;
        if (sm != null)
        {
            SetMasterVolume01(sm.Master);
            SetMusicVolume01(sm.Music);
            SetSfxVolume01(sm.Sfx);
        }
    }
    void Init()
    {
        // 배경음 플레이어 초기화
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = bgmClip;
        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();
        // Mixer 라우팅 
        if (musicGroup != null) bgmPlayer.outputAudioMixerGroup = musicGroup;

        // 카메라에 HighPassFilter가 없다면 null일 수 있음
        var cam = Camera.main;
        if (cam != null) bgmEffect = cam.GetComponent<AudioHighPassFilter>();

        // 효과음 플레이어 초기화
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
            src.bypassListenerEffects = true;  // 리스너의 이펙트 영향 배제 (선택)
            src.volume = sfxVolume;            // pre-mix
            if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup; // Mixer 라우팅 
            sfxPlayers[index] = src;

        }
    }
    public void PlayBGM(bool isPlay)
    {
        if (isPlay)
            bgmPlayer.Play();
        else
            bgmPlayer.Stop();
    }
    public void EffectBGM(bool isPlay)
    {
        bgmEffect.enabled = isPlay;
    }
    public void PlaySfx(Sfx sfx)
    {
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + channelIndex) % sfxPlayers.Length;

            var src = sfxPlayers[loopIndex];
            if (src.isPlaying) continue;

            int ranIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee) ranIndex = Random.Range(0, 2);

            channelIndex = loopIndex;
            src.clip = sfxClips[(int)sfx + ranIndex];
            src.Play();
            break;

        }
    }
    // === Volume via AudioMixer (슬라이더 0..1) ===
    public void SetMasterVolume01(float v01) => SetMixer01(PARAM_MASTER, v01);
    public void SetMusicVolume01(float v01) => SetMixer01(PARAM_MUSIC, v01);
    public void SetSfxVolume01(float v01) => SetMixer01(PARAM_SFX, v01);

    private void SetMixer01(string param, float v01)
    {
        if (audioMixer == null) return;
        float dB = Linear01ToDecibel(v01);
        audioMixer.SetFloat(param, dB);
    }

    // 0..1 -> dB(로그 스케일). 0이면 -80dB로 사실상 mute
    private float Linear01ToDecibel(float v)
    {
        if (v <= 0.0001f) return -80f;
        return Mathf.Log10(Mathf.Clamp01(v)) * 20f;
    }

    // (선택) pre-mix 볼륨도 슬라이더로 직접 만지고 싶다면:
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

}
