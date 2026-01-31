using UnityEngine;

public enum AchievementConditionType
{
    Distance,
    ScoreItemCount,
    GoldItemCount
}

[System.Serializable]
public class AchievementDefinition
{
    public string id;
    public AchievementConditionType condition = AchievementConditionType.Distance;
    public float distanceThreshold = 100f;
    public int countThreshold = 1;

    [Header("Display")]
    public string displayName;
    public LocalizedString displayNameLocalized;

    [Header("Reward")]
    public Sprite rewardSprite;
    public int rewardGold;
    public int rewardHearts;
    public string rewardSkinId;
    public string rewardSkinUnlockPrefix = "Skin.Unlocked.";
    public string rewardMessage;
    public LocalizedString rewardMessageLocalized;
}

[CreateAssetMenu(menuName = "RollSnow/Achievement Catalog", fileName = "AchievementCatalog")]
public class AchievementCatalog : ScriptableObject
{
    public string prefsPrefix = "Achievement.Completed.";
    public string claimedPrefsPrefix = "Achievement.Claimed.";
    public AchievementDefinition[] achievements;

    public AchievementDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || achievements == null)
            return null;

        for (int i = 0; i < achievements.Length; i++)
        {
            var entry = achievements[i];
            if (entry != null && entry.id == id)
                return entry;
        }

        return null;
    }
}
