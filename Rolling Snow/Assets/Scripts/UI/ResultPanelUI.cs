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
    [SerializeField] private TextMeshProUGUI distanceLabel;
    [SerializeField] private TextMeshProUGUI sizeLabel;
    [SerializeField] private TextMeshProUGUI scoreItemCountLabel;
    [SerializeField] private TextMeshProUGUI baseScoreLabel;
    [SerializeField] private TextMeshProUGUI finalScoreLabel;
    [SerializeField] private TextMeshProUGUI highScoreLabel;
    [SerializeField] private TextMeshProUGUI baseGoldLabel;
    [SerializeField] private TextMeshProUGUI finalGoldLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button achievementButton;
    [SerializeField] private GameObject noHeartsAlertRoot;
    [SerializeField] private float noHeartsAlertDuration = 0.5f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";
    [SerializeField] private string gameSceneName = "04_GameScene";
    [SerializeField] private string achievementSceneName = "02_Achievement";

    [Header("Titles")]
    [SerializeField] private string gameOverTitle = "Game Over!";
    [SerializeField] private string clearTitle = "Stage Clear!";
    [SerializeField] private LocalizedString gameOverTitleLocalized;
    [SerializeField] private LocalizedString clearTitleLocalized;

    [Header("Formats")]
    [SerializeField] private string timeFormat = "Time : {0}";
    [SerializeField] private string speedFormat = "Speed : {0:F1}";
    [SerializeField] private string distanceFormat = "Distance : {0}m";
    [SerializeField] private string sizeFormat = "Size : {0:F2}";
    [SerializeField] private string scoreItemCountFormat = "Score Items : {0}";
    [SerializeField] private string baseScoreFormat = "Base Score : {0}";
    [SerializeField] private string finalScoreFormat = "Final Score : {0}";
    [SerializeField] private string highScoreFormat = "High Score : {0}";
    [SerializeField] private string baseGoldFormat = "Base Gold : {0}";
    [SerializeField] private string finalGoldFormat = "Final Gold : {0}";
    [SerializeField] private LocalizedString timeFormatLocalized;
    [SerializeField] private LocalizedString speedFormatLocalized;
    [SerializeField] private LocalizedString distanceFormatLocalized;
    [SerializeField] private LocalizedString sizeFormatLocalized;
    [SerializeField] private LocalizedString scoreItemCountFormatLocalized;
    [SerializeField] private LocalizedString baseScoreFormatLocalized;
    [SerializeField] private LocalizedString finalScoreFormatLocalized;
    [SerializeField] private LocalizedString highScoreFormatLocalized;
    [SerializeField] private LocalizedString baseGoldFormatLocalized;
    [SerializeField] private LocalizedString finalGoldFormatLocalized;
    [SerializeField] private bool animateFinalResults = true;
    [SerializeField] private float resultsHoldSeconds = 0.6f;
    [SerializeField] private float resultsAnimateSeconds = 0.9f;
    [SerializeField] private bool useUnscaledTime = true;

    Coroutine resultRoutine;
    Coroutine noHeartsAlertRoutine;

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
            if (achievementButton != null) achievementButton.onClick.RemoveListener(OnAchievement);
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

        if (achievementButton != null)
        {
            achievementButton.onClick.RemoveAllListeners();
            achievementButton.onClick.AddListener(OnAchievement);
        }
    }

    public void Show(float elapsedTime, ResultState state)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        SetNoHeartsAlert(false);

        if (titleLabel != null)
            titleLabel.text = state == ResultState.Clear ? GetClearTitle() : GetGameOverTitle();
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
            timeLabel.text = string.Format(GetTimeFormat(), FormatTime(elapsedTime));
    }

    public void HideImmediate()
    {
        StopResultRoutine();
        StopNoHeartsAlertRoutine();
        SetNoHeartsAlert(false);
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
            if (scoreItemCountLabel != null)
                scoreItemCountLabel.text = string.Format(scoreItemCountFormat, 0);
            if (highScoreLabel != null)
                highScoreLabel.text = string.Format(highScoreFormat, 0);
            return;
        }

        var results = GameManager.Instance.GetLastRunResults();
        if (scoreLabel != null)
            scoreLabel.text = $"{results.FinalScore}";
        if (speedLabel != null)
            speedLabel.text = string.Format(GetSpeedFormat(), results.Speed);
        if (distanceLabel != null)
            distanceLabel.text = string.Format(GetDistanceFormat(), Mathf.FloorToInt(results.Distance));
        if (sizeLabel != null)
            sizeLabel.text = string.Format(GetSizeFormat(), results.Size);
        if (scoreItemCountLabel != null)
            scoreItemCountLabel.text = string.Format(GetScoreItemCountFormat(), results.ScoreItemCount);
        if (baseScoreLabel != null)
            baseScoreLabel.text = string.Format(GetBaseScoreFormat(), results.BaseScore);
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(GetFinalScoreFormat(), results.FinalScore);
        if (highScoreLabel != null)
            highScoreLabel.text = string.Format(GetHighScoreFormat(), GameManager.Instance.GetHighScore());
        if (baseGoldLabel != null)
            baseGoldLabel.text = string.Format(GetBaseGoldFormat(), Mathf.RoundToInt(results.RunGoldEarned));
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(GetFinalGoldFormat(), Mathf.RoundToInt(results.FinalGoldEarned));
    }

    void OnRestart()
    {
        if (GameManager.Instance != null && !HasAvailableHearts())
        {
            AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
            ShowNoHeartsAlertBrief();
            return;
        }
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

    void OnAchievement()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(achievementSceneName);
    }

    bool HasAvailableHearts()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return true;

        return system.GetStatus().Current > 0;
    }

    void ShowNoHeartsAlertBrief()
    {
        if (noHeartsAlertRoot == null)
            return;

        StopNoHeartsAlertRoutine();
        SetNoHeartsAlert(true);
        noHeartsAlertRoutine = StartCoroutine(HideNoHeartsAlertAfterDelay());
    }

    IEnumerator HideNoHeartsAlertAfterDelay()
    {
        float duration = Mathf.Max(0.01f, noHeartsAlertDuration);
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return new WaitForSeconds(duration);

        SetNoHeartsAlert(false);
        noHeartsAlertRoutine = null;
    }

    void StopResultRoutine()
    {
        if (resultRoutine == null)
            return;

        StopCoroutine(resultRoutine);
        resultRoutine = null;
    }

    void StopNoHeartsAlertRoutine()
    {
        if (noHeartsAlertRoutine == null)
            return;

        StopCoroutine(noHeartsAlertRoutine);
        noHeartsAlertRoutine = null;
    }

    IEnumerator AnimateFinalResults()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            yield break;

        var results = gm.GetLastRunResults();
        float runGold = results.RunGoldEarned;
        float finalGold = results.FinalGoldEarned;
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

            float distance = Mathf.Lerp(0f, results.Distance, t);
            int finalScore = Mathf.RoundToInt(Mathf.Lerp(results.BaseScore, results.FinalScore, t));
            int finalGoldValue = Mathf.RoundToInt(Mathf.Lerp(runGold, finalGold, t));

            ApplyAnimatedResults(distance, finalScore, finalGoldValue);
            yield return null;
        }

        ApplyAnimatedResults(results.Distance, results.FinalScore, Mathf.RoundToInt(finalGold));
        resultRoutine = null;
    }

    void ApplyBaseResults(GameManager.RunResults results)
    {
        float runGold = results.RunGoldEarned;
        if (speedLabel != null)
            speedLabel.text = string.Format(GetSpeedFormat(), results.Speed);
        if (distanceLabel != null)
            distanceLabel.text = string.Format(GetDistanceFormat(), 0);
        if (sizeLabel != null)
            sizeLabel.text = string.Format(GetSizeFormat(), results.Size);
        if (scoreItemCountLabel != null)
            scoreItemCountLabel.text = string.Format(GetScoreItemCountFormat(), results.ScoreItemCount);
        if (baseScoreLabel != null)
            baseScoreLabel.text = string.Format(GetBaseScoreFormat(), results.BaseScore);
        if (highScoreLabel != null && GameManager.Instance != null)
            highScoreLabel.text = string.Format(GetHighScoreFormat(), GameManager.Instance.GetHighScore());
        if (baseGoldLabel != null)
            baseGoldLabel.text = string.Format(GetBaseGoldFormat(), Mathf.RoundToInt(runGold));

        int initialScore = results.BaseScore;
        if (scoreLabel != null)
            scoreLabel.text = $"{initialScore}";
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(GetFinalScoreFormat(), initialScore);
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(GetFinalGoldFormat(), Mathf.RoundToInt(runGold));
    }

    void ApplyAnimatedResults(float distance, int finalScore, int finalGold)
    {
        if (distanceLabel != null)
            distanceLabel.text = string.Format(GetDistanceFormat(), Mathf.FloorToInt(distance));
        if (scoreLabel != null)
            scoreLabel.text = $"{finalScore}";
        if (finalScoreLabel != null)
            finalScoreLabel.text = string.Format(GetFinalScoreFormat(), finalScore);
        if (finalGoldLabel != null)
            finalGoldLabel.text = string.Format(GetFinalGoldFormat(), finalGold);
    }

    string GetGameOverTitle()
    {
        return LocalizationUtility.Resolve(gameOverTitleLocalized, gameOverTitle);
    }

    string GetClearTitle()
    {
        return LocalizationUtility.Resolve(clearTitleLocalized, clearTitle);
    }

    string GetTimeFormat()
    {
        return LocalizationUtility.Resolve(timeFormatLocalized, timeFormat);
    }

    string GetSpeedFormat()
    {
        return LocalizationUtility.Resolve(speedFormatLocalized, speedFormat);
    }

    string GetDistanceFormat()
    {
        return LocalizationUtility.Resolve(distanceFormatLocalized, distanceFormat);
    }

    string GetSizeFormat()
    {
        return LocalizationUtility.Resolve(sizeFormatLocalized, sizeFormat);
    }

    string GetScoreItemCountFormat()
    {
        return LocalizationUtility.Resolve(scoreItemCountFormatLocalized, scoreItemCountFormat);
    }

    string GetBaseScoreFormat()
    {
        return LocalizationUtility.Resolve(baseScoreFormatLocalized, baseScoreFormat);
    }

    string GetFinalScoreFormat()
    {
        return LocalizationUtility.Resolve(finalScoreFormatLocalized, finalScoreFormat);
    }

    string GetHighScoreFormat()
    {
        return LocalizationUtility.Resolve(highScoreFormatLocalized, highScoreFormat);
    }

    string GetBaseGoldFormat()
    {
        return LocalizationUtility.Resolve(baseGoldFormatLocalized, baseGoldFormat);
    }

    string GetFinalGoldFormat()
    {
        return LocalizationUtility.Resolve(finalGoldFormatLocalized, finalGoldFormat);
    }
}
