using System;
using System.Collections;
using UnityEngine;

public class RandomRewardButton : MonoBehaviour
{
    public enum RewardType
    {
        Skin,
        Heart1,
        Heart3
    }

    public event Action<RewardType> Rewarded;

    [Header("Weights")]
    [SerializeField] private float skinWeight = 1f;
    [SerializeField] private float heart1Weight = 3f;
    [SerializeField] private float heart3Weight = 1f;

    [Header("Skin Reward (PlayerPrefs)")]
    [SerializeField] private string skinKey = "Reward.SkinCount";
    [SerializeField] private int skinAmount = 1;

    [Header("Hearts")]
    [SerializeField] private int heart1Amount = 1;
    [SerializeField] private int heart3Amount = 3;

    [Header("Gold Cost")]
    [SerializeField] private int goldCost = 100;
    [SerializeField] private bool logInsufficientGold = true;

    [Header("No Gold UI")]
    [SerializeField] private GameObject noGoldRoot;
    [SerializeField] private TMPro.TextMeshProUGUI noGoldLabel;
    [SerializeField] private string noGoldMessage = "Not enough gold";
    [SerializeField] private float noGoldMessageDuration = 1.2f;
    [SerializeField] private float noGoldMoveUp = 40f;
    [SerializeField] private CanvasGroup noGoldCanvasGroup;
    [SerializeField] private RectTransform noGoldRect;

    [Header("Reward Effect")]
    [SerializeField] private Transform effectTarget;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeMagnitudeX = 6f;
    [SerializeField] private float shakeMagnitudeY = 12f;
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private float popDuration = 0.15f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool rewardAfterEffect = true;
    [SerializeField] private bool ignoreWhilePlaying = true;

    [Header("Debug")]
    [SerializeField] private bool logReward = true;

    bool isAnimating;
    Coroutine effectRoutine;
    Coroutine noGoldRoutine;
    Vector2 noGoldStartPos;
    bool hasCachedNoGoldPos;

    void Awake()
    {
        CacheNoGoldTargets();
    }

    void OnDisable()
    {
        StopNoGoldRoutine();
    }

    public void OnRewardButtonClicked()
    {
        if (ignoreWhilePlaying && isAnimating)
            return;

        if (!TrySpendGoldForReward())
            return;

        if (effectTarget == null)
            effectTarget = transform;

        RewardType reward = RollReward();
        if (rewardAfterEffect)
        {
            if (effectRoutine != null)
                StopCoroutine(effectRoutine);
            effectRoutine = StartCoroutine(PlayEffectThenReward(reward));
        }
        else
        {
            ApplyReward(reward);
            Rewarded?.Invoke(reward);
            if (effectRoutine != null)
                StopCoroutine(effectRoutine);
            effectRoutine = StartCoroutine(PlayEffect(effectTarget));
        }
    }

    RewardType RollReward()
    {
        float total = Mathf.Max(0f, skinWeight) + Mathf.Max(0f, heart1Weight) + Mathf.Max(0f, heart3Weight);
        if (total <= 0f)
            return RewardType.Heart1;

        float roll = UnityEngine.Random.value * total;
        float skin = Mathf.Max(0f, skinWeight);
        float heart1 = Mathf.Max(0f, heart1Weight);

        if (roll < skin)
            return RewardType.Skin;

        roll -= skin;
        if (roll < heart1)
            return RewardType.Heart1;

        return RewardType.Heart3;
    }

    void ApplyReward(RewardType reward)
    {
        switch (reward)
        {
            case RewardType.Skin:
                GrantSkin();
                break;
            case RewardType.Heart1:
                GrantHearts(heart1Amount);
                break;
            case RewardType.Heart3:
                GrantHearts(heart3Amount);
                break;
        }

        if (logReward)
            Debug.Log($"Reward granted: {reward}");
    }

    void GrantSkin()
    {
        int current = PlayerPrefs.GetInt(skinKey, 0);
        current = Mathf.Max(0, current) + Mathf.Max(1, skinAmount);
        PlayerPrefs.SetInt(skinKey, current);
        PlayerPrefs.Save();
    }

    void GrantHearts(int amount)
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return;

        system.GrantHearts(Mathf.Max(1, amount));
    }

    bool TrySpendGoldForReward()
    {
        if (goldCost <= 0)
            return true;

        var gold = GoldSystem.GetOrCreate();
        if (gold == null)
            return false;

        bool ok = gold.TrySpendGold(goldCost);
        if (!ok)
        {
            if (logInsufficientGold)
                Debug.Log("Not enough gold for reward.");
            ShowNoGoldMessage();
            return false;
        }

        return true;
    }

    IEnumerator PlayEffectThenReward(RewardType reward)
    {
        isAnimating = true;

        if (effectTarget != null)
            yield return PlayEffect(effectTarget);

        ApplyReward(reward);
        Rewarded?.Invoke(reward);
        isAnimating = false;
    }

    IEnumerator PlayEffect(Transform target)
    {
        if (target == null)
            yield break;

        RectTransform rect = target.GetComponent<RectTransform>();
        Vector3 startLocalPos = target.localPosition;
        Vector2 startAnchoredPos = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector3 startScale = target.localScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, shakeDuration);
        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            Vector2 offset = UnityEngine.Random.insideUnitCircle;
            offset.x *= Mathf.Max(0f, shakeMagnitudeX);
            offset.y *= Mathf.Max(0f, shakeMagnitudeY);

            if (rect != null)
                rect.anchoredPosition = startAnchoredPos + offset;
            else
                target.localPosition = startLocalPos + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        if (rect != null)
            rect.anchoredPosition = startAnchoredPos;
        else
            target.localPosition = startLocalPos;

        elapsed = 0f;
        duration = Mathf.Max(0f, popDuration);
        float pop = Mathf.Max(0.01f, popScaleMultiplier);
        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float scaleT = t < 0.5f ? t / 0.5f : (t - 0.5f) / 0.5f;
            float scale = t < 0.5f ? Mathf.Lerp(1f, pop, scaleT) : Mathf.Lerp(pop, 1f, scaleT);
            target.localScale = startScale * scale;
            yield return null;
        }

        target.localScale = startScale;
        isAnimating = false;
    }

    void ShowNoGoldMessage()
    {
        if (noGoldRoot == null && noGoldLabel == null)
            return;

        CacheNoGoldTargets();
        StopNoGoldRoutine();
        ShowNoGoldUI(true);
        noGoldRoutine = StartCoroutine(AnimateNoGoldMessage());
    }

    void ShowNoGoldUI(bool show)
    {
        if (noGoldRoot != null)
            noGoldRoot.SetActive(show);

        if (show && noGoldLabel != null)
            noGoldLabel.text = noGoldMessage;
    }

    IEnumerator AnimateNoGoldMessage()
    {
        float duration = Mathf.Max(0.1f, noGoldMessageDuration);
        float elapsed = 0f;

        if (noGoldCanvasGroup != null)
            noGoldCanvasGroup.alpha = 1f;

        if (noGoldRect != null)
            noGoldRect.anchoredPosition = noGoldStartPos;

        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);

            if (noGoldCanvasGroup != null)
                noGoldCanvasGroup.alpha = 1f - t;

            if (noGoldRect != null)
            {
                var offset = new Vector2(0f, Mathf.Lerp(0f, noGoldMoveUp, t));
                noGoldRect.anchoredPosition = noGoldStartPos + offset;
            }

            yield return null;
        }

        if (noGoldRect != null)
            noGoldRect.anchoredPosition = noGoldStartPos;
        ShowNoGoldUI(false);
        noGoldRoutine = null;
    }

    void StopNoGoldRoutine()
    {
        if (noGoldRoutine == null)
            return;

        StopCoroutine(noGoldRoutine);
        noGoldRoutine = null;
    }

    void CacheNoGoldTargets()
    {
        if (noGoldRoot != null)
        {
            if (noGoldRect == null)
                noGoldRect = noGoldRoot.GetComponent<RectTransform>();
            if (noGoldCanvasGroup == null)
                noGoldCanvasGroup = noGoldRoot.GetComponent<CanvasGroup>();
        }

        if (noGoldCanvasGroup == null && noGoldLabel != null)
            noGoldCanvasGroup = noGoldLabel.GetComponent<CanvasGroup>();

        if (noGoldCanvasGroup == null && noGoldRoot != null)
            noGoldCanvasGroup = noGoldRoot.AddComponent<CanvasGroup>();

        if (noGoldRect == null && noGoldLabel != null)
            noGoldRect = noGoldLabel.rectTransform;

        if (noGoldRect != null && !hasCachedNoGoldPos)
        {
            noGoldStartPos = noGoldRect.anchoredPosition;
            hasCachedNoGoldPos = true;
        }
    }
}
