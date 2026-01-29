using System.Collections;
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

    [Header("Daily Reward Claim")]
    [SerializeField] private bool enableDailyRankReward = true;
    [SerializeField] private string rewardFunctionName = "DailyRankReward";
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TextMeshProUGUI claimRewardButtonLabel;
    [SerializeField] private bool autoCreateClaimButton = true;
    [SerializeField] private Vector2 claimButtonSize = new Vector2(170f, 34f);
    [SerializeField] private Vector2 claimButtonOffset = new Vector2(0f, -28f);
    [SerializeField] private string claimAvailableText = "Claim reward";
    [SerializeField] private string claimClaimedText = "Claimed";
    [SerializeField] private string claimUnavailableText = "Not eligible";
    [SerializeField] private string claimCheckingText = "Checking...";
    [SerializeField] private string claimWorkingText = "Claiming...";
    [SerializeField] private string claimFailedText = "Claim failed";
    [SerializeField] private string claimSuccessText = "Claimed +{0} gold";

    readonly List<RankingEntryUI> spawnedEntries = new List<RankingEntryUI>();
    BackendRankReward rewardService;
    Coroutine claimFeedbackRoutine;
    bool claimInProgress;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (hideOnStart)
            SetVisible(false);

        EnsureClaimButton();
        HookClaimButton(true);
    }

    void OnEnable()
    {
        if (BackendManager.ConsumeAutoOpenRanking())
            OpenAndRefresh();

        if (BackendManager.Instance != null)
            BackendManager.Instance.NicknameChanged += HandleNicknameChanged;

        EnsureClaimButton();
        HookClaimButton(true);
    }

    void OnDisable()
    {
        if (BackendManager.Instance != null)
            BackendManager.Instance.NicknameChanged -= HandleNicknameChanged;

        HookClaimButton(false);
        StopClaimFeedbackRoutine();
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
            RefreshRewardStatus();
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
        RefreshRewardStatus();
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

    BackendRankReward GetRewardService()
    {
        if (rewardService == null)
            rewardService = new BackendRankReward();

        rewardService.FunctionName = rewardFunctionName;
        return rewardService;
    }

    void EnsureClaimButton()
    {
        if (!enableDailyRankReward)
        {
            if (claimRewardButton != null)
                claimRewardButton.gameObject.SetActive(false);
            return;
        }

        if (claimRewardButton != null)
            return;

        if (!autoCreateClaimButton || dailyRewardLabel == null)
            return;

        var buttonGo = new GameObject("RankRewardClaimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.layer = dailyRewardLabel.gameObject.layer;
        buttonGo.transform.SetParent(dailyRewardLabel.transform, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = claimButtonOffset;
        rect.sizeDelta = claimButtonSize;

        var image = buttonGo.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.layer = buttonGo.layer;
        labelGo.transform.SetParent(buttonGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.font = dailyRewardLabel.font;
        label.fontSize = Mathf.Max(12f, dailyRewardLabel.fontSize * 0.6f);
        label.color = dailyRewardLabel.color;
        label.text = claimUnavailableText;

        claimRewardButton = button;
        claimRewardButtonLabel = label;
    }

    void HookClaimButton(bool on)
    {
        if (claimRewardButton == null)
            return;

        if (!on)
        {
            claimRewardButton.onClick.RemoveListener(OnClaimRewardClicked);
            return;
        }

        claimRewardButton.onClick.RemoveListener(OnClaimRewardClicked);
        claimRewardButton.onClick.AddListener(OnClaimRewardClicked);
    }

    void RefreshRewardStatus()
    {
        if (!enableDailyRankReward)
        {
            if (claimRewardButton != null)
                claimRewardButton.gameObject.SetActive(false);
            return;
        }

        if (claimInProgress || claimFeedbackRoutine != null)
            return;

        EnsureClaimButton();
        HookClaimButton(true);

        if (claimRewardButton == null || claimRewardButtonLabel == null)
            return;

        claimRewardButton.gameObject.SetActive(true);

        if (BackendManager.Instance == null || !BackendManager.Instance.IsLoggedIn)
        {
            claimRewardButton.interactable = false;
            SetClaimLabel(claimUnavailableText);
            return;
        }

        claimRewardButton.interactable = false;
        SetClaimLabel(claimCheckingText);

        var service = GetRewardService();
        if (!service.TryGetStatus(out var status))
        {
            SetClaimLabel(claimFailedText);
            return;
        }

        if (!string.IsNullOrEmpty(status.WinnerNickname))
        {
            BackendRank.Instance.SetCachedYesterdayWinner(status.WinnerNickname);
            if (yesterdayWinnerLabel != null)
                yesterdayWinnerLabel.text = string.Format(yesterdayWinnerFormat, status.WinnerNickname);
        }

        if (status.IsClaimed)
        {
            SetClaimLabel(claimClaimedText);
            claimRewardButton.interactable = false;
            return;
        }

        if (status.IsClaimable)
        {
            SetClaimLabel(claimAvailableText);
            claimRewardButton.interactable = true;
            return;
        }

        SetClaimLabel(claimUnavailableText);
        claimRewardButton.interactable = false;
    }

    void SetClaimLabel(string text)
    {
        if (claimRewardButtonLabel != null)
            claimRewardButtonLabel.text = text;
    }

    void OnClaimRewardClicked()
    {
        if (claimInProgress)
            return;

        claimInProgress = true;
        if (claimRewardButton != null)
            claimRewardButton.interactable = false;
        SetClaimLabel(claimWorkingText);

        var service = GetRewardService();
        if (service.TryClaim(out var result))
        {
            if (!string.IsNullOrEmpty(result.WinnerNickname))
                BackendRank.Instance.SetCachedYesterdayWinner(result.WinnerNickname);

            int gold = Mathf.Max(0, result.RewardGold);
            if (gold > 0)
            {
                var goldSystem = GoldSystem.GetOrCreate();
                if (goldSystem != null)
                    goldSystem.AddGold(gold);
            }

            string message = gold > 0 ? string.Format(claimSuccessText, gold) : claimClaimedText;
            StartClaimFeedback(message);
        }
        else
        {
            StartClaimFeedback(claimFailedText);
        }

        claimInProgress = false;
    }

    void StartClaimFeedback(string message)
    {
        StopClaimFeedbackRoutine();
        claimFeedbackRoutine = StartCoroutine(ClaimFeedbackRoutine(message));
    }

    IEnumerator ClaimFeedbackRoutine(string message)
    {
        SetClaimLabel(message);
        yield return new WaitForSeconds(1.2f);
        claimFeedbackRoutine = null;
        RefreshRewardStatus();
    }

    void StopClaimFeedbackRoutine()
    {
        if (claimFeedbackRoutine == null)
            return;

        StopCoroutine(claimFeedbackRoutine);
        claimFeedbackRoutine = null;
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
