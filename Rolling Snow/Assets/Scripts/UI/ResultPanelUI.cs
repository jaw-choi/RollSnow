using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the result panel UI that appears after the player loses.
/// </summary>
public class ResultPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Titles")]
    [SerializeField] private string gameOverTitle = "Game Over!";
    [SerializeField] private string clearTitle = "Stage Clear!";

    public enum ResultState { GameOver, Clear }

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        HookButtons(true);
        HideImmediate();
    }

    void OnDestroy()
    {
        HookButtons(false);
    }

    void HookButtons(bool on)
    {
        if (!on)
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
            return;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestart);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }
    }

    public void Show(float score, float elapsedTime, ResultState state)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (titleLabel != null)
            titleLabel.text = state == ResultState.Clear ? clearTitle : gameOverTitle;
        if (scoreLabel != null)
            scoreLabel.text = $"Score : {Mathf.FloorToInt(score)}";
        if (timeLabel != null)
            timeLabel.text = $"Time : {FormatTime(elapsedTime)}";
    }

    public void HideImmediate()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            panelRoot.SetActive(false);
    }

    static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
        else
            SceneManager.LoadScene("GameScene");
    }

    void OnMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
