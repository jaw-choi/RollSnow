using UnityEngine;

public class HeartRewardSample : MonoBehaviour
{
    [Header("Ad Reward")]
    [SerializeField] private int adRewardHearts = 1;
    [SerializeField] private RewardedAdManager rewardedAdManager;

    [Header("Gold Reward")]
    [SerializeField] private int adRewardGold = 20;

    [Header("Gem Reward (Sample Storage)")]
    [SerializeField] private int gemCost = 10;
    [SerializeField] private string gemKey = "Sample.Gems";

    [Header("Gold Spend Reward")]
    [SerializeField] private int goldCost = 50;
    [SerializeField] private int goldRewardHearts = 1;

    public void OnAdRewardButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var manager = rewardedAdManager;
        if (manager == null)
            manager = RewardedAdManager.Instance ?? FindObjectOfType<RewardedAdManager>();

        if (manager == null)
        {
            Debug.LogWarning("RewardedAdManager not found.");
            return;
        }

        manager.ShowRewardedAd(() =>
        {
            var system = HeartSystem.GetOrCreate();
            if (system == null)
                return;

            system.GrantHearts(Mathf.Max(1, adRewardHearts));
        });
    }

    public void OnGemUseButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return;

        int currentGems = PlayerPrefs.GetInt(gemKey, 0);
        if (currentGems < gemCost)
            return;

        PlayerPrefs.SetInt(gemKey, currentGems - gemCost);
        PlayerPrefs.Save();

        system.GrantHearts(1);
    }

    public void OnAdRewardGoldButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var manager = rewardedAdManager;
        if (manager == null)
            manager = RewardedAdManager.Instance ?? FindObjectOfType<RewardedAdManager>();

        if (manager == null)
        {
            Debug.LogWarning("RewardedAdManager not found.");
            return;
        }

        manager.ShowRewardedAd(() =>
        {
            var gold = GoldSystem.GetOrCreate();
            if (gold == null)
                return;

            gold.AddGold(Mathf.Max(1, adRewardGold));
        });
    }

    public void OnSpendGoldForHeartClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var heartSystem = HeartSystem.GetOrCreate();
        var goldSystem = GoldSystem.GetOrCreate();
        if (heartSystem == null || goldSystem == null)
            return;

        int cost = Mathf.Max(1, goldCost);
        if (!goldSystem.TrySpendGold(cost))
            return;

        heartSystem.GrantHearts(Mathf.Max(1, goldRewardHearts));
    }
}
