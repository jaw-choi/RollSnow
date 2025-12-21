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

    public bool IsGameOver { get; private set; } = false;
    public bool IsCleared { get; private set; } = false;

    [SerializeField] private InputActionReference restartAction;

    ResultPanelUI resultPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            Instance = null;
        }
    }

    void OnEnable()
    {
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.Disable();
        }
    }

    // Note: input is polled from `restartAction` in Update()

    void Update()
    {
        if (!IsGameOver)
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
        float elapsedTime = Time.timeSinceLevelLoad;

        Debug.Log("GAME OVER - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.GameOver, elapsedTime);
    }

    public void LevelClear()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsCleared = true;
        Time.timeScale = 0f;
        float elapsedTime = Time.timeSinceLevelLoad;

        Debug.Log("CLEAR - Score: " + Mathf.FloorToInt(score));
        ShowResultPanel(ResultPanelUI.ResultState.Clear, elapsedTime);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        score = 0f;
        IsGameOver = false;
        IsCleared = false;
        HideResultPanel();
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        score = 0f;
        IsGameOver = false;
        IsCleared = false;
        HideResultPanel();
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
            panel.Show(score, elapsedTime, state);
    }

    void HideResultPanel()
    {
        var panel = GetOrFindResultPanel();
        if (panel != null)
            panel.HideImmediate();
    }
}
