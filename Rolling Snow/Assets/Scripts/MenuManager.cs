using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "GameScene";

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
        Resume();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadGameScene()
    {
        Resume();
        SceneManager.LoadScene(gameSceneName);
    }

    public void RestartGame()
    {
        Resume();
        SceneManager.LoadScene(gameSceneName);
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
        if (IsPaused) Resume();
        else Pause();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
