using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public enum SettingsMode { MainMenu, InGame }

    [Header("Mode")]
    [SerializeField] private SettingsMode mode;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle hapticsToggle;

    void OnEnable()
    {
        var sm = SettingsManager.Instance;
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(sm.Music);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sm.Sfx);

        if (hapticsToggle != null)
            hapticsToggle.SetIsOnWithoutNotify(sm.Haptics);

        HookEvents(true);
    }

    void OnDisable()
    {
        HookEvents(false);
    }

    void HookEvents(bool enable)
    {
        if (enable)
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }
            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }
            if (hapticsToggle != null)
            {
                hapticsToggle.onValueChanged.RemoveListener(OnHaptics);
                hapticsToggle.onValueChanged.AddListener(OnHaptics);
            }
        }
        else
        {
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (hapticsToggle != null)
                hapticsToggle.onValueChanged.RemoveListener(OnHaptics);
        }
    }

    void OnMusicChanged(float value)
    {
        SettingsManager.Instance.SetMusic(value);
    }

    void OnSfxChanged(float value)
    {
        SettingsManager.Instance.SetSfx(value);
    }

    void OnHaptics(bool value)
    {
        SettingsManager.Instance.SetHaptics(value);
        if (value)
            Haptics.Tap();
    }

    public void SetMode(SettingsMode newMode)
    {
        mode = newMode;
    }
}
