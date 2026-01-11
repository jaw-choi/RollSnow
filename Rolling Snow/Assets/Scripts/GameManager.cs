using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Current score (in seconds * scoreRate)")]
    public float score = 0f;
    [Tooltip("Points awarded per second while playing")]
    public float scoreRate = 1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private string scoreTextObjectName = "Score";

    [Header("Scoring")]
    [SerializeField] private int scoreDisplayMultiplier = 5;
    [SerializeField] private int scoreToGoldMultiplier = 5;
    [SerializeField] private float finalScoreMultiplier = 1f;
    [SerializeField] private float speedScoreBonus = 0f;
    [SerializeField] private float sizeScoreBonus = 0f;
    [SerializeField] private float finalGoldMultiplier = 1f;
    [SerializeField] private float speedGoldBonus = 0f;
    [SerializeField] private float sizeGoldBonus = 0f;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "04_GameScene";
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";

    public bool IsGameOver { get; private set; } = false;
    public bool IsCleared { get; private set; } = false;

    ResultPanelUI resultPanel;
    [SerializeField] private Player player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ScoreBasedCameraFollow cameraFollow;
    [SerializeField] private Camera gameplayCamera;
    GameSceneManager gameSceneManager;
    Vector3 playerStartPosition;
    Quaternion playerStartRotation;
    bool playerStartCaptured = false;
    Vector3 cameraStartPosition;
    Quaternion cameraStartRotation;
    bool cameraStartCaptured = false;
    float playSessionStartTime;
    bool isGameplayActive = false;
    bool gameOverEffectsPlayed = false;
    bool goldAwardedForRun = false;
    int runGoldStart = 0;
    bool runResultsCaptured = false;
    RunResults lastRunResults;

    public struct RunResults
    {
        public int BaseScore;
        public int FinalScore;
        public float BaseGold;
        public float FinalGold;
        public float FinalGoldEarned;
        public float RunGoldEarned;
        public float Speed;
        public float Size;
        public float Distance;
    }

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
            if (scoreLabel == null || scoreLabel.Equals(null))
                CacheScoreLabel();
            UpdateScoreLabel(score);
        }
    }

    public void GameOver()
    {
        if (IsGameOver) return;

        PlayGameOverEffects();
        AwardGoldForRun();
        IsGameOver = true;
        IsCleared = false;
        isGameplayActive = false;
        Time.timeScale = 0f;
        float elapsedTime = GetSessionElapsedTime();

        Debug.Log("GAME OVER - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.GameOver, elapsedTime);
    }

    public void BeginGameOver()
    {
        if (IsGameOver) return;

        isGameplayActive = false;
    }

    public void PlayGameOverEffects()
    {
        if (gameOverEffectsPlayed)
            return;

        gameOverEffectsPlayed = true;
        var settings = SettingsManager.Instance;
        if (settings != null && settings.Haptics)
            Haptics.Tap(0.2f);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(false);
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
        }
    }

    public void LevelClear()
    {
        if (IsGameOver) return;

        AwardGoldForRun();
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
        if (!TryConsumeHeartForRun())
        {
            AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
            return;
        }

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
        ResetWorldState();
        StartNewSession();
        isGameplayActive = true;
        AudioManager.instance?.PlayBGM(true);
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
        {
            panel.Show(elapsedTime, state);
            panel.SetNoHeartsAlert(IsOutOfHearts());
        }
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
        gameOverEffectsPlayed = false;
        goldAwardedForRun = false;
        runResultsCaptured = false;
        UpdateScoreLabel(score);
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
            CacheSceneManagerReferences();
            CachePlayerReferences(true);
            CacheCameraReferences(true);
            CacheScoreLabel();
            ResetPlayerState();
            ResetCameraState();
            StartNewSession();
            isGameplayActive = true;
            AudioManager.instance.PlayBGM(true);
            score = 0f;
            UpdateScoreLabel(score);
        }
        else
        {
            player = null;
            playerStartCaptured = false;
            cameraFollow = null;
            gameplayCamera = null;
            cameraStartCaptured = false;
            scoreLabel = null;
            gameSceneManager = null;
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

        if ((playerController == null || playerController.Equals(null)) && player != null && !player.Equals(null))
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.GetComponentInChildren<PlayerController>(true);
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
        if (playerController == null || playerController.Equals(null))
            playerController = target != null ? target.GetComponent<PlayerController>() : null;
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

    void CacheSceneManagerReferences()
    {
        if (gameSceneManager == null || gameSceneManager.Equals(null))
        {
            gameSceneManager = FindObjectOfType<GameSceneManager>(true);
        }
    }

    void ResetWorldState()
    {
        CacheSceneManagerReferences();
        if (gameSceneManager == null || gameSceneManager.Equals(null))
            return;

        var scroller = gameSceneManager.WorldScroller;
        if (scroller != null && !scroller.Equals(null))
            scroller.ResetWorld();
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

    void CacheScoreLabel()
    {
        if (scoreLabel != null && !scoreLabel.Equals(null))
            return;
        if (!IsInGameScene())
            return;

        TextMeshProUGUI found = null;
        var scoreObject = GameObject.Find(scoreTextObjectName);
        if (scoreObject != null)
        {
            found = scoreObject.GetComponent<TextMeshProUGUI>();
        }

        if (found == null)
        {
            var labels = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label != null && label.name == scoreTextObjectName)
                {
                    found = label;
                    break;
                }
            }
        }

        if (found != null)
            scoreLabel = found;
    }

    void UpdateScoreLabel(float score)
    {
        if (scoreLabel == null || scoreLabel.Equals(null))
            return;

        int displayScore = GetDisplayScore();
        scoreLabel.text = displayScore.ToString();
    }

    public void AddScore(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
            return;

        score = Mathf.Max(0f, score + delta);
        if (scoreLabel == null || scoreLabel.Equals(null))
            CacheScoreLabel();
        UpdateScoreLabel(score);
    }

    void StartNewSession()
    {
        playSessionStartTime = Time.time;
        runGoldStart = GetCurrentGold();
        runResultsCaptured = false;
    }

    void AwardGoldForRun()
    {
        if (goldAwardedForRun)
            return;

        goldAwardedForRun = true;
        CaptureRunResults();

        float award = Mathf.Max(0f, lastRunResults.FinalGoldEarned - lastRunResults.RunGoldEarned);
        if (award > 0f)
        {
            var gold = GoldSystem.GetOrCreate();
            if (gold != null)
                gold.AddGold((int)award);
        }
    }

    bool TryConsumeHeartForRun()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return true;

        return system.TryConsumeHeart();
    }

    bool IsOutOfHearts()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return false;

        return system.GetStatus().Current <= 0;
    }

    float GetSessionElapsedTime()
    {
        return Mathf.Max(0f, Time.time - playSessionStartTime);
    }

    public int GetDisplayScore()
    {
        float distance = GetDistanceDescended();
        return Mathf.FloorToInt(distance) * Mathf.Max(1, scoreDisplayMultiplier);
    }

    public int GetRunGoldEarned()
    {
        return Mathf.Max(0, GetCurrentGold() - runGoldStart);
    }

    public float GetCurrentSpeed()
    {
        if (playerController == null || playerController.Equals(null))
            CachePlayerReferences(false);
        if (playerController == null || playerController.Equals(null))
            return 0f;

        return playerController.CurrentSpeed;
    }

    public float GetCurrentSize()
    {
        if (player == null || player.Equals(null))
            CachePlayerReferences(false);
        if (player == null || player.Equals(null))
            return 0f;

        return player.transform.localScale.x;
    }

    public float GetDistanceDescended()
    {
        if (player == null || player.Equals(null))
            CachePlayerReferences(false);
        if (player == null || player.Equals(null))
            return 0f;

        float startY = playerStartCaptured ? playerStartPosition.y : player.transform.position.y;
        float delta = startY - player.transform.position.y;
        return Mathf.Max(0f, delta);
    }

    public RunResults GetLastRunResults()
    {
        CaptureRunResults();
        return lastRunResults;
    }

    void CaptureRunResults()
    {
        if (runResultsCaptured)
            return;

        int runGoldEarned = GetRunGoldEarned();
        float distance = GetDistanceDescended();
        int baseScore = Mathf.FloorToInt(distance) * Mathf.Max(1, scoreDisplayMultiplier);
        int baseGoldFromScore = baseScore * Mathf.Max(1, scoreToGoldMultiplier);
        int baseGold = runGoldEarned;

        float speed = GetCurrentSpeed();
        float size = GetCurrentSize();

        int finalScore = Mathf.RoundToInt(baseScore * Mathf.Max(0f, finalScoreMultiplier));
        finalScore = Mathf.Max(0, finalScore);

        float baseGoldTotal = baseGold + Mathf.Max(0f, baseGoldFromScore);
        baseGoldTotal = (float)baseGoldTotal * finalGoldMultiplier;
        float finalGoldEarned = Mathf.Max(0f, finalGoldMultiplier);
        finalGoldEarned = Mathf.Max(baseGoldTotal, finalGoldEarned);
        float finalGold = runGoldStart + finalGoldEarned;
        lastRunResults = new RunResults
        {
            BaseScore = baseScore,
            FinalScore = finalScore,
            BaseGold = baseGold,
            FinalGold = finalGold,
            FinalGoldEarned = finalGoldEarned,
            RunGoldEarned = runGoldEarned,
            Speed = speed,
            Size = size,
            Distance = distance
        };

        runResultsCaptured = true;
    }

    int GetCurrentGold()
    {
        var system = GoldSystem.GetOrCreate();
        if (system == null)
            return 0;

        return system.GetGold();
    }
}
