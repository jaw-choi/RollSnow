using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TapToStartOverlay : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private string fallbackSceneName = "04_GameScene";
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private bool disableOverlayOnTap = true;

    private bool hasStarted;

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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasStarted)
        {
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
            SceneManager.LoadScene(fallbackSceneName);
        }
    }
}
