using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoHeartsRestartMessage : MonoBehaviour
{
    [SerializeField] private Button restartButton;

    [Header("No Heart UI")]
    [SerializeField] private GameObject noHeartsRoot;
    [SerializeField] private TextMeshProUGUI noHeartsLabel;
    [SerializeField] private string noHeartsMessage = "No Hearts";
    [SerializeField] private float noHeartsMessageDuration = 1.2f;
    [SerializeField] private float noHeartsMoveUp = 40f;
    [SerializeField] private CanvasGroup noHeartsCanvasGroup;
    [SerializeField] private RectTransform noHeartsRect;

    bool isNoHeartsMessageActive;
    Coroutine noHeartsRoutine;
    Vector2 noHeartsStartPos;
    bool hasCachedNoHeartsPos;

    void Awake()
    {
        if (restartButton == null)
            restartButton = GetComponent<Button>();

        CacheNoHeartsTargets();
        RefreshNoHeartsUI();
    }

    void OnEnable()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(HandleRestartClicked);

        var system = HeartSystem.GetOrCreate();
        if (system != null)
            system.HeartsChanged += HandleHeartsChanged;

        RefreshNoHeartsUI();
    }

    void OnDisable()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(HandleRestartClicked);

        if (HeartSystem.Instance != null)
            HeartSystem.Instance.HeartsChanged -= HandleHeartsChanged;

        StopNoHeartsRoutine();
    }

    void HandleRestartClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        if (HasAvailableHearts())
            return;

        ShowNoHeartsMessage();
    }

    void HandleHeartsChanged(HeartSystem.HeartStatus status)
    {
        RefreshNoHeartsUI();
    }

    bool HasAvailableHearts()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return true;

        return system.GetStatus().Current > 0;
    }

    void RefreshNoHeartsUI()
    {
        if (HasAvailableHearts())
        {
            StopNoHeartsRoutine();
            ShowNoHeartsUI(false);
            return;
        }

        if (!isNoHeartsMessageActive)
            ShowNoHeartsUI(false);
    }

    void ShowNoHeartsMessage()
    {
        if (noHeartsRoot == null && noHeartsLabel == null)
            return;

        CacheNoHeartsTargets();
        StopNoHeartsRoutine();
        isNoHeartsMessageActive = true;
        ShowNoHeartsUI(true);
        noHeartsRoutine = StartCoroutine(AnimateNoHeartsMessage());
    }

    void ShowNoHeartsUI(bool show)
    {
        if (noHeartsRoot != null)
            noHeartsRoot.SetActive(show);

        if (show && noHeartsLabel != null)
            noHeartsLabel.text = noHeartsMessage;
    }

    IEnumerator AnimateNoHeartsMessage()
    {
        float duration = Mathf.Max(0.1f, noHeartsMessageDuration);
        float elapsed = 0f;

        if (noHeartsCanvasGroup != null)
            noHeartsCanvasGroup.alpha = 1f;

        if (noHeartsRect != null)
            noHeartsRect.anchoredPosition = noHeartsStartPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (noHeartsCanvasGroup != null)
                noHeartsCanvasGroup.alpha = 1f - t;

            if (noHeartsRect != null)
            {
                var offset = new Vector2(0f, Mathf.Lerp(0f, noHeartsMoveUp, t));
                noHeartsRect.anchoredPosition = noHeartsStartPos + offset;
            }

            yield return null;
        }

        isNoHeartsMessageActive = false;
        if (noHeartsRect != null)
            noHeartsRect.anchoredPosition = noHeartsStartPos;
        ShowNoHeartsUI(false);
        noHeartsRoutine = null;
    }

    void StopNoHeartsRoutine()
    {
        if (noHeartsRoutine == null)
            return;

        StopCoroutine(noHeartsRoutine);
        noHeartsRoutine = null;
        isNoHeartsMessageActive = false;
    }

    void CacheNoHeartsTargets()
    {
        if (noHeartsRoot != null)
        {
            if (noHeartsRect == null)
                noHeartsRect = noHeartsRoot.GetComponent<RectTransform>();
            if (noHeartsCanvasGroup == null)
                noHeartsCanvasGroup = noHeartsRoot.GetComponent<CanvasGroup>();
        }

        if (noHeartsCanvasGroup == null && noHeartsLabel != null)
            noHeartsCanvasGroup = noHeartsLabel.GetComponent<CanvasGroup>();

        if (noHeartsCanvasGroup == null && noHeartsRoot != null)
            noHeartsCanvasGroup = noHeartsRoot.AddComponent<CanvasGroup>();

        if (noHeartsRect == null && noHeartsLabel != null)
            noHeartsRect = noHeartsLabel.rectTransform;

        if (noHeartsRect != null && !hasCachedNoHeartsPos)
        {
            noHeartsStartPos = noHeartsRect.anchoredPosition;
            hasCachedNoHeartsPos = true;
        }
    }
}
