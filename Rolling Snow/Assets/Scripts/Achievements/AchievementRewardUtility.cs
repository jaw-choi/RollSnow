public static class AchievementRewardUtility
{
    public static void Grant(AchievementDefinition achievement)
    {
        if (achievement == null)
            return;

        if (achievement.rewardGold > 0)
        {
            var gold = GoldSystem.GetOrCreate();
            if (gold != null)
                gold.AddGold(achievement.rewardGold);
        }

        if (achievement.rewardHearts > 0)
        {
            var hearts = HeartSystem.GetOrCreate();
            if (hearts != null)
                hearts.GrantHearts(achievement.rewardHearts);
        }

        if (!string.IsNullOrEmpty(achievement.rewardSkinId))
        {
            string prefix = string.IsNullOrEmpty(achievement.rewardSkinUnlockPrefix)
                ? SkinStorage.DefaultUnlockPrefix
                : achievement.rewardSkinUnlockPrefix;
            SkinStorage.Unlock(achievement.rewardSkinId, prefix);
        }
    }
}
