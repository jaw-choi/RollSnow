using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Current score (in seconds * scoreRate)")]
    public float score = 0f;
    [Tooltip("Points awarded per second while playing")]
    public float scoreRate = 1f;

    public bool IsGameOver { get; private set; } = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private InputActionReference restartAction;
#endif

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
        }
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.performed += OnRestartPerformed;
            restartAction.action.Enable();
        }
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (restartAction != null && restartAction.action != null)
        {
            restartAction.action.performed -= OnRestartPerformed;
            restartAction.action.Disable();
        }
#endif
    }

    void Update()
    {
        if (!IsGameOver)
        {
            score += scoreRate * Time.deltaTime;
        }

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        // Fallback for legacy input if project still uses it
        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private void OnRestartPerformed(InputAction.CallbackContext ctx)
    {
        Restart();
    }
#endif

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
