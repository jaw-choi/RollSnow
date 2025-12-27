using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Current score (in seconds * scoreRate)")]
    public float score = 0f;
    [Tooltip("Points awarded per second while playing")]
    public float scoreRate = 1f;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "04_GameScene";
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";

    public bool IsGameOver { get; private set; } = false;
    public bool IsCleared { get; private set; } = false;

    ResultPanelUI resultPanel;
    [SerializeField] private Player player;
    [SerializeField] private ScoreBasedCameraFollow cameraFollow;
    [SerializeField] private Camera gameplayCamera;
    Vector3 playerStartPosition;
    Quaternion playerStartRotation;
    bool playerStartCaptured = false;
    Vector3 cameraStartPosition;
    Quaternion cameraStartRotation;
    bool cameraStartCaptured = false;
    float playSessionStartTime;
    bool isGameplayActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
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

    void Update()
    {
        if (!IsGameOver && isGameplayActive)
        {
            score += scoreRate * Time.deltaTime;
        }
    }

    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsCleared = false;
        isGameplayActive = false;
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
        isGameplayActive = false;
        Time.timeScale = 0f;
        float elapsedTime = GetSessionElapsedTime();

        Debug.Log("CLEAR - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.Clear, elapsedTime);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        ResetCoreState();
        HideResultPanel();
        isGameplayActive = false;

        if (!IsInGameScene())
        {
            player = null;
            playerStartCaptured = false;
            cameraFollow = null;
            gameplayCamera = null;
            cameraStartCaptured = false;
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        ResetPlayerState();
        StartNewSession();
        isGameplayActive = true;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        ResetCoreState();
        HideResultPanel();
        isGameplayActive = false;
        player = null;
        playerStartCaptured = false;
        cameraFollow = null;
        gameplayCamera = null;
        cameraStartCaptured = false;
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
            panel.Show(elapsedTime, state);
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

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ResetCoreState();
            HideResultPanel();
            playerStartCaptured = false;
            cameraStartCaptured = false;
            cameraFollow = null;
            gameplayCamera = null;
            CachePlayerReferences(true);
            CacheCameraReferences(true);
            ResetPlayerState();
            ResetCameraState();
            StartNewSession();
            isGameplayActive = true;
        }
        else
        {
            player = null;
            playerStartCaptured = false;
            cameraFollow = null;
            gameplayCamera = null;
            cameraStartCaptured = false;
            isGameplayActive = false;
        }
    }

    public bool IsPlaying()
    {
        return !IsGameOver && isGameplayActive;
    }

    bool IsInGameScene()
    {
        return SceneManager.GetActiveScene().name == gameSceneName;
    }

    void CachePlayerReferences(bool refreshStartValues)
    {
        if (player == null || player.Equals(null))
        {
            player = FindObjectOfType<Player>(true);
        }

        if (player != null && (refreshStartValues || !playerStartCaptured))
        {
            playerStartPosition = player.transform.position;
            playerStartRotation = player.transform.rotation;
            playerStartCaptured = true;
        }
    }

    public void RegisterPlayer(Player target)
    {
        player = target;
        playerStartCaptured = false;
        CachePlayerReferences(true);
    }

    public void UnregisterPlayer(Player target)
    {
        if (player == target)
        {
            player = null;
            playerStartCaptured = false;
        }
    }

    void ResetPlayerState()
    {
        CachePlayerReferences(false);
        if (!playerStartCaptured || player == null || player.Equals(null))
            return;

        var target = player.transform;
        target.SetPositionAndRotation(playerStartPosition, playerStartRotation);

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = playerStartPosition;
            rb.rotation = playerStartRotation;
        }

        player.ResetPlayerData();
        ResetCameraState();
    }

    void ResetCameraState()
    {
        CacheCameraReferences(false);

        if (cameraFollow != null && !cameraFollow.Equals(null))
        {
            cameraFollow.SnapToPlayerImmediately();
            return;
        }

        if (!cameraStartCaptured || gameplayCamera == null || gameplayCamera.Equals(null))
            return;

        gameplayCamera.transform.SetPositionAndRotation(cameraStartPosition, cameraStartRotation);
    }

    void CacheCameraReferences(bool refreshStartValues)
    {
        if (cameraFollow == null || cameraFollow.Equals(null))
        {
            cameraFollow = FindObjectOfType<ScoreBasedCameraFollow>(true);
        }

        if ((gameplayCamera == null || gameplayCamera.Equals(null)) && cameraFollow != null && !cameraFollow.Equals(null))
        {
            gameplayCamera = cameraFollow.GetComponent<Camera>();
        }

        if (gameplayCamera == null || gameplayCamera.Equals(null))
        {
            var mainCam = Camera.main;
            if (mainCam != null && !mainCam.Equals(null))
            {
                gameplayCamera = mainCam;
            }
        }

        if (gameplayCamera != null && !gameplayCamera.Equals(null) && (refreshStartValues || !cameraStartCaptured))
        {
            var camTransform = gameplayCamera.transform;
            cameraStartPosition = camTransform.position;
            cameraStartRotation = camTransform.rotation;
            cameraStartCaptured = true;
        }
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
