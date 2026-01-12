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
}

[CreateAssetMenu(menuName = "RollSnow/Achievement Catalog", fileName = "AchievementCatalog")]
public class AchievementCatalog : ScriptableObject
{
    public string prefsPrefix = "Achievement.Completed.";
    public AchievementDefinition[] achievements;
}
