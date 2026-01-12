using System;
using UnityEngine;

public class HeartSystem : MonoBehaviour
{
    public struct HeartStatus
    {
        public int Current;
        public int Max;
        public int SecondsToNext;
        public bool IsFull;
        public int Extra;
    }

    private static HeartSystem instance;
    public static HeartSystem Instance => instance;

    public event Action<HeartStatus> HeartsChanged;

    [Header("Config")]
    [SerializeField] private int maxHearts = 5;
    [SerializeField] private int recoveryMinutes = 10;

    [Header("Storage Keys")]
    [SerializeField] private string heartsKey = "HeartSystem.Hearts";
    [SerializeField] private string timestampKey = "HeartSystem.LastTimestamp";

    private int currentHearts;
    private long lastTimestampSeconds;
    private bool isInitialized;

    public static HeartSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindObjectOfType<HeartSystem>();
        if (instance == null)
        {
            var go = new GameObject("HeartSystem");
            instance = go.AddComponent<HeartSystem>();
        }

        instance.Initialize();
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
        Refresh(true);
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SaveState();
            instance = null;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Refresh(true);
        }
        else
        {
            SaveState();
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveState();
        }
        else
        {
            Refresh(true);
        }
    }

    void OnValidate()
    {
        if (maxHearts < 1) maxHearts = 1;
        if (recoveryMinutes < 1) recoveryMinutes = 1;
    }

    public HeartStatus GetStatus()
    {
        Refresh(false);
        return BuildStatus();
    }

    public bool CanPlay
    {
        get
        {
            Refresh(false);
            return currentHearts > 0;
        }
    }

    public int GetSecondsToNextHeart()
    {
        Refresh(false);
        return BuildStatus().SecondsToNext;
    }

    public bool TryConsumeHeart()
    {
        Initialize();
        Refresh(false);

        if (currentHearts <= 0)
            return false;

        currentHearts = Mathf.Max(0, currentHearts - 1);
        lastTimestampSeconds = GetUtcNowSeconds();
        SaveState();
        NotifyStatus();
        return true;
    }

    public void GrantHearts(int amount)
    {
        if (amount <= 0)
            return;

        Initialize();
        Refresh(false);

        int before = currentHearts;
        currentHearts = Mathf.Max(0, currentHearts + amount);
        if (currentHearts >= maxHearts)
            lastTimestampSeconds = GetUtcNowSeconds();

        if (before != currentHearts)
        {
            SaveState();
            NotifyStatus();
        }
    }

    public void ApplyAuthoritativeState(int hearts, long unixTimestampSeconds)
    {
        Initialize();

        currentHearts = Mathf.Max(0, hearts);
        lastTimestampSeconds = unixTimestampSeconds > 0 ? unixTimestampSeconds : GetUtcNowSeconds();
        SaveState();
        NotifyStatus();
    }

    void Initialize()
    {
        if (isInitialized)
            return;

        if (maxHearts < 1) maxHearts = 1;
        if (recoveryMinutes < 1) recoveryMinutes = 1;

        LoadState();
        isInitialized = true;
    }

    void Refresh(bool forceNotify)
    {
        Initialize();

        int beforeHearts = currentHearts;
        bool beforeFull = currentHearts >= maxHearts;
        long beforeTimestamp = lastTimestampSeconds;

        ApplyRecovery();

        bool afterFull = currentHearts >= maxHearts;
        bool changed = beforeHearts != currentHearts
            || beforeFull != afterFull
            || beforeTimestamp != lastTimestampSeconds;

        if (changed)
            SaveState();

        if (changed || forceNotify)
            NotifyStatus();
    }

    void ApplyRecovery()
    {
        currentHearts = Mathf.Max(0, currentHearts);
        if (currentHearts >= maxHearts)
            return;

        long now = GetUtcNowSeconds();
        long elapsed = now - lastTimestampSeconds;
        if (elapsed < 0)
        {
            elapsed = 0;
            lastTimestampSeconds = now;
        }

        long intervalSeconds = GetRecoverySeconds();
        long maxNeeded = (long)(maxHearts - currentHearts) * intervalSeconds;
        if (elapsed > maxNeeded)
            elapsed = maxNeeded;

        if (elapsed < intervalSeconds)
            return;

        long gained = Math.Min(maxHearts - currentHearts, elapsed / intervalSeconds);
        if (gained <= 0)
            return;

        currentHearts += (int)gained;
        if (currentHearts >= maxHearts)
        {
            currentHearts = maxHearts;
            lastTimestampSeconds = now;
        }
        else
        {
            lastTimestampSeconds += gained * intervalSeconds;
        }
    }

    HeartStatus BuildStatus()
    {
        var status = new HeartStatus
        {
            Current = currentHearts,
            Max = maxHearts,
            IsFull = currentHearts >= maxHearts,
            SecondsToNext = 0,
            Extra = Mathf.Max(0, currentHearts - maxHearts)
        };

        if (status.IsFull)
            return status;

        long now = GetUtcNowSeconds();
        long elapsed = now - lastTimestampSeconds;
        if (elapsed < 0)
            elapsed = 0;

        long interval = GetRecoverySeconds();
        long remaining = interval - (elapsed % interval);
        if (remaining <= 0)
            remaining = interval;

        status.SecondsToNext = (int)Mathf.Clamp(remaining, 1, interval);
        return status;
    }

    void NotifyStatus()
    {
        HeartsChanged?.Invoke(BuildStatus());
    }

    void LoadState()
    {
        currentHearts = Mathf.Max(0, PlayerPrefs.GetInt(heartsKey, maxHearts));

        string ts = PlayerPrefs.GetString(timestampKey, string.Empty);
        if (!long.TryParse(ts, out lastTimestampSeconds) || lastTimestampSeconds <= 0)
            lastTimestampSeconds = GetUtcNowSeconds();
    }

    void SaveState()
    {
        PlayerPrefs.SetInt(heartsKey, currentHearts);
        PlayerPrefs.SetString(timestampKey, lastTimestampSeconds.ToString());
        PlayerPrefs.Save();
    }

    static long GetUtcNowSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    long GetRecoverySeconds()
    {
        int minutes = Mathf.Max(1, recoveryMinutes);
        return (long)minutes * 60L;
    }
}
