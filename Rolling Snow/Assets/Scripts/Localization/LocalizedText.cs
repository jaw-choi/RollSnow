using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private LocalizedString text;

    void Awake()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Apply();
        var settings = SettingsManager.Instance;
        if (settings != null)
            settings.LanguageChanged += HandleLanguageChanged;
    }

    void OnDisable()
    {
        var settings = SettingsManager.Instance;
        if (settings != null)
            settings.LanguageChanged -= HandleLanguageChanged;
    }

    void HandleLanguageChanged(GameLanguage language)
    {
        Apply(language);
    }

    public void Apply()
    {
        Apply(LocalizationUtility.GetCurrentLanguage());
    }

    void Apply(GameLanguage language)
    {
        if (target != null)
            target.text = text.Get(language);
    }
}
