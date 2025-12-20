using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class LogoIntro : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
