using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class TapToStartOverlay : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private string fallbackSceneName = "04_GameScene";
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private bool disableOverlayOnTap = true;
    [Header("No Heart UI")]
    [SerializeField] private GameObject noHeartsRoot;
    [SerializeField] private TextMeshProUGUI noHeartsLabel;
    [SerializeField] private string noHeartsMessage = "No Hearts";
    [SerializeField] private float noHeartsMessageDuration = 1.2f;
    [SerializeField] private float noHeartsMoveUp = 40f;
    [SerializeField] private CanvasGroup noHeartsCanvasGroup;
    [SerializeField] private RectTransform noHeartsRect;

    private bool hasStarted;
    private bool isNoHeartsMessageActive;
    private Coroutine noHeartsRoutine;
    private Vector2 noHeartsStartPos;
    private bool hasCachedNoHeartsPos;

    private void Awake()
    {
        if (overlayCanvasGroup == null)
        {
            overlayCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.blocksRaycasts = true;
        }

        CacheNoHeartsTargets();
        RefreshNoHeartsUI();
    }

    private void OnEnable()
    {
        var system = HeartSystem.GetOrCreate();
        if (system != null)
            system.HeartsChanged += HandleHeartsChanged;
        RefreshNoHeartsUI();
    }

    private void OnDisable()
    {
        if (HeartSystem.Instance != null)
            HeartSystem.Instance.HeartsChanged -= HandleHeartsChanged;

        StopNoHeartsRoutine();
    }

    private void HandleHeartsChanged(HeartSystem.HeartStatus status)
    {
        RefreshNoHeartsUI();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasStarted)
        {
            return;
        }

        if (!HasAvailableHearts())
        {
            AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
            ShowNoHeartsMessage();
            return;
        }

        hasStarted = true;

        if (disableOverlayOnTap && overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
            overlayCanvasGroup.interactable = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else if (!string.IsNullOrEmpty(fallbackSceneName))
        {
            var system = HeartSystem.GetOrCreate();
            if (system != null && !system.TryConsumeHeart())
            {
                hasStarted = false;
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
                ShowNoHeartsMessage();
                return;
            }

            SceneManager.LoadScene(fallbackSceneName);
        }
    }

    private bool HasAvailableHearts()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return true;

        return system.GetStatus().Current > 0;
    }

    private void RefreshNoHeartsUI()
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

    private void ShowNoHeartsMessage()
    {
        if (noHeartsRoot == null && noHeartsLabel == null)
            return;

        CacheNoHeartsTargets();
        StopNoHeartsRoutine();
        isNoHeartsMessageActive = true;
        ShowNoHeartsUI(true);
        noHeartsRoutine = StartCoroutine(AnimateNoHeartsMessage());
    }

    private void ShowNoHeartsUI(bool show)
    {
        if (noHeartsRoot != null)
            noHeartsRoot.SetActive(show);

        if (show && noHeartsLabel != null)
            noHeartsLabel.text = noHeartsMessage;
    }

    private IEnumerator AnimateNoHeartsMessage()
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

    private void StopNoHeartsRoutine()
    {
        if (noHeartsRoutine == null)
            return;

        StopCoroutine(noHeartsRoutine);
        noHeartsRoutine = null;
        isNoHeartsMessageActive = false;
    }

    private void CacheNoHeartsTargets()
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
