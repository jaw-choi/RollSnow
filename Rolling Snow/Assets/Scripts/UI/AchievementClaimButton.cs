using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AchievementClaimButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Source")]
    [SerializeField] private AchievementCatalog catalog;
    [SerializeField] private string achievementId;
    [SerializeField] private bool useIconTintSource = true;

    [Header("Behavior")]
    [SerializeField] private bool requireCompleted = true;
    [SerializeField] private bool ignoreIfClaimed = true;
    [SerializeField] private string completedPrefsPrefix = AchievementStorage.DefaultCompletedPrefix;
    [SerializeField] private string claimedPrefsPrefix = AchievementStorage.DefaultClaimedPrefix;

    [Header("Popup")]
    [SerializeField] private AchievementClaimPopupUI popup;

    [Header("Button Hook")]
    [SerializeField] private Button targetButton;
    [SerializeField] private bool autoHookButton = true;
    [SerializeField] private bool autoRefreshInteractable = true;

    AchievementIconTint iconTint;

    void Awake()
    {
        if (useIconTintSource)
            iconTint = GetComponent<AchievementIconTint>();

        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (popup == null)
            popup = FindObjectOfType<AchievementClaimPopupUI>(true);
    }

    void OnEnable()
    {
        if (autoHookButton)
            BindButton(true);

        if (autoRefreshInteractable)
            RefreshInteractable();
    }

    void OnDisable()
    {
        if (autoHookButton)
            BindButton(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    public void OnClick()
    {
        HandleClick();
    }

    public void RefreshInteractable()
    {
        if (targetButton == null)
            return;

        ResolveSources();
        if (string.IsNullOrEmpty(achievementId))
        {
            targetButton.interactable = false;
            return;
        }

        if (requireCompleted && !AchievementStorage.IsCompleted(achievementId, catalog, completedPrefsPrefix))
        {
            targetButton.interactable = false;
            return;
        }

        if (ignoreIfClaimed && AchievementStorage.IsClaimed(achievementId, catalog, claimedPrefsPrefix))
        {
            targetButton.interactable = false;
            return;
        }

        targetButton.interactable = true;
    }

    void ResolveSources()
    {
        if (!useIconTintSource || iconTint == null)
            return;

        if (catalog == null)
            catalog = iconTint.Catalog;

        if (string.IsNullOrEmpty(achievementId))
            achievementId = iconTint.AchievementId;
    }

    void HandleClick()
    {
        ResolveSources();
        if (popup == null || string.IsNullOrEmpty(achievementId))
            return;

        if (requireCompleted && !AchievementStorage.IsCompleted(achievementId, catalog, completedPrefsPrefix))
            return;

        if (ignoreIfClaimed && AchievementStorage.IsClaimed(achievementId, catalog, claimedPrefsPrefix))
            return;

        popup.Show(achievementId, catalog);
    }

    void BindButton(bool bind)
    {
        if (targetButton == null)
            return;

        if (!bind)
        {
            targetButton.onClick.RemoveListener(OnClick);
            return;
        }

        targetButton.onClick.RemoveListener(OnClick);
        targetButton.onClick.AddListener(OnClick);
    }
}
