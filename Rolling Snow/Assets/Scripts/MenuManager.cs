using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";
    [SerializeField] private string gameSceneName = "04_GameScene";

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private bool pauseOnStart = false;

    public bool IsPaused { get; private set; }

    void Start()
    {
        if (pauseOnStart)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    public void LoadMainMenu()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        Resume();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void LoadGameScene()
    {
        Resume();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else
        {
            var system = HeartSystem.GetOrCreate();
            if (system != null && !system.TryConsumeHeart())
            {
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
                return;
            }

            AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void RestartGame()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        Resume();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused && Time.timeScale > 0f)
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            return;
        }
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void TogglePause()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        if (IsPaused) Resume();
        else Pause();
    }

    public void QuitGame()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        Application.Quit();
    }
}
