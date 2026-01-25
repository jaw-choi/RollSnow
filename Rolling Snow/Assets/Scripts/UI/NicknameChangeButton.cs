using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameChangeButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NicknamePanelUI nicknamePanel;
    [SerializeField] private RewardedAdManager rewardedAdManager;

    [Header("Confirm")]
    [SerializeField] private bool showConfirm = true;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmMessageLabel;
    [SerializeField] private string confirmMessage = "Watch an ad to change nickname.";
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private string adNotReadyMessage = "Ad not ready.";
    [SerializeField] private string adLoadingMessage = "Loading ad...";
    [SerializeField] private float adWaitTimeoutSeconds = 8f;
    [SerializeField] private float adWaitPollSeconds = 0.2f;

    bool isWaitingForAd;
    Coroutine adWaitRoutine;

    void Awake()
    {
        HookConfirmButtons(true);
        SetConfirmVisible(false);
    }

    void OnDestroy()
    {
        StopAdWait();
        HookConfirmButtons(false);
    }

    void HookConfirmButtons(bool enable)
    {
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(OnConfirmYes);
            if (enable)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(OnConfirmNo);
            if (enable)
                confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
    }

    public void OnChangeNicknameClicked()
    {
        if (!showConfirm)
        {
            StartAdFlow();
            return;
        }

        if (confirmMessageLabel != null)
            confirmMessageLabel.text = confirmMessage;

        SetConfirmVisible(true);
    }

    void OnConfirmYes()
    {
        SetConfirmVisible(false);
        StartAdFlow();
    }

    void OnConfirmNo()
    {
        StopAdWait();
        SetConfirmVisible(false);
    }

    void StartAdFlow()
    {
        var manager = rewardedAdManager != null ? rewardedAdManager : RewardedAdManager.Instance;
        if (manager == null || !manager.IsReady())
        {
            ShowStatus(string.IsNullOrEmpty(adLoadingMessage) ? adNotReadyMessage : adLoadingMessage);
            StartAdWait(manager);
            return;
        }

        manager.ShowRewardedAd(() =>
        {
            if (nicknamePanel != null)
                nicknamePanel.OpenForChange();
        });
    }

    void StartAdWait(RewardedAdManager manager)
    {
        if (isWaitingForAd || manager == null)
            return;

        StopAdWait();
        adWaitRoutine = StartCoroutine(WaitForAdReady(manager));
    }

    System.Collections.IEnumerator WaitForAdReady(RewardedAdManager manager)
    {
        isWaitingForAd = true;
        float timeout = Mathf.Max(0f, adWaitTimeoutSeconds);
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (manager != null && manager.IsReady())
            {
                manager.ShowRewardedAd(() =>
                {
                    if (nicknamePanel != null)
                        nicknamePanel.OpenForChange();
                });
                isWaitingForAd = false;
                adWaitRoutine = null;
                yield break;
            }

            float step = Mathf.Max(0.05f, adWaitPollSeconds);
            yield return new WaitForSeconds(step);
            elapsed += step;
        }

        isWaitingForAd = false;
        adWaitRoutine = null;
        ShowStatus(adNotReadyMessage);
    }

    void StopAdWait()
    {
        if (adWaitRoutine != null)
        {
            StopCoroutine(adWaitRoutine);
            adWaitRoutine = null;
        }
        isWaitingForAd = false;
    }

    void ShowStatus(string message)
    {
        if (statusLabel != null)
            statusLabel.text = message;
    }

    void SetConfirmVisible(bool visible)
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(visible);
    }
}
