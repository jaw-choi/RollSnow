using GoogleMobileAds.Api;
using UnityEngine;
using System;
using UnityEngine.Serialization;

public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance { get; private set; }

    //[SerializeField] private string adUnitId = "ca-app-pub-3940256099942544/5224354917"; // test id
    [FormerlySerializedAs("adUnitId")]
    [SerializeField] private string androidAdUnitId = "ca-app-pub-8502618733998421/2441408968";
    [SerializeField] private string iosAdUnitId = "ca-app-pub-8502618733998421/2766319319";

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
        string adUnitId = GetRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogError("Rewarded ad unit id is empty.");
            return;
        }

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

    string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return !string.IsNullOrEmpty(iosAdUnitId) ? iosAdUnitId : androidAdUnitId;
#else
        return androidAdUnitId;
#endif
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
