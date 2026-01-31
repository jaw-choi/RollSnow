using UnityEngine;
using UnityEngine.UI;

public class AchievementIconTint : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private AchievementCatalog catalog;
    [SerializeField] private string achievementId = "achievement_1";
    [SerializeField] private string prefsPrefix = "Achievement.Completed.";
    [SerializeField] private bool readFromPlayerPrefs = true;

    [Header("Targets")]
    [SerializeField] private Image[] iconImages;

    [Header("Colors")]
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color completedColor = new Color(0f, 0f, 0f, 1f);

    public string AchievementId => achievementId;
    public AchievementCatalog Catalog => catalog;
    public string PrefsPrefix => prefsPrefix;
    public bool ReadFromPlayerPrefs => readFromPlayerPrefs;

    void Awake()
    {
        CacheIconImages();
    }

    void OnEnable()
    {
        Refresh();
    }

    void CacheIconImages()
    {
        if (iconImages != null && iconImages.Length > 0)
            return;

        var image = GetComponentInChildren<Image>(true);
        if (image != null)
            iconImages = new[] { image };
    }

    public void Refresh()
    {
        bool completed = readFromPlayerPrefs ? IsCompletedFromPrefs() : false;
        Apply(completed);
    }

    public void SetCompleted(bool completed, bool saveToPrefs = true)
    {
        Apply(completed);

        if (!saveToPrefs || !readFromPlayerPrefs)
            return;

        PlayerPrefs.SetInt(GetPrefsKey(), completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    bool IsCompletedFromPrefs()
    {
        if (string.IsNullOrEmpty(achievementId))
            return false;

        return PlayerPrefs.GetInt(GetPrefsKey(), 0) > 0;
    }

    string GetPrefsKey()
    {
        string prefix = catalog != null ? catalog.prefsPrefix : prefsPrefix;
        if (string.IsNullOrEmpty(prefix))
            return achievementId;

        return $"{prefix}{achievementId}";
    }

    void Apply(bool completed)
    {
        if (iconImages == null || iconImages.Length == 0)
            return;

        Color color = completed ? completedColor : lockedColor;
        for (int i = 0; i < iconImages.Length; i++)
        {
            if (iconImages[i] != null)
                iconImages[i].color = color;
        }
    }
}
