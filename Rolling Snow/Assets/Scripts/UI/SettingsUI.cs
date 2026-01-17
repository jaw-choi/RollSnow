using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public enum SettingsMode { MainMenu, InGame }

    [System.Serializable]
    struct LanguageIconSwap
    {
        public Image target;
        public Sprite koreanSprite;
        public Sprite englishSprite;

        public void Apply(GameLanguage language)
        {
            if (target == null)
                return;

            Sprite sprite = language == GameLanguage.English ? englishSprite : koreanSprite;
            if (sprite == null)
                sprite = language == GameLanguage.English ? koreanSprite : englishSprite;

            if (sprite != null)
                target.sprite = sprite;
        }
    }

    [Header("Mode")]
    [SerializeField] private SettingsMode mode;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle hapticsToggle;

    [Header("Language")]
    [SerializeField] private Button languageButton;
    [SerializeField] private Button languageLeftButton;
    [SerializeField] private Button languageRightButton;
    [SerializeField] private LanguageIconSwap[] languageIcons;

    void OnEnable()
    {
        var sm = SettingsManager.Instance;
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(sm.Music);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sm.Sfx);

        if (hapticsToggle != null)
            hapticsToggle.SetIsOnWithoutNotify(sm.Haptics);

        if (sm != null)
            sm.LanguageChanged += HandleLanguageChanged;
        UpdateLanguageIcons(sm != null ? sm.Language : GameLanguage.Korean);

        HookEvents(true);
    }

    void OnDisable()
    {
        var sm = SettingsManager.Instance;
        if (sm != null)
            sm.LanguageChanged -= HandleLanguageChanged;

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
            if (languageButton != null && !IsLanguageButtonAlias(languageButton))
            {
                languageButton.onClick.RemoveListener(OnLanguageClicked);
                languageButton.onClick.AddListener(OnLanguageClicked);
            }
            if (languageLeftButton != null)
            {
                languageLeftButton.onClick.RemoveListener(OnLanguageLeftClicked);
                languageLeftButton.onClick.AddListener(OnLanguageLeftClicked);
            }
            if (languageRightButton != null)
            {
                languageRightButton.onClick.RemoveListener(OnLanguageRightClicked);
                languageRightButton.onClick.AddListener(OnLanguageRightClicked);
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
            if (languageButton != null && !IsLanguageButtonAlias(languageButton))
                languageButton.onClick.RemoveListener(OnLanguageClicked);
            if (languageLeftButton != null)
                languageLeftButton.onClick.RemoveListener(OnLanguageLeftClicked);
            if (languageRightButton != null)
                languageRightButton.onClick.RemoveListener(OnLanguageRightClicked);
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

    void OnLanguageClicked()
    {
        StepLanguage(1);
    }

    void OnLanguageLeftClicked()
    {
        StepLanguage(-1);
    }

    void OnLanguageRightClicked()
    {
        StepLanguage(1);
    }

    void StepLanguage(int delta)
    {
        var settings = SettingsManager.Instance;
        if (settings == null)
            return;

        settings.SetLanguage(GetSteppedLanguage(settings.Language, delta));
        UpdateLanguageIcons(settings.Language);
    }

    GameLanguage GetSteppedLanguage(GameLanguage current, int delta)
    {
        if (delta == 0)
            return current;

        var values = (GameLanguage[])Enum.GetValues(typeof(GameLanguage));
        if (values == null || values.Length == 0)
            return current;

        int index = Array.IndexOf(values, current);
        if (index < 0)
            index = 0;

        int next = (index + delta) % values.Length;
        if (next < 0)
            next += values.Length;

        return values[next];
    }

    bool IsLanguageButtonAlias(Button button)
    {
        return button != null && (button == languageLeftButton || button == languageRightButton);
    }

    void HandleLanguageChanged(GameLanguage language)
    {
        UpdateLanguageIcons(language);
    }

    void UpdateLanguageIcons(GameLanguage language)
    {
        if (languageIcons == null || languageIcons.Length == 0)
            return;

        for (int i = 0; i < languageIcons.Length; i++)
            languageIcons[i].Apply(language);
    }

    public void SetMode(SettingsMode newMode)
    {
        mode = newMode;
    }
}
