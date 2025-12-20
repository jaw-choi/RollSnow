using UnityEngine;

[DisallowMultipleComponent]
public class LogoBreather : MonoBehaviour
{
    [Header("Scale Pulse")]
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float scaleAmplitude = 0.05f;
    [SerializeField, Min(0.1f)] private float cycleDuration = 2f;

    [Header("Optional Alpha Pulse")]
    [SerializeField] private bool pulseAlpha = true;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.85f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [SerializeField] private CanvasGroup canvasGroup;

    private Vector3 baseScale;
    private float angularFrequency;

    void Awake()
    {
        if (target == null) target = transform;
        baseScale = target.localScale;
        angularFrequency = Mathf.PI * 2f / Mathf.Max(0.0001f, cycleDuration);

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp(maxAlpha, minAlpha, 1f);
    }

    void Update()
    {
        float normalized = Mathf.Sin(Time.unscaledTime * angularFrequency) * 0.5f + 0.5f;
        float scaleFactor = 1f + (normalized - 0.5f) * 2f * scaleAmplitude;
        target.localScale = baseScale * scaleFactor;

        if (pulseAlpha && canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, normalized);
        }
    }
}
