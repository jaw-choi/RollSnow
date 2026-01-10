using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Defaults")]
    [SerializeField] private float defaultBgm = 1f;
    [SerializeField] private float defaultSfx = 1f;

    const string PrefBgm = "BGM_VOLUME";
    const string PrefSfx = "SFX";

    void OnEnable()
    {
        ApplySavedValues();
        HookListeners(true);
    }

    void OnDisable()
    {
        HookListeners(false);
    }

    void ApplySavedValues()
    {
        float bgmValue = PlayerPrefs.HasKey(PrefBgm) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgm)) : Mathf.Clamp01(defaultBgm);
        float sfxValue = PlayerPrefs.HasKey(PrefSfx) ? Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx)) : Mathf.Clamp01(defaultSfx);

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.SetValueWithoutNotify(bgmValue);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(sfxValue);
        }

        ApplyVolumes(bgmValue, sfxValue);
        SaveVolumes(bgmValue, sfxValue);
    }

    void HookListeners(bool on)
    {
        if (!on)
        {
            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(HandleBgmChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(HandleBgmChanged);
            bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }
    }

    void HandleBgmChanged(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefBgm, value);
        ApplyBgm(value);
    }

    void HandleSfxChanged(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefSfx, value);
        ApplySfx(value);
    }

    void ApplyVolumes(float bgmValue, float sfxValue)
    {
        ApplyBgm(bgmValue);
        ApplySfx(sfxValue);
    }

    void ApplyBgm(float value)
    {
        var manager = AudioManager.instance;
        if (manager != null)
            manager.ApplyBgmVolume01(value);
    }

    void ApplySfx(float value)
    {
        var manager = AudioManager.instance;
        if (manager != null)
            manager.ApplySfxVolume01(value);
    }

    void SaveVolumes(float bgmValue, float sfxValue)
    {
        PlayerPrefs.SetFloat(PrefBgm, bgmValue);
        PlayerPrefs.SetFloat(PrefSfx, sfxValue);
    }
}
