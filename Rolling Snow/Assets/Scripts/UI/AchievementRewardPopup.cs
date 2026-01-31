using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementRewardPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI rewardLabel;
    [SerializeField] private Image rewardImage;

    [Header("Text")]
    [SerializeField] private string titleText = "Achievement Complete!";
    [SerializeField] private LocalizedString titleTextLocalized;
    [SerializeField] private string rewardFormat = "Reward: {0}";
    [SerializeField] private LocalizedString rewardFormatLocalized;
    [SerializeField] private string goldFormat = "Gold +{0}";
    [SerializeField] private string heartsFormat = "Hearts +{0}";
    [SerializeField] private string skinFormat = "Skin: {0}";
    [SerializeField] private string separator = " / ";

    [Header("Behavior")]
    [SerializeField] private float visibleSeconds = 2.5f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool queuePopups = true;

    readonly Queue<AchievementDefinition> pending = new Queue<AchievementDefinition>();
    Coroutine displayRoutine;

    void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        HideImmediate();
    }

    void OnEnable()
    {
        Subscribe(true);
    }

    void OnDisable()
    {
        Subscribe(false);
    }

    void OnDestroy()
    {
        Subscribe(false);
    }

    void Subscribe(bool on)
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        if (on)
            gm.AchievementCompleted += HandleAchievementCompleted;
        else
            gm.AchievementCompleted -= HandleAchievementCompleted;
    }

    void HandleAchievementCompleted(AchievementDefinition achievement)
    {
        if (achievement == null)
            return;

        if (displayRoutine != null)
        {
            if (queuePopups)
                pending.Enqueue(achievement);
            else
                RestartPopup(achievement);
            return;
        }

        ShowPopup(achievement);
    }

    void RestartPopup(AchievementDefinition achievement)
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        ShowPopup(achievement);
    }

    void ShowPopup(AchievementDefinition achievement)
    {
        ApplyAchievement(achievement);
        if (popupRoot != null)
            popupRoot.SetActive(true);

        float duration = Mathf.Max(0.1f, visibleSeconds);
        displayRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    IEnumerator HideAfterDelay(float duration)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return new WaitForSeconds(duration);

        HideImmediate();

        if (pending.Count > 0)
        {
            var next = pending.Dequeue();
            ShowPopup(next);
            yield break;
        }

        displayRoutine = null;
    }

    void HideImmediate()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    void ApplyAchievement(AchievementDefinition achievement)
    {
        if (titleLabel != null)
            titleLabel.text = LocalizationUtility.Resolve(titleTextLocalized, titleText);

        if (nameLabel != null)
        {
            string name = ResolveDisplayName(achievement);
            nameLabel.text = name;
            nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(name));
        }

        if (rewardLabel != null)
        {
            string rewardText = ResolveRewardText(achievement);
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
            Sprite sprite = achievement != null ? achievement.rewardSprite : null;
            rewardImage.sprite = sprite;
            rewardImage.gameObject.SetActive(sprite != null);
        }
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
