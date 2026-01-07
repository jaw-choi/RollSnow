using UnityEngine;

public class HeartRewardSample : MonoBehaviour
{
    [Header("Ad Reward")]
    [SerializeField] private int adRewardHearts = 1;
    [SerializeField] private RewardedAdManager rewardedAdManager;

    [Header("Gem Reward (Sample Storage)")]
    [SerializeField] private int gemCost = 10;
    [SerializeField] private string gemKey = "Sample.Gems";

    public void OnAdRewardButtonClicked()
    {
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
}
