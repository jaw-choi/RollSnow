using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementClaimPopupUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AchievementCatalog fallbackCatalog;
    [SerializeField] private string completedPrefsPrefix = AchievementStorage.DefaultCompletedPrefix;
    [SerializeField] private string claimedPrefsPrefix = AchievementStorage.DefaultClaimedPrefix;

    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI rewardLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Image rewardImage;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button closeButton;

    [Header("Text")]
    [SerializeField] private string titleText = "Achievement Reward";
    [SerializeField] private LocalizedString titleTextLocalized;
    [SerializeField] private string rewardFormat = "Reward: {0}";
    [SerializeField] private LocalizedString rewardFormatLocalized;
    [SerializeField] private string claimedText = "Already claimed";
    [SerializeField] private LocalizedString claimedTextLocalized;
    [SerializeField] private string lockedText = "Not completed yet";
    [SerializeField] private LocalizedString lockedTextLocalized;
    [SerializeField] private string goldFormat = "Gold +{0}";
    [SerializeField] private string heartsFormat = "Hearts +{0}";
    [SerializeField] private string skinFormat = "Skin: {0}";
    [SerializeField] private string separator = " / ";

    [Header("Behavior")]
    [SerializeField] private bool closeOnClaim = true;
    [SerializeField] private bool autoClaimIfNoButton = true;

    AchievementDefinition currentAchievement;
    AchievementCatalog currentCatalog;

    void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        HookButtons(true);
        HideImmediate();
    }

    void OnDestroy()
    {
        HookButtons(false);
    }

    void HookButtons(bool on)
    {
        if (!on)
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(OnClaimClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(HideImmediate);
            return;
        }

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideImmediate);
        }
    }

    public void Show(string achievementId, AchievementCatalog catalog = null)
    {
        var targetCatalog = catalog != null ? catalog : fallbackCatalog;
        if (targetCatalog == null || string.IsNullOrEmpty(achievementId))
            return;

        var achievement = targetCatalog.GetById(achievementId);
        if (achievement == null)
            return;

        currentAchievement = achievement;
        currentCatalog = targetCatalog;

        ApplyContent();
        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (claimButton == null && autoClaimIfNoButton)
            TryClaimReward();
    }

    public void HideImmediate()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    void OnClaimClicked()
    {
        TryClaimReward();
    }

    void TryClaimReward()
    {
        if (currentAchievement == null)
            return;

        if (!AchievementStorage.IsCompleted(currentAchievement.id, currentCatalog, completedPrefsPrefix))
        {
            ApplyStatus(false, false);
            return;
        }

        if (AchievementStorage.IsClaimed(currentAchievement.id, currentCatalog, claimedPrefsPrefix))
        {
            ApplyStatus(true, true);
            return;
        }

        AchievementRewardUtility.Grant(currentAchievement);
        AchievementStorage.SetClaimed(currentAchievement.id, currentCatalog, claimedPrefsPrefix, true);

        ApplyStatus(true, true);
        if (closeOnClaim)
            HideImmediate();
    }

    void ApplyContent()
    {
        if (titleLabel != null)
            titleLabel.text = LocalizationUtility.Resolve(titleTextLocalized, titleText);

        string displayName = ResolveDisplayName(currentAchievement);
        if (nameLabel != null)
        {
            nameLabel.text = displayName;
            nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(displayName));
        }

        if (rewardLabel != null)
        {
            string rewardText = ResolveRewardText(currentAchievement);
            if (!string.IsNullOrEmpty(rewardText))
            {
                string format = LocalizationUtility.Resolve(rewardFormatLocalized, rewardFormat);
                rewardLabel.text = string.Format(format, rewardText);
                rewardLabel.gameObject.SetActive(true);
            }
            else
            {
                rewardLabel.gameObject.SetActive(false);
            }
        }

        if (rewardImage != null)
        {
            Sprite sprite = currentAchievement != null ? currentAchievement.rewardSprite : null;
            rewardImage.sprite = sprite;
            rewardImage.gameObject.SetActive(sprite != null);
        }

        bool completed = AchievementStorage.IsCompleted(currentAchievement.id, currentCatalog, completedPrefsPrefix);
        bool claimed = AchievementStorage.IsClaimed(currentAchievement.id, currentCatalog, claimedPrefsPrefix);
        ApplyStatus(completed, claimed);
    }

    void ApplyStatus(bool completed, bool claimed)
    {
        if (claimButton != null)
            claimButton.interactable = completed && !claimed;

        if (statusLabel == null)
            return;

        if (!completed)
        {
            statusLabel.text = LocalizationUtility.Resolve(lockedTextLocalized, lockedText);
            statusLabel.gameObject.SetActive(true);
            return;
        }

        if (claimed)
        {
            statusLabel.text = LocalizationUtility.Resolve(claimedTextLocalized, claimedText);
            statusLabel.gameObject.SetActive(true);
            return;
        }

        statusLabel.gameObject.SetActive(false);
    }

    string ResolveDisplayName(AchievementDefinition achievement)
    {
        if (achievement == null)
            return string.Empty;

        if (achievement.displayNameLocalized.HasAny)
            return achievement.displayNameLocalized.Get(LocalizationUtility.GetCurrentLanguage());
        if (!string.IsNullOrEmpty(achievement.displayName))
            return achievement.displayName;
        return achievement.id;
    }

    string ResolveRewardText(AchievementDefinition achievement)
    {
        if (achievement == null)
            return string.Empty;

        if (achievement.rewardMessageLocalized.HasAny)
            return achievement.rewardMessageLocalized.Get(LocalizationUtility.GetCurrentLanguage());
        if (!string.IsNullOrEmpty(achievement.rewardMessage))
            return achievement.rewardMessage;

        var parts = new List<string>(3);
        if (achievement.rewardGold > 0)
            parts.Add(string.Format(goldFormat, achievement.rewardGold));
        if (achievement.rewardHearts > 0)
            parts.Add(string.Format(heartsFormat, achievement.rewardHearts));
        if (!string.IsNullOrEmpty(achievement.rewardSkinId))
            parts.Add(string.Format(skinFormat, achievement.rewardSkinId));

        return parts.Count > 0 ? string.Join(separator, parts) : string.Empty;
    }
}
