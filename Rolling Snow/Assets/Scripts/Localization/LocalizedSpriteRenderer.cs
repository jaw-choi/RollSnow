using UnityEngine;

[DisallowMultipleComponent]
public class LocalizedSpriteRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite koreanSprite;
    [SerializeField] private Sprite englishSprite;

    void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();
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
        if (target == null)
            return;

        Sprite sprite = language == GameLanguage.English ? englishSprite : koreanSprite;
        if (sprite == null)
            sprite = language == GameLanguage.English ? koreanSprite : englishSprite;

        if (sprite != null)
            target.sprite = sprite;
    }
}
