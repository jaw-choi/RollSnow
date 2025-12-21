using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public enum SettingsMode { MainMenu, InGame }

    [Header("Mode")]
    [SerializeField] private SettingsMode mode;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle hapticsToggle;

    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button restartButton;

    void OnEnable()
    {
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(mode == SettingsMode.InGame);

        if (restartButton != null)
            restartButton.gameObject.SetActive(mode == SettingsMode.InGame);

        var sm = SettingsManager.Instance;
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(sm.Master);
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
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            }
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
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBack);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestart);
            }
        }
        else
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (hapticsToggle != null)
                hapticsToggle.onValueChanged.RemoveListener(OnHaptics);
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();
            if (restartButton != null)
                restartButton.onClick.RemoveAllListeners();
        }
    }

    void OnMasterChanged(float value)
    {
        SettingsManager.Instance.SetMaster(value);
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
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    void OnBack()
    {
        if (mode == SettingsMode.MainMenu)
        {
            gameObject.SetActive(false);
        }
        else if (mode == SettingsMode.InGame)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            gameObject.SetActive(false);
        }
    }

    public void SetMode(SettingsMode newMode)
    {
        mode = newMode;
    }
}
