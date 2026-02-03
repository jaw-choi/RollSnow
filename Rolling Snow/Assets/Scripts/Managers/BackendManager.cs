using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Functions;

public class BackendManager : MonoBehaviour
{
    const string LogTag = "[BackendManager]";

    public static BackendManager Instance { get; private set; }

    [Header("Auto Login")]
    [SerializeField] private bool autoLoginOnStart = true;
    [SerializeField] private bool forceEnableIfDisabledAtRuntime = true;
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private string idPrefKey = "Backend.CustomId";
    [SerializeField] private string pwPrefKey = "Backend.CustomPw";
    [SerializeField] private string nicknamePrefKey = "Backend.Nickname";
    [SerializeField] private string functionSignatureKey = "c2110ff1-f8e6-11f0-b5b6-e5df1ba1698210893";
    [SerializeField] private int signupRetryCount = 3;
    [SerializeField] private float signupRetryDelay = 0.2f;

    public bool IsInitialized { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public string UserId { get; private set; }
    public string Nickname { get; private set; }
    public bool HasNickname => !string.IsNullOrEmpty(Nickname);

    public event Action LoginCompleted;
    public event Action<string> NicknameChanged;

    public string FunctionSignatureKey => functionSignatureKey;

    public FirebaseAuth Auth { get; private set; }
    public FirebaseFirestore Firestore { get; private set; }
    public FirebaseFunctions Functions { get; private set; }

    static bool pendingAutoOpenRanking;
    static bool pendingRequireNickname;
    bool isHookedToGameManager;

    static string Mask(string value, int keep = 8)
    {
        if (string.IsNullOrEmpty(value))
            return "(empty)";

        if (value.Length <= keep)
            return value;

        return value.Substring(0, keep) + "...";
    }

    static string PrefState(string key)
    {
        bool hasKey = PlayerPrefs.HasKey(key);
        string value = PlayerPrefs.GetString(key, string.Empty);
        return $"{key}: hasKey={hasKey}, value={Mask(value)}";
    }

    void Awake()
    {
        Debug.Log($"{LogTag} Awake. object={name}, enabled={enabled}, activeInHierarchy={gameObject.activeInHierarchy}");
        if (!enabled)
        {
            Debug.LogWarning($"{LogTag} Component is disabled. OnEnable/Start will not run unless enabled.");
            if (forceEnableIfDisabledAtRuntime)
            {
                enabled = true;
                Debug.LogWarning($"{LogTag} Auto-enabled component. forceEnableIfDisabledAtRuntime={forceEnableIfDisabledAtRuntime}");
            }
        }

        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("Duplicate BackendManager detected. Destroying this instance.");
                Destroy(gameObject);
            }
            return;
        }

        Instance = this;
        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);

        Debug.Log($"{LogTag} Awake complete. persistBetweenScenes={persistBetweenScenes}");
        Debug.Log($"{LogTag} Pref snapshot @Awake -> {PrefState(idPrefKey)} | {PrefState(pwPrefKey)} | {PrefState(nicknamePrefKey)}");
    }

    void OnEnable()
    {
        Debug.Log($"{LogTag} OnEnable.");
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryHookGameManager();
    }

    void OnDisable()
    {
        Debug.Log($"{LogTag} OnDisable.");
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnhookGameManager();
    }

    IEnumerator Start()
    {
        Debug.Log($"{LogTag} Start. autoLoginOnStart={autoLoginOnStart}");
        if (!autoLoginOnStart)
        {
            Debug.LogWarning($"{LogTag} Auto login skipped because autoLoginOnStart=false.");
            yield break;
        }

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
        Debug.Log($"{LogTag} InitializeAndLogin begin.");
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result != DependencyStatus.Available)
        {
            Debug.LogError("Firebase dependencies not available: " + dependencyTask.Result);
            IsInitialized = false;
            Debug.Log($"{LogTag} InitializeAndLogin end. IsInitialized={IsInitialized}, IsLoggedIn={IsLoggedIn}");
            yield break;
        }

        try
        {
            Auth = FirebaseAuth.DefaultInstance;
            Firestore = FirebaseFirestore.DefaultInstance;
            Functions = FirebaseFunctions.DefaultInstance;
        }
        catch (Exception ex)
        {
            Auth = null;
            Firestore = null;
            Functions = null;
            IsInitialized = false;
            Debug.LogError("Firebase initialization failed. Ensure google-services json files exist in Assets/StreamingAssets. " + ex.Message);
            Debug.Log($"{LogTag} InitializeAndLogin end. IsInitialized={IsInitialized}, IsLoggedIn={IsLoggedIn}");
            yield break;
        }

        IsInitialized = true;
        Debug.Log($"{LogTag} Firebase initialized. IsInitialized={IsInitialized}");
        if (Firestore != null)
        {
            var enableNetworkTask = Firestore.EnableNetworkAsync();
            yield return new WaitUntil(() => enableNetworkTask.IsCompleted);
            if (enableNetworkTask.Exception != null)
                Debug.LogWarning($"{LogTag} Firestore network enable failed: {enableNetworkTask.Exception.GetBaseException().Message}");
            else
                Debug.Log($"{LogTag} Firestore network enabled.");
        }

        yield return AutoLogin();
        Debug.Log($"{LogTag} InitializeAndLogin end. IsInitialized={IsInitialized}, IsLoggedIn={IsLoggedIn}, UserId={Mask(UserId)}, HasNickname={HasNickname}");
    }

    IEnumerator AutoLogin()
    {
        bool createdNew = EnsureCredentials(out string id, out string pw);
        Debug.Log($"{LogTag} AutoLogin begin. createdNewCredentials={createdNew}, id={Mask(id)}");
        if (createdNew)
        {
            bool signedUp = false;
            for (int i = 0; i < Mathf.Max(1, signupRetryCount); i++)
            {
                Debug.Log($"{LogTag} CustomSignUp attempt {i + 1}/{Mathf.Max(1, signupRetryCount)} for id={Mask(id)}");
                bool ok = false;
                string error = string.Empty;
                yield return BackendLogin.Instance.CustomSignUp(id, pw, (success, message) =>
                {
                    ok = success;
                    error = message;
                });

                if (ok)
                {
                    signedUp = true;
                    Debug.Log($"{LogTag} CustomSignUp success.");
                    break;
                }

                Debug.LogWarning("Custom sign up failed: " + error);

                id = GenerateId();
                pw = GeneratePassword();
                SaveCredentials(id, pw);
                Debug.Log($"{LogTag} Regenerated credentials after sign-up failure. newId={Mask(id)}");

                if (signupRetryDelay > 0f)
                    yield return new WaitForSeconds(signupRetryDelay);
            }

            if (!signedUp)
                Debug.LogWarning("Custom sign up did not succeed after retries.");
        }

        Debug.Log($"{LogTag} CustomLogin attempt for id={Mask(id)}");
        bool loggedIn = false;
        string loginError = string.Empty;
        yield return BackendLogin.Instance.CustomLogin(id, pw, (success, message) =>
        {
            loggedIn = success;
            loginError = message;
        });

        if (!loggedIn)
        {
            IsLoggedIn = false;
            Debug.LogError("Auto login failed: " + loginError);
            Debug.Log($"{LogTag} AutoLogin end. IsLoggedIn={IsLoggedIn}, reason={loginError}");
            yield break;
        }

        IsLoggedIn = true;
        UserId = BackendLogin.Instance.CurrentUserId;
        Debug.Log($"{LogTag} CustomLogin success. userId={Mask(UserId)}");

        string backendNickname = string.Empty;
        bool hasBackendNickname = false;
        yield return BackendLogin.Instance.TryGetNickname((success, nickname) =>
        {
            hasBackendNickname = success;
            backendNickname = nickname;
        });
        Debug.Log($"{LogTag} TryGetNickname result. success={hasBackendNickname}, nickname='{backendNickname}'");

        if (!hasBackendNickname)
        {
            backendNickname = PlayerPrefs.GetString(nicknamePrefKey, string.Empty);
            Debug.Log($"{LogTag} Fallback nickname from PlayerPrefs. nickname='{backendNickname}'");
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

        Debug.Log($"{LogTag} Login state set. IsLoggedIn={IsLoggedIn}, HasNickname={HasNickname}");
        Debug.Log($"{LogTag} EnsureUserDocument begin.");
        yield return BackendGameData.Instance.EnsureUserDocument();
        Debug.Log($"{LogTag} EnsureUserDocument end.");

        if (HasNickname)
        {
            Debug.Log($"{LogTag} Sync rank entry on login.");
            StartCoroutine(BackendRank.Instance.RankInsert(GetLocalHighScore()));
        }

        RequireNicknameIfMissing();
        LoginCompleted?.Invoke();
        Debug.Log($"{LogTag} LoginCompleted invoked.");
    }

    bool EnsureCredentials(out string id, out string pw)
    {
        Debug.Log($"{LogTag} EnsureCredentials begin.");
        Debug.Log($"{LogTag} Pref snapshot @EnsureCredentials -> {PrefState(idPrefKey)} | {PrefState(pwPrefKey)}");

        id = PlayerPrefs.GetString(idPrefKey, string.Empty);
        pw = PlayerPrefs.GetString(pwPrefKey, string.Empty);

        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(pw))
        {
            Debug.Log($"{LogTag} Existing credentials found. id={Mask(id)}");
            return false;
        }

        if (string.IsNullOrEmpty(id))
            Debug.LogWarning($"{LogTag} Missing id in PlayerPrefs. key={idPrefKey}");
        if (string.IsNullOrEmpty(pw))
            Debug.LogWarning($"{LogTag} Missing password in PlayerPrefs. key={pwPrefKey}");

        id = GenerateId();
        pw = GeneratePassword();
        Debug.Log($"{LogTag} Generated credentials in-memory. id={Mask(id)}, pw={Mask(pw)}");
        SaveCredentials(id, pw);
        pendingRequireNickname = true;
        Debug.Log($"{LogTag} New credentials generated. id={Mask(id)}");
        return true;
    }

    void SaveCredentials(string id, string pw)
    {
        Debug.Log($"{LogTag} SaveCredentials begin. id={Mask(id)}, pw={Mask(pw)}");
        PlayerPrefs.SetString(idPrefKey, id);
        PlayerPrefs.SetString(pwPrefKey, pw);
        PlayerPrefs.Save();
        Debug.Log($"{LogTag} SaveCredentials end. {PrefState(idPrefKey)} | {PrefState(pwPrefKey)}");
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

        StartCoroutine(BackendRank.Instance.RankInsert(score));
        RequestRankingAutoOpen();
    }

    public void RequestNicknameUpdate(string nickname, Action<bool, string> onComplete)
    {
        StartCoroutine(RequestNicknameUpdateRoutine(nickname, onComplete));
    }

    IEnumerator RequestNicknameUpdateRoutine(string nickname, Action<bool, string> onComplete)
    {
        Debug.Log($"{LogTag} RequestNicknameUpdate begin. nickname='{nickname}'");
        if (!IsLoggedIn)
        {
            Debug.LogWarning($"{LogTag} RequestNicknameUpdate rejected: NotLoggedIn");
            onComplete?.Invoke(false, "NotLoggedIn");
            yield break;
        }

        if (string.IsNullOrEmpty(nickname) || nickname == Nickname)
        {
            Debug.LogWarning($"{LogTag} RequestNicknameUpdate rejected: InvalidNickname");
            onComplete?.Invoke(false, "InvalidNickname");
            yield break;
        }

        bool isAvailable = false;
        string checkError = string.Empty;
        yield return BackendLogin.Instance.CheckNickname(nickname, (success, message) =>
        {
            isAvailable = success;
            checkError = message;
        });

        if (!isAvailable)
        {
            Debug.LogWarning($"{LogTag} RequestNicknameUpdate check failed: {checkError}");
            onComplete?.Invoke(false, string.IsNullOrEmpty(checkError) ? "DuplicateNickname" : checkError);
            yield break;
        }

        bool updated = false;
        string updateError = string.Empty;
        yield return BackendLogin.Instance.UpdateNickname(nickname, (success, message) =>
        {
            updated = success;
            updateError = message;
        });

        if (!updated)
        {
            Debug.LogWarning($"{LogTag} RequestNicknameUpdate update failed: {updateError}");
            onComplete?.Invoke(false, string.IsNullOrEmpty(updateError) ? "UpdateFailed" : updateError);
            yield break;
        }

        Nickname = nickname;
        PlayerPrefs.SetString(nicknamePrefKey, nickname);
        PlayerPrefs.Save();

        RankingPanelUI.NotifyLocalNicknameUpdated(UserId, nickname);
        NicknameChanged?.Invoke(nickname);
        Debug.Log($"{LogTag} RequestNicknameUpdate success.");
        onComplete?.Invoke(true, string.Empty);

        // Close nickname UI immediately after core nickname update, then sync secondary data in background.
        StartCoroutine(SyncNicknameSideEffectsRoutine(nickname));
    }

    IEnumerator SyncNicknameSideEffectsRoutine(string nickname)
    {
        Debug.Log($"{LogTag} Nickname side-effect sync begin.");
        yield return BackendGameData.Instance.GameDataUpdate(null, nickname);
        yield return BackendRank.Instance.RankInsert(GetLocalHighScore());
        RankingPanelUI.NotifyLocalNicknameUpdated(UserId, nickname);
        NicknameChanged?.Invoke(nickname);
        Debug.Log($"{LogTag} Nickname side-effect sync end.");
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
            Debug.Log($"{LogTag} Nickname already present. No setup popup needed.");
            pendingRequireNickname = false;
            return;
        }

        bool opened = TryOpenNicknamePanel();
        Debug.Log($"{LogTag} Nickname missing. Open setup panel result={opened}");
        if (!opened)
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
