using GoogleMobileAds.Api;
using UnityEngine;
using System;

public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance { get; private set; }

    [SerializeField] private string adUnitId = "ca-app-pub-3940256099942544/5224354917"; // test id
    //[SerializeField] private string adUnitId = "ca-app-pub-8502618733998421/2441408968"; // my id

    [SerializeField] private bool persistBetweenScenes = true;
    private RewardedAd rewardedAd;
    private float retryDelay = 2f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        MobileAds.Initialize(_ => { });
        LoadRewardedAd();
    }

    void LoadRewardedAd()
    {
        var request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.Log($"Rewarded ad load failed: {error}");
                Invoke(nameof(LoadRewardedAd), retryDelay);
                retryDelay = Mathf.Min(retryDelay * 2f, 60f);
                return;
            }

            retryDelay = 2f;
            rewardedAd = ad;

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded ad closed. Reloading.");
                LoadRewardedAd();
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError err) =>
            {
                Debug.Log($"Rewarded ad failed to show: {err}");
                LoadRewardedAd();
            };
        });
    }

    public bool IsReady() => rewardedAd != null && rewardedAd.CanShowAd();

    public void ShowRewardedAd(Action onReward)
    {
        if (!IsReady())
        {
            Debug.Log("Rewarded ad not ready.");
            LoadRewardedAd();
            return;
        }

        rewardedAd.Show(_ =>
        {
            Debug.Log("Reward granted.");
            onReward?.Invoke();
        });
    }
}
