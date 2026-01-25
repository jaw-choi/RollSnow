using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Backend SDK namespace
using BackEnd;

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance { get; private set; }

    [Header("Auto Login")]
    [SerializeField] private bool autoLoginOnStart = true;
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private string idPrefKey = "Backend.CustomId";
    [SerializeField] private string pwPrefKey = "Backend.CustomPw";
    [SerializeField] private string nicknamePrefKey = "Backend.Nickname";
    [SerializeField] private int signupRetryCount = 3;
    [SerializeField] private float signupRetryDelay = 0.2f;

    public bool IsInitialized { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public string UserId { get; private set; }
    public string Nickname { get; private set; }
    public bool HasNickname => !string.IsNullOrEmpty(Nickname);

    public event Action LoginCompleted;
    public event Action<string> NicknameChanged;

    static bool pendingAutoOpenRanking;
    static bool pendingRequireNickname;
    bool isHookedToGameManager;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryHookGameManager();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnhookGameManager();
    }

    IEnumerator Start()
    {
        if (!autoLoginOnStart)
            yield break;

        yield return InitializeAndLogin();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryHookGameManager();
        if (pendingRequireNickname)
            TryOpenNicknamePanel();
    }

    void TryHookGameManager()
    {
        if (isHookedToGameManager)
            return;

        if (GameManager.Instance == null)
            return;

        GameManager.Instance.HighScoreUpdated += HandleHighScoreUpdated;
        isHookedToGameManager = true;
    }

    void UnhookGameManager()
    {
        if (!isHookedToGameManager)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.HighScoreUpdated -= HandleHighScoreUpdated;

        isHookedToGameManager = false;
    }

    public IEnumerator InitializeAndLogin()
    {
        var bro = Backend.Initialize();
        if (!bro.IsSuccess())
        {
            Debug.LogError("Backend initialize failed: " + bro);
            IsInitialized = false;
            yield break;
        }

        Debug.Log("Backend initialize success: " + bro);
        IsInitialized = true;

        yield return AutoLogin();
    }

    IEnumerator AutoLogin()
    {
        bool createdNew = EnsureCredentials(out string id, out string pw);
        if (createdNew)
        {
            bool signedUp = false;
            for (int i = 0; i < Mathf.Max(1, signupRetryCount); i++)
            {
                var bro = BackendLogin.Instance.CustomSignUp(id, pw);
                if (bro.IsSuccess())
                {
                    signedUp = true;
                    break;
                }

                id = GenerateId();
                pw = GeneratePassword();
                SaveCredentials(id, pw);

                if (signupRetryDelay > 0f)
                    yield return new WaitForSeconds(signupRetryDelay);
            }

            if (!signedUp)
                Debug.LogWarning("Custom sign up did not succeed after retries.");
        }

        var loginBro = BackendLogin.Instance.CustomLogin(id, pw);
        if (!loginBro.IsSuccess())
        {
            IsLoggedIn = false;
            Debug.LogError("Auto login failed: " + loginBro);
            yield break;
        }

        IsLoggedIn = true;
        UserId = id;

        if (!BackendLogin.Instance.TryGetNickname(out string backendNickname))
        {
            backendNickname = PlayerPrefs.GetString(nicknamePrefKey, string.Empty);
        }

        if (!string.IsNullOrEmpty(backendNickname))
        {
            Nickname = backendNickname;
            PlayerPrefs.SetString(nicknamePrefKey, backendNickname);
            PlayerPrefs.Save();
        }
        else
        {
            Nickname = string.Empty;
        }

        BackendGameData.Instance.EnsureRowInDate();
        RequireNicknameIfMissing();
        LoginCompleted?.Invoke();
    }

    bool EnsureCredentials(out string id, out string pw)
    {
        id = PlayerPrefs.GetString(idPrefKey, string.Empty);
        pw = PlayerPrefs.GetString(pwPrefKey, string.Empty);

        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(pw))
            return false;

        id = GenerateId();
        pw = GeneratePassword();
        SaveCredentials(id, pw);
        return true;
    }

    void SaveCredentials(string id, string pw)
    {
        PlayerPrefs.SetString(idPrefKey, id);
        PlayerPrefs.SetString(pwPrefKey, pw);
        PlayerPrefs.Save();
    }

    string GenerateId()
    {
        return "guest_" + Guid.NewGuid().ToString("N");
    }

    string GeneratePassword()
    {
        return "pw_" + Guid.NewGuid().ToString("N");
    }

    void HandleHighScoreUpdated(int score)
    {
        if (!IsLoggedIn)
            return;

        BackendRank.Instance.RankInsert(score);
        RequestRankingAutoOpen();
    }

    public void RequestNicknameUpdate(string nickname, Action<bool, string> onComplete)
    {
        StartCoroutine(RequestNicknameUpdateRoutine(nickname, onComplete));
    }

    IEnumerator RequestNicknameUpdateRoutine(string nickname, Action<bool, string> onComplete)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(false, "NotLoggedIn");
            yield break;
        }

        if (string.IsNullOrEmpty(nickname) || nickname == Nickname)
        {
            onComplete?.Invoke(false, "InvalidNickname");
            yield break;
        }

        var checkBro = BackendLogin.Instance.CheckNickname(nickname);
        if (!checkBro.IsSuccess())
        {
            onComplete?.Invoke(false, "DuplicateNickname");
            yield break;
        }

        var updateBro = BackendLogin.Instance.UpdateNickname(nickname);
        if (!updateBro.IsSuccess())
        {
            onComplete?.Invoke(false, "UpdateFailed");
            yield break;
        }

        Nickname = nickname;
        PlayerPrefs.SetString(nicknamePrefKey, nickname);
        PlayerPrefs.Save();

        BackendGameData.Instance.GameDataUpdate(null, nickname);
        BackendRank.Instance.RankInsert(GetLocalHighScore());

        NicknameChanged?.Invoke(nickname);
        onComplete?.Invoke(true, string.Empty);
    }

    int GetLocalHighScore()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.GetHighScore();

        return PlayerPrefs.GetInt("HighScore", 0);
    }

    void RequireNicknameIfMissing()
    {
        if (HasNickname)
        {
            pendingRequireNickname = false;
            return;
        }

        if (!TryOpenNicknamePanel())
            pendingRequireNickname = true;
        else
            pendingRequireNickname = false;
    }

    bool TryOpenNicknamePanel()
    {
        var panel = FindObjectOfType<NicknamePanelUI>(true);
        if (panel == null)
            return false;

        panel.OpenForFirstSetup();
        return true;
    }

    public void RequestRankingAutoOpen()
    {
        if (RankingPanelUI.TryOpenAndRefresh())
        {
            pendingAutoOpenRanking = false;
            return;
        }

        pendingAutoOpenRanking = true;
    }

    public static bool ConsumeAutoOpenRanking()
    {
        if (!pendingAutoOpenRanking)
            return false;

        pendingAutoOpenRanking = false;
        return true;
    }

    public static bool ConsumeRequireNickname()
    {
        if (!pendingRequireNickname)
            return false;

        pendingRequireNickname = false;
        return true;
    }
}
