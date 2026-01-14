using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingFadeEffect : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool destroyOnComplete = true;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private string textOverride;

    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    SpriteRenderer spriteRenderer;
    Graphic uiGraphic;
    Color tmpBaseColor;
    Color spriteBaseColor;
    Color graphicBaseColor;
    Vector3 startLocalPosition;
    Vector3 startAnchoredPosition;
    float elapsed;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        if (tmpText != null && !string.IsNullOrEmpty(textOverride))
            tmpText.text = textOverride;

        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (tmpText == null)
            uiGraphic = GetComponentInChildren<Graphic>(true);

        if (tmpText != null)
            tmpBaseColor = tmpText.color;
        if (spriteRenderer != null)
            spriteBaseColor = spriteRenderer.color;
        if (uiGraphic != null)
            graphicBaseColor = uiGraphic.color;
    }

    void OnEnable()
    {
        elapsed = 0f;
        if (rectTransform != null)
            startAnchoredPosition = rectTransform.anchoredPosition3D;
        else
            startLocalPosition = transform.localPosition;

        ApplyAlpha(alphaCurve != null ? alphaCurve.Evaluate(0f) : 1f);
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsed += dt;

        float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        float moveT = positionCurve != null ? positionCurve.Evaluate(t) : t;

        if (rectTransform != null)
            rectTransform.anchoredPosition3D = startAnchoredPosition + moveOffset * moveT;
        else
            transform.localPosition = startLocalPosition + moveOffset * moveT;

        float alphaT = alphaCurve != null ? alphaCurve.Evaluate(t) : (1f - t);
        ApplyAlpha(alphaT);

        if (t >= 1f && destroyOnComplete)
            Destroy(gameObject);
    }

    void ApplyAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (tmpText != null)
        {
            var c = tmpBaseColor;
            c.a *= alpha;
            tmpText.color = c;
        }

        if (spriteRenderer != null)
        {
            var c = spriteBaseColor;
            c.a *= alpha;
            spriteRenderer.color = c;
        }

        if (uiGraphic != null)
        {
            var c = graphicBaseColor;
            c.a *= alpha;
            uiGraphic.color = c;
        }
    }

    public void SetText(string value)
    {
        textOverride = value;
        if (tmpText != null)
            tmpText.text = value;
    }
}
