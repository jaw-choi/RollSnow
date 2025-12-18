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

    public bool IsGameOver { get; private set; } = false;

    [SerializeField] private InputActionReference restartAction;

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
        Time.timeScale = 0f;
        Debug.Log("GAME OVER - Score: " + Mathf.FloorToInt(score));
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        score = 0f;
        IsGameOver = false;
        SceneManager.LoadScene("GameScene");
    }
}
