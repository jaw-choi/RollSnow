using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingPanelUI : MonoBehaviour
{
    public static RankingPanelUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI myRankLabel;
    [SerializeField] private TextMeshProUGUI topRankLabel;
    [SerializeField] private TextMeshProUGUI dailyRewardLabel;
    [SerializeField] private TextMeshProUGUI yesterdayWinnerLabel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform listContent;
    [SerializeField] private RankingEntryUI entryPrefab;
    [SerializeField] private bool hideOnStart = true;

    [Header("Text")]
    [SerializeField] private string myRankFormat = "My Rank: {0}";
    [SerializeField] private string topRankFormat = "Top 1: {0} ({1})";
    [SerializeField] private string dailyRewardNotice = "18:00 top1 reward: 300 gold";
    [SerializeField] private string yesterdayWinnerFormat = "Yesterday reward: {0}";

    readonly List<RankingEntryUI> spawnedEntries = new List<RankingEntryUI>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (hideOnStart)
            SetVisible(false);
    }

    void OnEnable()
    {
        if (BackendManager.ConsumeAutoOpenRanking())
            OpenAndRefresh();

        if (BackendManager.Instance != null)
            BackendManager.Instance.NicknameChanged += HandleNicknameChanged;
    }

    void OnDisable()
    {
        if (BackendManager.Instance != null)
            BackendManager.Instance.NicknameChanged -= HandleNicknameChanged;
    }

    void HandleNicknameChanged(string nickname)
    {
        if (IsVisible())
            RefreshRankings();
    }

    public void OpenAndRefresh()
    {
        SetVisible(true);
        RefreshRankings();
    }

    public void Close()
    {
        SetVisible(false);
    }

    public bool IsVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
    }

    public void RefreshRankings()
    {
        if (dailyRewardLabel != null)
            dailyRewardLabel.text = dailyRewardNotice;

        string yesterdayWinner = BackendRank.Instance.GetCachedYesterdayWinner();
        if (yesterdayWinnerLabel != null)
            yesterdayWinnerLabel.text = string.Format(yesterdayWinnerFormat,
                string.IsNullOrEmpty(yesterdayWinner) ? "-" : yesterdayWinner);

        if (!BackendRank.Instance.TryGetRankList(out List<BackendRank.RankEntry> entries))
        {
            ApplyEmptyLabels();
            ClearEntries();
            return;
        }

        BackendRank.Instance.TryGetMyRank(out BackendRank.RankEntry myEntry);

        if (entries.Count > 0)
        {
            var top = entries[0];
            if (topRankLabel != null)
                topRankLabel.text = string.Format(topRankFormat, top.Nickname, top.Score);
        }
        else
        {
            if (topRankLabel != null)
                topRankLabel.text = string.Format(topRankFormat, "-", "0");
        }

        if (myRankLabel != null)
        {
            string rankText = myEntry.Rank > 0 ? myEntry.Rank.ToString() : "-";
            myRankLabel.text = string.Format(myRankFormat, rankText);
        }

        PopulateEntries(entries, myEntry);
    }

    void ApplyEmptyLabels()
    {
        if (topRankLabel != null)
            topRankLabel.text = string.Format(topRankFormat, "-", "0");
        if (myRankLabel != null)
            myRankLabel.text = string.Format(myRankFormat, "-");
    }

    void PopulateEntries(List<BackendRank.RankEntry> entries, BackendRank.RankEntry myEntry)
    {
        ClearEntries();
        if (entryPrefab == null || listContent == null)
            return;

        string myNickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : myEntry.Nickname;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var item = Instantiate(entryPrefab, listContent);
            bool highlight = !string.IsNullOrEmpty(myNickname) && entry.Nickname == myNickname;
            item.SetEntry(entry.Rank, entry.Nickname, entry.Score, highlight);
            spawnedEntries.Add(item);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    void ClearEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
                Destroy(spawnedEntries[i].gameObject);
        }

        spawnedEntries.Clear();
    }

    public static bool TryOpenAndRefresh()
    {
        if (Instance == null)
        {
            Instance = FindObjectOfType<RankingPanelUI>(true);
            if (Instance == null)
                return false;
        }

        Instance.OpenAndRefresh();
        return true;
    }
}
