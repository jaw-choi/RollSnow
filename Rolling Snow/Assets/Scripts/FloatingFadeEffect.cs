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
    CanvasGroup[] canvasGroups;
    TMP_Text[] tmpTexts;
    SpriteRenderer[] spriteRenderers;
    Graphic[] uiGraphics;
    Color[] tmpBaseColors;
    Color[] spriteBaseColors;
    Color[] graphicBaseColors;
    Vector3 startLocalPosition;
    Vector3 startAnchoredPosition;
    float elapsed;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);
        tmpTexts = GetComponentsInChildren<TMP_Text>(true);

        if (!string.IsNullOrEmpty(textOverride) && tmpTexts != null)
        {
            foreach (var text in tmpTexts)
                text.text = textOverride;
        }

        canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        uiGraphics = GetComponentsInChildren<Graphic>(true);

        if (tmpTexts != null && tmpTexts.Length > 0 && uiGraphics != null && uiGraphics.Length > 0)
        {
            int count = 0;
            foreach (var graphic in uiGraphics)
            {
                if (!(graphic is TMP_Text))
                    count++;
            }

            if (count != uiGraphics.Length)
            {
                var filtered = new Graphic[count];
                int index = 0;
                foreach (var graphic in uiGraphics)
                {
                    if (graphic is TMP_Text)
                        continue;
                    filtered[index++] = graphic;
                }
                uiGraphics = filtered;
            }
        }

        if (tmpTexts != null)
        {
            tmpBaseColors = new Color[tmpTexts.Length];
            for (int i = 0; i < tmpTexts.Length; i++)
                tmpBaseColors[i] = tmpTexts[i].color;
        }

        if (spriteRenderers != null)
        {
            spriteBaseColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                spriteBaseColors[i] = spriteRenderers[i].color;
        }

        if (uiGraphics != null)
        {
            graphicBaseColors = new Color[uiGraphics.Length];
            for (int i = 0; i < uiGraphics.Length; i++)
                graphicBaseColors[i] = uiGraphics[i].color;
        }
    }

    void OnEnable()
    {
        ResetState();
    }

    void ResetState()
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
        if (canvasGroups != null)
        {
            for (int i = 0; i < canvasGroups.Length; i++)
                canvasGroups[i].alpha = alpha;
        }

        if (tmpTexts != null && tmpBaseColors != null)
        {
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                var c = tmpBaseColors[i];
                c.a *= alpha;
                tmpTexts[i].color = c;
            }
        }

        if (spriteRenderers != null && spriteBaseColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var c = spriteBaseColors[i];
                c.a *= alpha;
                spriteRenderers[i].color = c;
            }
        }

        if (uiGraphics != null && graphicBaseColors != null)
        {
            for (int i = 0; i < uiGraphics.Length; i++)
            {
                var c = graphicBaseColors[i];
                c.a *= alpha;
                uiGraphics[i].color = c;
            }
        }
    }

    public void SetText(string value)
    {
        textOverride = value;
        if (tmpTexts != null)
        {
            foreach (var text in tmpTexts)
                text.text = value;
        }
    }

    public void Configure(Vector3 moveOffset, float duration, bool useUnscaledTime, bool destroyOnComplete)
    {
        this.moveOffset = moveOffset;
        this.duration = duration;
        this.useUnscaledTime = useUnscaledTime;
        this.destroyOnComplete = destroyOnComplete;

        if (isActiveAndEnabled)
            ResetState();
    }
}
