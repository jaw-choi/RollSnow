using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknamePanelUI : MonoBehaviour
{
    enum NicknameMode
    {
        FirstSetup,
        Change
    }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TextMeshProUGUI errorLabel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool openOnMissingNickname = true;

    [Header("Validation")]
    [SerializeField] private int minLength = 2;
    [SerializeField] private int maxLength = 12;
    [SerializeField] private string invalidMessage = "Nickname must be 2-12 characters.";
    [SerializeField] private string duplicateMessage = "Nickname already in use.";
    [SerializeField] private string offlineMessage = "Network unavailable. Try again.";
    [SerializeField] private string genericErrorMessage = "Nickname update failed.";

    NicknameMode currentMode = NicknameMode.FirstSetup;
    bool isBusy;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (confirmButton == null)
            Debug.LogWarning("[NicknamePanelUI] confirmButton is not assigned.");

        HookButtons(true);
        if (hideOnStart)
            SetVisible(false);
    }

    void OnEnable()
    {
        if (BackendManager.Instance != null)
        {
            BackendManager.Instance.LoginCompleted += HandleLoginCompleted;
            BackendManager.Instance.NicknameChanged += HandleNicknameChanged;
        }

        if (BackendManager.ConsumeRequireNickname())
            OpenForFirstSetup();

        RefreshGate();
    }

    void OnDisable()
    {
        if (BackendManager.Instance != null)
        {
            BackendManager.Instance.LoginCompleted -= HandleLoginCompleted;
            BackendManager.Instance.NicknameChanged -= HandleNicknameChanged;
        }
    }

    void HandleLoginCompleted()
    {
        RefreshGate();
    }

    void HandleNicknameChanged(string nickname)
    {
        if (currentMode == NicknameMode.FirstSetup)
            SetVisible(false);
    }

    void HookButtons(bool enable)
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (enable)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (enable)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    void RefreshGate()
    {
        if (!openOnMissingNickname)
            return;

        var backend = BackendManager.Instance;
        if (backend == null || !backend.IsLoggedIn)
            return;

        if (!backend.HasNickname)
            OpenForFirstSetup();
    }

    public void OpenForFirstSetup()
    {
        currentMode = NicknameMode.FirstSetup;
        PreparePanel();
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
        SetVisible(true);
    }

    public void OpenForChange()
    {
        currentMode = NicknameMode.Change;
        PreparePanel();
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(true);
        SetVisible(true);
    }

    void PreparePanel()
    {
        isBusy = false;
        if (errorLabel != null)
            errorLabel.text = string.Empty;
        if (nicknameInput != null)
            nicknameInput.text = string.Empty;
    }

    void OnConfirmClicked()
    {
        Debug.Log("[NicknamePanelUI] Confirm clicked.");
        if (isBusy)
            return;

        string nickname = nicknameInput != null ? nicknameInput.text.Trim() : string.Empty;
        Debug.Log($"[NicknamePanelUI] Input nickname='{nickname}'");
        if (!IsValid(nickname))
        {
            ShowError(invalidMessage);
            return;
        }

        if (BackendManager.Instance == null)
        {
            ShowError(genericErrorMessage);
            return;
        }

        isBusy = true;
        if (confirmButton != null)
            confirmButton.interactable = false;

        BackendManager.Instance.RequestNicknameUpdate(nickname, OnNicknameUpdateResult);
    }

    void OnNicknameUpdateResult(bool success, string reason)
    {
        Debug.Log($"[NicknamePanelUI] Update result. success={success}, reason={reason}");
        isBusy = false;
        if (confirmButton != null)
            confirmButton.interactable = true;

        if (success)
        {
            SetVisible(false);
            return;
        }

        if (reason == "DuplicateNickname")
            ShowError(duplicateMessage);
        else if (reason == "InvalidNickname")
            ShowError(invalidMessage);
        else if (reason == "Offline" || reason == "CheckFailed")
            ShowError(offlineMessage);
        else
            ShowError(genericErrorMessage);
    }

    void OnCancelClicked()
    {
        if (currentMode == NicknameMode.Change)
            SetVisible(false);
    }

    bool IsValid(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return false;

        int length = nickname.Length;
        return length >= minLength && length <= maxLength;
    }

    void ShowError(string message)
    {
        Debug.LogWarning("[NicknamePanelUI] " + message);
        if (errorLabel != null)
            errorLabel.text = message;
    }

    void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
    }
}
