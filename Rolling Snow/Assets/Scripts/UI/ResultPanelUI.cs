using System.Collections;
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
    [SerializeField] private TextMeshProUGUI speedLabel;
    [SerializeField] private TextMeshProUGUI sizeLabel;
    [SerializeField] private TextMeshProUGUI baseScoreLabel;
    [SerializeField] private TextMeshProUGUI finalScoreLabel;
    [SerializeField] private TextMeshProUGUI baseGoldLabel;
    [SerializeField] private TextMeshProUGUI finalGoldLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject noHeartsAlertRoot;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";
    [SerializeField] private string gameSceneName = "04_GameScene";

    [Header("Titles")]
    [SerializeField] private string gameOverTitle = "Game Over!";
    [SerializeField] private string clearTitle = "Stage Clear!";

    [Header("Formats")]
    [SerializeField] private string speedFormat = "Speed : {0:F1}";
    [SerializeField] private string sizeFormat = "Size : {0:F2}";
    [SerializeField] private string baseScoreFormat = "Base Score : {0}";
    [SerializeField] private string finalScoreFormat = "Final Score : {0}";
    [SerializeField] private string baseGoldFormat = "Base Gold : {0}";
    [SerializeField] private string finalGoldFormat = "Final Gold : {0}";
    [SerializeField] private bool animateFinalResults = true;
    [SerializeField] private float resultsHoldSeconds = 0.6f;
    [SerializeField] private float resultsAnimateSeconds = 0.9f;
    [SerializeField] private bool useUnscaledTime = true;

    Coroutine resultRoutine;

    public enum ResultState { GameOver, Clear }

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        RegisterWithGameManager();
        HookButtons(true);
        HideImmediate();
    }

    void OnEnable()
    {
        RegisterWithGameManager();
    }

    void OnDestroy()
    {
        UnregisterFromGameManager();
        HookButtons(false);
    }

    void RegisterWithGameManager()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterResultPanel(this);
    }

    void UnregisterFromGameManager()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterResultPanel(this);
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

    public void Show(float elapsedTime, ResultState state)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        SetNoHeartsAlert(false);

        if (titleLabel != null)
            titleLabel.text = state == ResultState.Clear ? clearTitle : gameOverTitle;
        if (animateFinalResults && GameManager.Instance != null)
        {
            StopResultRoutine();
            resultRoutine = StartCoroutine(AnimateFinalResults());
        }
        else
        {
            ApplyRunResults(elapsedTime);
        }
        if (timeLabel != null)
            timeLabel.text = $"Time : {FormatTime(elapsedTime)}";
    }

    public void HideImmediate()
    {
        StopResultRoutine();
        if (panelRoot != null && panelRoot.activeSelf)
            panelRoot.SetActive(false);
    }

    public void SetNoHeartsAlert(bool show)
    {
        if (noHeartsAlertRoot != null)
            noHeartsAlertRoot.SetActive(show);
    }

    static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    void ApplyRunResults(float elapsedTime)
    {
        if (GameManager.Instance == null)
        {
            int displayScore = Mathf.FloorToInt(elapsedTime) * 5;
            if (scoreLabel != null)
                scoreLabel.text = $"{displayScore}";
            if (finalScoreLabel != null)
                finalScoreLabel.text = string.Format(finalScoreFormat, displayScore);
            return;
        }

        var results = GameManager.Instance.GetLastRunResults();
        if (scoreLabel != null)
            scoreLabel.text = $"{results.FinalScore}";
        if (speedLabel != null)
            speedLabel.text = string.Format(speedFormat, results.Speed);
        if (sizeLabel != null)
            sizeLabel.text = string.Format(sizeFormat, results.Size);
        if (baseScoreLabel != null)
            baseScoreLabel.text = string.Format(baseScoreFormat, results.BaseScore);
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(finalScoreFormat, results.FinalScore);
        if (baseGoldLabel != null)
            baseGoldLabel.text = string.Format(baseGoldFormat, results.BaseGold);
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(finalGoldFormat, results.FinalGold);
    }

    void OnRestart()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    void OnMainMenu()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    void StopResultRoutine()
    {
        if (resultRoutine == null)
            return;

        StopCoroutine(resultRoutine);
        resultRoutine = null;
    }

    IEnumerator AnimateFinalResults()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            yield break;

        var results = gm.GetLastRunResults();
        ApplyBaseResults(results);

        float hold = Mathf.Max(0f, resultsHoldSeconds);
        if (hold > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(hold);
            else
                yield return new WaitForSeconds(hold);
        }

        float duration = Mathf.Max(0.01f, resultsAnimateSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            float speed = Mathf.Lerp(results.Speed, 0f, t);
            float size = Mathf.Lerp(results.Size, 0f, t);
            int finalScore = Mathf.RoundToInt(Mathf.Lerp(results.BaseScore, results.FinalScore, t));
            int finalGold = Mathf.RoundToInt(Mathf.Lerp(results.BaseGold, results.FinalGold, t));

            ApplyAnimatedResults(speed, size, finalScore, finalGold);
            yield return null;
        }

        ApplyAnimatedResults(0f, 0f, results.FinalScore, results.FinalGold);
        resultRoutine = null;
    }

    void ApplyBaseResults(GameManager.RunResults results)
    {
        if (speedLabel != null)
            speedLabel.text = string.Format(speedFormat, results.Speed);
        if (sizeLabel != null)
            sizeLabel.text = string.Format(sizeFormat, results.Size);
        if (baseScoreLabel != null)
            baseScoreLabel.text = string.Format(baseScoreFormat, results.BaseScore);
        if (baseGoldLabel != null)
            baseGoldLabel.text = string.Format(baseGoldFormat, results.BaseGold);

        int initialScore = results.BaseScore;
        int initialGold = results.BaseGold;
        if (scoreLabel != null)
            scoreLabel.text = $"{initialScore}";
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(finalScoreFormat, initialScore);
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(finalGoldFormat, initialGold);
    }

    void ApplyAnimatedResults(float speed, float size, int finalScore, int finalGold)
    {
        if (speedLabel != null)
            speedLabel.text = string.Format(speedFormat, speed);
        if (sizeLabel != null)
            sizeLabel.text = string.Format(sizeFormat, size);
        if (scoreLabel != null)
            scoreLabel.text = $"{finalScore}";
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(finalScoreFormat, finalScore);
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(finalGoldFormat, finalGold);
    }
}
