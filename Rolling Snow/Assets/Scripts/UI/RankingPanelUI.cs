using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingPanelUI : MonoBehaviour
{
    public static RankingPanelUI Instance { get; private set; }
    static string pendingNicknameUserId;
    static string pendingNicknameValue;

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

    [Header("Reward Images")]
    [SerializeField] private Sprite rewardImageRank1;
    [SerializeField] private Sprite rewardImageRank2;
    [SerializeField] private Sprite rewardImageRank3;
    [SerializeField] private GameObject alarmDot;
    [SerializeField] private CanvasGroup rewardImageCanvasGroup;

    [Header("Text")]
    [SerializeField] private string myRankFormat = "My Rank: {0}";
    [SerializeField] private string topRankFormat = "Top 1: {0} ({1})";
    [SerializeField] private string dailyRewardNotice = "Ranking reset in: {0}";
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

    [Header("Countdown Timer")]
    [SerializeField] private float countdownUpdateInterval = 1f;
    [SerializeField] private float rewardStatusCheckInterval = 5f;

    readonly List<RankingEntryUI> spawnedEntries = new List<RankingEntryUI>();
    readonly List<BackendRank.RankEntry> visibleEntries = new List<BackendRank.RankEntry>();
    BackendRankReward rewardService;
    Coroutine claimFeedbackRoutine;
    Coroutine countdownTimerRoutine;
    Coroutine refreshRankingsRoutine;
    Coroutine refreshRewardRoutine;
    bool claimInProgress;
    int currentPlayerRank = -1;

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
        StopCountdownTimer();
    }

    void HandleNicknameChanged(string nickname)
    {
        NotifyLocalNicknameUpdated(GetCurrentUserId(), nickname);
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
        if (refreshRankingsRoutine != null)
            StopCoroutine(refreshRankingsRoutine);

        refreshRankingsRoutine = StartCoroutine(RefreshRankingsRoutine());
    }

    IEnumerator RefreshRankingsRoutine()
    {
        string yesterdayWinner = BackendRank.Instance.GetCachedYesterdayWinner();
        if (yesterdayWinnerLabel != null)
            yesterdayWinnerLabel.text = string.Format(yesterdayWinnerFormat,
                string.IsNullOrEmpty(yesterdayWinner) ? "-" : yesterdayWinner);

        bool success = false;
        List<BackendRank.RankEntry> entries = null;
        yield return BackendRank.Instance.FetchRankList((ok, list) =>
        {
            success = ok;
            entries = list;
        });

        if (!success || entries == null)
        {
            ApplyEmptyLabels();
            ClearEntries();
            currentPlayerRank = -1;
            RefreshRewardStatus();
            refreshRankingsRoutine = null;
            yield break;
        }

        ApplyPendingNicknameOverride(entries);

        var myEntry = BackendRank.Instance.FindMyEntry(entries,
            GetCurrentUserId(),
            BackendManager.Instance != null ? BackendManager.Instance.Nickname : string.Empty);

        currentPlayerRank = myEntry.Rank;

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
            string myNickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : myEntry.Nickname;
            myRankLabel.text = FormatMyRankLabel(rankText, myNickname);
        }

        PopulateEntries(entries, myEntry);
        StartCountdownTimer();
        RefreshRewardStatus();
        refreshRankingsRoutine = null;
    }

    void ApplyEmptyLabels()
    {
        if (topRankLabel != null)
            topRankLabel.text = string.Format(topRankFormat, "-", "0");
        if (myRankLabel != null)
        {
            string myNickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : string.Empty;
            myRankLabel.text = FormatMyRankLabel("-", myNickname);
        }
    }

    void PopulateEntries(List<BackendRank.RankEntry> entries, BackendRank.RankEntry myEntry)
    {
        ClearEntries();
        if (entryPrefab == null || listContent == null)
            return;

        string myNickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : myEntry.Nickname;
        string myUserId = GetCurrentUserId();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var item = Instantiate(entryPrefab, listContent);
            bool isMe = !string.IsNullOrEmpty(myUserId) && entry.UserId == myUserId;
            string displayNickname = isMe && !string.IsNullOrEmpty(myNickname) ? myNickname : entry.Nickname;
            bool highlight = isMe || (!string.IsNullOrEmpty(myNickname) && entry.Nickname == myNickname);
            item.SetEntry(entry.Rank, displayNickname, entry.Score, highlight);
            visibleEntries.Add(new BackendRank.RankEntry
            {
                Rank = entry.Rank,
                Nickname = displayNickname,
                Score = entry.Score,
                GamerInDate = entry.GamerInDate,
                Index = entry.Index,
                UserId = entry.UserId
            });
            spawnedEntries.Add(item);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    string FormatMyRankLabel(string rankText, string nickname)
    {
        string safeNickname = string.IsNullOrEmpty(nickname) ? "-" : nickname;
        if (string.IsNullOrEmpty(myRankFormat))
            return $"My Rank: {rankText} ({safeNickname})";

        if (myRankFormat.Contains("{1}"))
            return string.Format(myRankFormat, rankText, safeNickname);

        return string.Format(myRankFormat, rankText) + $" ({safeNickname})";
    }

    string GetCurrentUserId()
    {
        string managerUserId = BackendManager.Instance != null ? BackendManager.Instance.UserId : string.Empty;
        if (!string.IsNullOrEmpty(managerUserId))
            return managerUserId;

        return BackendLogin.Instance.CurrentUserId;
    }

    void ApplyPendingNicknameOverride(List<BackendRank.RankEntry> entries)
    {
        if (entries == null || string.IsNullOrEmpty(pendingNicknameUserId) || string.IsNullOrEmpty(pendingNicknameValue))
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.UserId != pendingNicknameUserId)
                continue;

            entry.Nickname = pendingNicknameValue;
            entries[i] = entry;
        }
    }

    void ApplyNicknameToVisibleEntries(string userId, string nickname)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return;

        bool patched = false;
        for (int i = 0; i < spawnedEntries.Count && i < visibleEntries.Count; i++)
        {
            if (visibleEntries[i].UserId != userId)
                continue;

            var entry = visibleEntries[i];
            entry.Nickname = nickname;
            visibleEntries[i] = entry;
            spawnedEntries[i].SetEntry(entry.Rank, nickname, entry.Score, true);
            patched = true;
        }

        if (myRankLabel != null)
        {
            string rankText = currentPlayerRank > 0 ? currentPlayerRank.ToString() : "-";
            myRankLabel.text = FormatMyRankLabel(rankText, nickname);
        }

        if (!patched && IsVisible())
            RefreshRankings();
    }

    public static void NotifyLocalNicknameUpdated(string userId, string nickname)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return;

        pendingNicknameUserId = userId;
        pendingNicknameValue = nickname;

        if (Instance != null)
            Instance.ApplyNicknameToVisibleEntries(userId, nickname);
    }

    void ClearEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
                Destroy(spawnedEntries[i].gameObject);
        }

        spawnedEntries.Clear();
        visibleEntries.Clear();
    }

    BackendRankReward GetRewardService()
    {
        if (rewardService == null)
            rewardService = new BackendRankReward();

        rewardService.FunctionName = rewardFunctionName;
        if (BackendManager.Instance != null)
        {
            rewardService.FunctionKey = BackendManager.Instance.FunctionSignatureKey;
            if (string.IsNullOrEmpty(rewardService.FunctionKey))
            {
                Debug.LogWarning("FunctionSignatureKey is empty in BackendManager. Check Inspector.");
            }
        }
        else
        {
            Debug.LogError("BackendManager.Instance is null. Cannot set FunctionKey.");
        }
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
            HideAlarmDot();
            return;
        }

        if (claimInProgress || refreshRewardRoutine != null)
            return;

        refreshRewardRoutine = StartCoroutine(RefreshRewardStatusRoutine());
    }

    IEnumerator RefreshRewardStatusRoutine()
    {
        EnsureClaimButton();
        HookClaimButton(true);

        if (claimRewardButton == null || claimRewardButtonLabel == null)
        {
            refreshRewardRoutine = null;
            yield break;
        }

        if (BackendManager.Instance == null || !BackendManager.Instance.IsLoggedIn)
        {
            claimRewardButton.gameObject.SetActive(false);
            HideAlarmDot();
            refreshRewardRoutine = null;
            yield break;
        }

        if (currentPlayerRank < 1 || currentPlayerRank > 3)
        {
            claimRewardButton.gameObject.SetActive(false);
            HideRewardImage();
            HideAlarmDot();
            refreshRewardRoutine = null;
            yield break;
        }

        claimRewardButton.interactable = false;
        SetClaimLabel(claimCheckingText);

        var service = GetRewardService();
        bool success = false;
        BackendRankReward.RewardStatus status = new BackendRankReward.RewardStatus();
        yield return service.TryGetStatus((ok, result) =>
        {
            success = ok;
            status = result;
        });

        refreshRewardRoutine = null;

        if (!success)
        {
            SetClaimLabel(claimFailedText);
            claimRewardButton.gameObject.SetActive(false);
            HideAlarmDot();
            yield break;
        }

        Debug.Log($"[RankRewardStatus] Success={status.IsSuccess}, Claimable={status.IsClaimable}, Claimed={status.IsClaimed}, Gold={status.RewardGold}, Winner={status.WinnerNickname}, Date={status.RewardDate}, Remaining={status.RemainingSeconds}, Message={status.Message}");

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
            claimRewardButton.gameObject.SetActive(false);
            HideAlarmDot();
            yield break;
        }

        if (status.IsClaimable)
        {
            SetClaimLabel(claimAvailableText);
            claimRewardButton.interactable = true;
            claimRewardButton.gameObject.SetActive(true);
            ShowAlarmDot();
            yield break;
        }

        SetClaimLabel(claimUnavailableText);
        claimRewardButton.interactable = false;
        claimRewardButton.gameObject.SetActive(false);
        HideAlarmDot();
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

        if (rewardImageCanvasGroup != null && rewardImageCanvasGroup.gameObject.activeSelf)
        {
            HideRewardImage();
            return;
        }

        ShowRewardImage();
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

    void StartCountdownTimer()
    {
        StopCountdownTimer();
        countdownTimerRoutine = StartCoroutine(CountdownTimerRoutine());
    }

    void StopCountdownTimer()
    {
        if (countdownTimerRoutine == null)
            return;

        StopCoroutine(countdownTimerRoutine);
        countdownTimerRoutine = null;
    }

    IEnumerator CountdownTimerRoutine()
    {
        float timeSinceLastStatusCheck = rewardStatusCheckInterval;
        int localRemainingSeconds = 0;
        bool hadValidRemaining = false;
        bool didZeroRefresh = false;

        while (true)
        {
            timeSinceLastStatusCheck += countdownUpdateInterval;

            if (timeSinceLastStatusCheck >= rewardStatusCheckInterval)
            {
                var service = GetRewardService();
                int remainingFrom3_47PM = service.GetRemainingSecondsUntil3_47PM();
                if (remainingFrom3_47PM > 0)
                {
                    localRemainingSeconds = remainingFrom3_47PM;
                    hadValidRemaining = true;
                    Debug.Log($"Server-calculated remaining seconds until 3:47 PM: {localRemainingSeconds}");
                }
                else
                {
                    Debug.Log("Failed to calculate remaining seconds from server time.");
                }
                timeSinceLastStatusCheck = 0f;
            }

            localRemainingSeconds = Mathf.Max(0, localRemainingSeconds - (int)countdownUpdateInterval);

            if (localRemainingSeconds <= 0 && !hadValidRemaining)
            {
                if (dailyRewardLabel != null)
                    dailyRewardLabel.text = string.Format(dailyRewardNotice, "-");
            }
            else
            {
                UpdateCountdownDisplay(localRemainingSeconds);
            }

            if (localRemainingSeconds <= 0 && hadValidRemaining && !didZeroRefresh)
            {
                Debug.Log("Countdown reached 0. Refreshing claim status (no auto-claim). ");
                RefreshRewardStatus();
                didZeroRefresh = true;
            }

            yield return new WaitForSeconds(countdownUpdateInterval);
        }
    }

    int ParseRemainingTime(string rewardDate)
    {
        if (string.IsNullOrEmpty(rewardDate))
            return 0;

        if (int.TryParse(rewardDate, out int seconds))
            return Mathf.Max(0, seconds);

        string[] parts = rewardDate.Split(':');
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out int hours) &&
                int.TryParse(parts[1], out int minutes) &&
                int.TryParse(parts[2], out int secs))
            {
                return hours * 3600 + minutes * 60 + secs;
            }
        }

        return 0;
    }

    void UpdateCountdownDisplay(int remainingSeconds)
    {
        if (dailyRewardLabel == null)
            return;

        int hours = remainingSeconds / 3600;
        int minutes = (remainingSeconds % 3600) / 60;
        int seconds = remainingSeconds % 60;

        string timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        dailyRewardLabel.text = string.Format(dailyRewardNotice, timeStr);
    }

    void ShowRewardImage()
    {
        if (rewardImageCanvasGroup == null)
            return;

        if (currentPlayerRank < 1 || currentPlayerRank > 3)
        {
            HideRewardImage();
            return;
        }

        Sprite spriteToShow = null;
        switch (currentPlayerRank)
        {
            case 1:
                spriteToShow = rewardImageRank1;
                break;
            case 2:
                spriteToShow = rewardImageRank2;
                break;
            case 3:
                spriteToShow = rewardImageRank3;
                break;
        }

        Image image = rewardImageCanvasGroup.GetComponent<Image>();
        if (image != null && spriteToShow != null)
        {
            image.sprite = spriteToShow;
            rewardImageCanvasGroup.gameObject.SetActive(true);
            rewardImageCanvasGroup.alpha = 1f;
        }
    }

    void HideRewardImage()
    {
        if (rewardImageCanvasGroup != null)
        {
            rewardImageCanvasGroup.gameObject.SetActive(false);
        }
    }

    void ShowAlarmDot()
    {
        if (alarmDot != null)
            alarmDot.SetActive(true);
    }

    void HideAlarmDot()
    {
        if (alarmDot != null)
            alarmDot.SetActive(false);
    }

    void AutoClaimReward()
    {
        if (claimInProgress)
            return;

        StartCoroutine(AutoClaimRewardRoutine());
    }

    IEnumerator AutoClaimRewardRoutine()
    {
        claimInProgress = true;

        var service = GetRewardService();
        bool success = false;
        BackendRankReward.ClaimResult result = new BackendRankReward.ClaimResult();
        yield return service.TryClaim((ok, claimResult) =>
        {
            success = ok;
            result = claimResult;
        });

        if (success)
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

            SetVisible(false);
        }

        claimInProgress = false;
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
