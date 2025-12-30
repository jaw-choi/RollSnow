using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class LogoBreather : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float cycleDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.75f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    private CanvasGroup canvasGroup;
    private float angularFrequency;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        angularFrequency = Mathf.PI * 2f / Mathf.Max(0.0001f, cycleDuration);
        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp(maxAlpha, minAlpha, 1f);
    }

    private void Update()
    {
        float normalized = Mathf.Sin(Time.unscaledTime * angularFrequency) * 0.5f + 0.5f;
        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, normalized);
    }
}
