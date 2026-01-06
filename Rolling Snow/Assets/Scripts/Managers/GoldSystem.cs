using System;
using UnityEngine;

public class GoldSystem : MonoBehaviour
{
    private static GoldSystem instance;
    public static GoldSystem Instance => instance;

    public event Action<int> GoldChanged;

    [Header("Config")]
    [SerializeField] private int startingGold = 0;
    [SerializeField] private int maxGold = 999999;

    [Header("Storage Key")]
    [SerializeField] private string goldKey = "GoldSystem.Gold";

    private int currentGold;
    private bool isInitialized;

    public static GoldSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindObjectOfType<GoldSystem>();
        if (instance == null)
        {
            var go = new GameObject("GoldSystem");
            instance = go.AddComponent<GoldSystem>();
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
        Notify();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SaveState();
            instance = null;
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            SaveState();
    }

    void OnValidate()
    {
        if (maxGold < 0) maxGold = 0;
        if (startingGold < 0) startingGold = 0;
    }

    public int GetGold()
    {
        Initialize();
        return currentGold;
    }

    public void SetGold(int amount)
    {
        Initialize();
        int clamped = Mathf.Clamp(amount, 0, maxGold);
        if (currentGold == clamped)
            return;

        currentGold = clamped;
        SaveState();
        Notify();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        SetGold(currentGold + amount);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        Initialize();
        if (currentGold < amount)
            return false;

        currentGold -= amount;
        SaveState();
        Notify();
        return true;
    }

    public void ResetGold()
    {
        SetGold(startingGold);
    }

    void Initialize()
    {
        if (isInitialized)
            return;

        if (maxGold < 0) maxGold = 0;
        if (startingGold < 0) startingGold = 0;

        LoadState();
        isInitialized = true;
    }

    void LoadState()
    {
        if (PlayerPrefs.HasKey(goldKey))
            currentGold = PlayerPrefs.GetInt(goldKey, startingGold);
        else
            currentGold = startingGold;

        currentGold = Mathf.Clamp(currentGold, 0, maxGold);
    }

    void SaveState()
    {
        PlayerPrefs.SetInt(goldKey, currentGold);
        PlayerPrefs.Save();
    }

    void Notify()
    {
        GoldChanged?.Invoke(currentGold);
    }
}
