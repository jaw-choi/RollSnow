using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Current score (in seconds * scoreRate)")]
    public float score = 0f;
    [Tooltip("Points awarded per second while playing")]
    public float scoreRate = 1f;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Player References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerController playerController;

    [Header("World References")]
    [SerializeField] private TrailStampSpawner[] trailStampSpawners;

    public bool IsGameOver { get; private set; } = false;
    public bool IsCleared { get; private set; } = false;

    [SerializeField] private InputActionReference restartAction;

    ResultPanelUI resultPanel;
    Vector3 playerStartPosition;
    Quaternion playerStartRotation;
    bool playerStartCaptured = false;
    float playSessionStartTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartNewSession();
            CachePlayerReferences(true);
            CacheTrailStampSpawners(true);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    void OnEnable()
    {
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.Enable();
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.Disable();
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Note: input is polled from `restartAction` in Update()

    void Update()
    {
        if (!IsGameOver && IsInGameScene())
        {
            score += scoreRate * Time.deltaTime;
        }

        // Poll the Input System restart action (if assigned)
        if (restartAction != null && restartAction.action != null && restartAction.action.triggered)
        {
            Restart();
        }
    }
    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsCleared = false;
        Time.timeScale = 0f;
        float elapsedTime = GetSessionElapsedTime();

        Debug.Log("GAME OVER - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.GameOver, elapsedTime);
    }

    public void LevelClear()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsCleared = true;
        Time.timeScale = 0f;
        float elapsedTime = GetSessionElapsedTime();

        Debug.Log("CLEAR - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.Clear, elapsedTime);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        ResetCoreState();
        HideResultPanel();
        StartNewSession();
        ResetGameplayState();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ResetCoreState();
        HideResultPanel();
        player = null;
        playerController = null;
        playerStartCaptured = false;
        trailStampSpawners = null;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RegisterResultPanel(ResultPanelUI panel)
    {
        resultPanel = panel;
    }

    public void UnregisterResultPanel(ResultPanelUI panel)
    {
        if (resultPanel == panel)
            resultPanel = null;
    }

    ResultPanelUI GetOrFindResultPanel()
    {
        if (resultPanel == null)
            resultPanel = FindObjectOfType<ResultPanelUI>(true);
        return resultPanel;
    }

    void ShowResultPanel(ResultPanelUI.ResultState state, float elapsedTime)
    {
        var panel = GetOrFindResultPanel();
        if (panel != null)
            panel.Show( elapsedTime, state);
    }

    void HideResultPanel()
    {
        var panel = GetOrFindResultPanel();
        if (panel != null)
            panel.HideImmediate();
    }

    void ResetCoreState()
    {
        score = 0f;
        IsGameOver = false;
        IsCleared = false;
    }

    void ResetGameplayState()
    {
        CachePlayerReferences(!playerStartCaptured);
        CacheTrailStampSpawners(false);

        if (trailStampSpawners != null)
        {
            foreach (var spawner in trailStampSpawners)
            {
                if (spawner != null)
                    spawner.ClearTrail();
            }
        }

        if (!playerStartCaptured)
            return;

        if (playerController != null)
        {
            playerController.ResetControllerState(playerStartPosition, playerStartRotation);
        }
        else if (player != null)
        {
            Transform target = player.transform;
            target.SetPositionAndRotation(playerStartPosition, playerStartRotation);

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = playerStartPosition;
                rb.rotation = playerStartRotation;
            }
        }

        if (player != null)
        {
            player.ResetPlayerData();
        }

    }

    void CachePlayerReferences(bool refreshStartValues)
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        if (player != null && playerController == null)
            playerController = player.GetComponent<PlayerController>();

        if (player != null && (refreshStartValues || !playerStartCaptured))
        {
            Transform target = player.transform;
            playerStartPosition = target.position;
            playerStartRotation = target.rotation;
            playerStartCaptured = true;
        }
    }

    void CacheTrailStampSpawners(bool forceRefresh)
    {
        if (forceRefresh || trailStampSpawners == null || trailStampSpawners.Length == 0)
        {
            trailStampSpawners = FindObjectsOfType<TrailStampSpawner>();
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool loadedGameScene = scene.name == gameSceneName;
        if (loadedGameScene)
        {
            Time.timeScale = 1f;
            ResetCoreState();
            HideResultPanel();
            player = null;
            playerController = null;
            playerStartCaptured = false;
            CachePlayerReferences(true);
            CacheTrailStampSpawners(true);
            StartNewSession();
        }
        else
        {
            player = null;
            playerController = null;
            playerStartCaptured = false;
            trailStampSpawners = null;
        }
    }

    public bool IsPlaying()
    {
        return !IsGameOver && IsInGameScene();
    }

    bool IsInGameScene()
    {
        return SceneManager.GetActiveScene().name == gameSceneName;
    }

    void StartNewSession()
    {
        playSessionStartTime = Time.time;
    }

    float GetSessionElapsedTime()
    {
        return Mathf.Max(0f, Time.time - playSessionStartTime);
    }
}
