using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RandomRewardButton : MonoBehaviour
{
    public enum RewardType
    {
        Skin,
        Heart1,
        Heart3
    }

    public enum SkinRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class SkinResultEntry
    {
        public string id;
        public string displayName;
        public SkinRarity rarity = SkinRarity.Common;
        public Sprite sprite;
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

    [Header("Skin Result Display")]
    [SerializeField] private bool showSkinResult = true;
    [SerializeField] private bool keepSkinResultVisible = true;
    [SerializeField] private bool blockWhileSkinResultVisible = true;
    [SerializeField] private bool closeSkinResultOnAnyInput = true;
    [SerializeField] private GameObject skinResultRoot;
    [SerializeField] private Image skinResultImage;
    [SerializeField] private CanvasGroup skinResultCanvasGroup;
    [SerializeField] private RectTransform skinResultRect;
    [SerializeField] private TMPro.TextMeshProUGUI skinResultNameLabel;
    [SerializeField] private TMPro.TextMeshProUGUI skinResultRarityLabel;
    [SerializeField] private Image skinResultRarityGlow;
    [SerializeField] private float skinResultVisibleAlpha = 1f;
    [SerializeField] private bool hideSkinResultOnAwake = true;
    [SerializeField] private float skinResultFadeIn = 0.12f;
    [SerializeField] private float skinResultPopScale = 1.12f;
    [SerializeField] private float skinResultPopDuration = 0.18f;
    [SerializeField] private float skinResultSettleDuration = 0.12f;
    [SerializeField] private float skinResultHold = 0.6f;
    [SerializeField] private float skinResultFadeOut = 0.2f;

    [Header("Skin Result Content")]
    [SerializeField] private SkinResultEntry[] skinResultEntries;
    [SerializeField] private Sprite[] skinResultSprites;
    [SerializeField] private bool preferUnownedSkins = true;
    [SerializeField] private string skinUnlockPrefix = "Skin.Unlocked.";

    [Header("Heart Result Content")]
    [SerializeField] private string heart1ResultName = "Heart +1";
    [SerializeField] private string heart3ResultName = "Heart +3";
    [SerializeField] private Sprite heart1ResultSprite;
    [SerializeField] private Sprite heart3ResultSprite;
    [SerializeField] private SkinRarity heart1Rarity = SkinRarity.Common;
    [SerializeField] private SkinRarity heart3Rarity = SkinRarity.Rare;

    [Header("Skin Result Rarity")]
    [SerializeField] private string commonRarityText = "Common";
    [SerializeField] private string rareRarityText = "Rare";
    [SerializeField] private string epicRarityText = "Epic";
    [SerializeField] private string legendaryRarityText = "Legendary";
    [SerializeField] private Color commonRarityColor = Color.white;
    [SerializeField] private Color rareRarityColor = new Color(0.35f, 0.8f, 1f, 1f);
    [SerializeField] private Color epicRarityColor = new Color(1f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color legendaryRarityColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float commonRarityPulseScale = 1f;
    [SerializeField] private float rareRarityPulseScale = 1.08f;
    [SerializeField] private float epicRarityPulseScale = 1.14f;
    [SerializeField] private float legendaryRarityPulseScale = 1.2f;
    [SerializeField] private float rarityPulseDuration = 0.35f;
    [SerializeField] private float rarityGlowAlpha = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool logReward = true;

    bool isAnimating;
    Coroutine effectRoutine;
    Coroutine noGoldRoutine;
    Vector2 noGoldStartPos;
    bool hasCachedNoGoldPos;
    bool isSkinResultVisible;
    SkinResultEntry pendingSkinResult;
    string pendingSkinId;
    SkinResultEntry lastSkinResult;
    bool hasCachedSkinResult;
    Vector3 skinResultBaseScale = Vector3.one;
    RectTransform skinResultGlowRect;
    Vector3 skinResultGlowBaseScale = Vector3.one;

    void Awake()
    {
        CacheNoGoldTargets();
        CacheSkinResultTargets();
        if (hideSkinResultOnAwake)
            HideSkinResult(true);
    }

    void OnDisable()
    {
        StopNoGoldRoutine();
    }

    void Update()
    {
        if (!closeSkinResultOnAnyInput || !isSkinResultVisible)
            return;

        if (IsAnyInputBegan())
            HideSkinResult(true);
    }

    public void OnRewardButtonClicked()
    {
        if (ignoreWhilePlaying && isAnimating)
            return;

        if (blockWhileSkinResultVisible && isSkinResultVisible)
            return;

        if (isSkinResultVisible && !blockWhileSkinResultVisible)
            HideSkinResult(false);

        if (!TrySpendGoldForReward())
            return;

        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Gacha);

        if (effectTarget == null)
            effectTarget = transform;

        pendingSkinResult = null;
        pendingSkinId = null;
        RewardType reward = RollReward();
        if (reward == RewardType.Skin)
            TrySelectSkinReward(out pendingSkinResult, out pendingSkinId);
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
            effectRoutine = StartCoroutine(PlayEffectThenSkinResult(reward));
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
                GrantSkin(pendingSkinResult, pendingSkinId);
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

    void GrantSkin(SkinResultEntry entry, string skinId)
    {
        int current = PlayerPrefs.GetInt(skinKey, 0);
        current = Mathf.Max(0, current) + Mathf.Max(1, skinAmount);
        PlayerPrefs.SetInt(skinKey, current);
        if (!string.IsNullOrEmpty(skinId))
            PlayerPrefs.SetInt($"{skinUnlockPrefix}{skinId}", 1);
        PlayerPrefs.Save();

        lastSkinResult = entry;
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

        yield return PlayRewardResultEffect(reward);

        isAnimating = false;
    }

    IEnumerator PlayEffectThenSkinResult(RewardType reward)
    {
        if (effectTarget != null)
            yield return PlayEffect(effectTarget);

        yield return PlayRewardResultEffect(reward);
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

    IEnumerator PlayRewardResultEffect(RewardType reward)
    {
        if (!showSkinResult)
            yield break;

        CacheSkinResultTargets();
        SkinResultEntry entry = null;
        switch (reward)
        {
            case RewardType.Skin:
                entry = lastSkinResult ?? pendingSkinResult;
                if (entry == null && !TrySelectSkinReward(out entry, out _))
                    yield break;
                break;
            case RewardType.Heart1:
                entry = BuildHeartResult(RewardType.Heart1);
                break;
            case RewardType.Heart3:
                entry = BuildHeartResult(RewardType.Heart3);
                break;
            default:
                yield break;
        }

        bool hasImageTarget = skinResultImage != null;
        bool hasTextTarget = skinResultNameLabel != null || skinResultRarityLabel != null;
        if (!hasImageTarget && !hasTextTarget)
            yield break;

        if (skinResultRoot != null)
            skinResultRoot.SetActive(true);

        if (hasImageTarget)
        {
            skinResultImage.enabled = entry != null && entry.sprite != null;
            skinResultImage.sprite = entry != null ? entry.sprite : null;
        }

        if (entry != null)
            ApplySkinResultText(entry);

        SetSkinResultAlpha(0f);

        RectTransform rect = skinResultRect != null ? skinResultRect : (hasImageTarget ? skinResultImage.rectTransform : null);
        Vector3 startScale = rect != null ? rect.localScale : skinResultBaseScale;
        Vector3 popScale = startScale * Mathf.Max(0.01f, skinResultPopScale);

        float fadeIn = Mathf.Max(0f, skinResultFadeIn);
        float popIn = Mathf.Max(0f, skinResultPopDuration);
        float inDuration = Mathf.Max(fadeIn, popIn);

        float elapsed = 0f;
        while (elapsed < inDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float tAlpha = fadeIn > 0f ? Mathf.Clamp01(elapsed / fadeIn) : 1f;
            float tScale = popIn > 0f ? Mathf.Clamp01(elapsed / popIn) : 1f;
            SetSkinResultAlpha(Mathf.Lerp(0f, skinResultVisibleAlpha, tAlpha));
            if (rect != null)
                rect.localScale = Vector3.Lerp(startScale, popScale, tScale);
            yield return null;
        }

        SetSkinResultAlpha(skinResultVisibleAlpha);
        if (rect != null)
            rect.localScale = popScale;

        if (entry != null)
            yield return PlayRarityPulse(entry.rarity, rect);

        if (keepSkinResultVisible)
        {
            if (rect != null)
                yield return PlaySkinResultSettle(rect, popScale, startScale);

            isSkinResultVisible = true;
            yield break;
        }

        if (skinResultHold > 0f)
            yield return useUnscaledTime ? new WaitForSecondsRealtime(skinResultHold) : new WaitForSeconds(skinResultHold);

        float fadeOut = Mathf.Max(0f, skinResultFadeOut);
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = fadeOut > 0f ? Mathf.Clamp01(elapsed / fadeOut) : 1f;
            SetSkinResultAlpha(Mathf.Lerp(skinResultVisibleAlpha, 0f, t));
            if (rect != null)
                rect.localScale = Vector3.Lerp(popScale, startScale, t);
            yield return null;
        }

        SetSkinResultAlpha(0f);
        if (rect != null)
            rect.localScale = startScale;

        if (skinResultRoot != null)
            skinResultRoot.SetActive(false);
    }

    bool TrySelectSkinReward(out SkinResultEntry entry, out string skinId)
    {
        entry = null;
        skinId = null;

        if (skinResultEntries != null && skinResultEntries.Length > 0)
            return TrySelectFromEntries(skinResultEntries, out entry, out skinId);

        if (skinResultSprites != null && skinResultSprites.Length > 0)
            return TrySelectFromSprites(skinResultSprites, out entry, out skinId);

        return false;
    }

    SkinResultEntry BuildHeartResult(RewardType reward)
    {
        if (reward != RewardType.Heart1 && reward != RewardType.Heart3)
            return null;

        string name = reward == RewardType.Heart1 ? heart1ResultName : heart3ResultName;
        Sprite sprite = reward == RewardType.Heart1 ? heart1ResultSprite : heart3ResultSprite;
        SkinRarity rarity = reward == RewardType.Heart1 ? heart1Rarity : heart3Rarity;

        return new SkinResultEntry
        {
            displayName = name,
            rarity = rarity,
            sprite = sprite
        };
    }

    bool TrySelectFromEntries(SkinResultEntry[] entries, out SkinResultEntry entry, out string skinId)
    {
        entry = null;
        skinId = null;

        int count = entries.Length;
        int[] all = new int[count];
        int[] unowned = new int[count];
        int allCount = 0;
        int unownedCount = 0;

        for (int i = 0; i < count; i++)
        {
            SkinResultEntry candidate = entries[i];
            if (candidate == null || candidate.sprite == null)
                continue;

            all[allCount++] = i;
            if (preferUnownedSkins && !IsSkinUnlocked(candidate, i))
                unowned[unownedCount++] = i;
        }

        if (allCount == 0)
            return false;

        int selectedIndex = (preferUnownedSkins && unownedCount > 0)
            ? unowned[UnityEngine.Random.Range(0, unownedCount)]
            : all[UnityEngine.Random.Range(0, allCount)];

        entry = entries[selectedIndex];
        skinId = GetSkinId(entry, selectedIndex);
        return entry != null && entry.sprite != null;
    }

    bool TrySelectFromSprites(Sprite[] sprites, out SkinResultEntry entry, out string skinId)
    {
        entry = null;
        skinId = null;

        int count = sprites.Length;
        int[] all = new int[count];
        int[] unowned = new int[count];
        int allCount = 0;
        int unownedCount = 0;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
                continue;

            all[allCount++] = i;
            if (preferUnownedSkins && !IsSkinUnlocked(sprite, i))
                unowned[unownedCount++] = i;
        }

        if (allCount == 0)
            return false;

        int selectedIndex = (preferUnownedSkins && unownedCount > 0)
            ? unowned[UnityEngine.Random.Range(0, unownedCount)]
            : all[UnityEngine.Random.Range(0, allCount)];

        Sprite selectedSprite = sprites[selectedIndex];
        skinId = GetSkinId(selectedSprite, selectedIndex);
        entry = new SkinResultEntry
        {
            id = skinId,
            displayName = selectedSprite != null ? selectedSprite.name : string.Empty,
            rarity = SkinRarity.Common,
            sprite = selectedSprite
        };

        return selectedSprite != null;
    }

    bool IsSkinUnlocked(SkinResultEntry entry, int index)
    {
        string id = GetSkinId(entry, index);
        return IsSkinUnlocked(id);
    }

    bool IsSkinUnlocked(Sprite sprite, int index)
    {
        string id = GetSkinId(sprite, index);
        return IsSkinUnlocked(id);
    }

    bool IsSkinUnlocked(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return false;

        return PlayerPrefs.GetInt($"{skinUnlockPrefix}{skinId}", 0) > 0;
    }

    string GetSkinId(SkinResultEntry entry, int index)
    {
        if (entry == null)
            return $"skin_{index}";

        if (!string.IsNullOrEmpty(entry.id))
            return entry.id;

        if (entry.sprite != null && !string.IsNullOrEmpty(entry.sprite.name))
            return entry.sprite.name;

        return $"skin_{index}";
    }

    string GetSkinId(Sprite sprite, int index)
    {
        if (sprite != null && !string.IsNullOrEmpty(sprite.name))
            return sprite.name;

        return $"skin_{index}";
    }

    void ApplySkinResultText(SkinResultEntry entry)
    {
        if (skinResultNameLabel != null)
            skinResultNameLabel.text = GetSkinDisplayName(entry);

        if (skinResultRarityLabel != null)
            skinResultRarityLabel.text = GetRarityText(entry.rarity);

        Color rarityColor = GetRarityColor(entry.rarity);
        if (skinResultRarityLabel != null)
            skinResultRarityLabel.color = rarityColor;

        if (skinResultRarityGlow != null)
        {
            skinResultRarityGlow.gameObject.SetActive(true);
            Color glowColor = rarityColor;
            glowColor.a = 0f;
            skinResultRarityGlow.color = glowColor;
        }
    }

    string GetSkinDisplayName(SkinResultEntry entry)
    {
        if (entry == null)
            return "Skin";

        if (!string.IsNullOrEmpty(entry.displayName))
            return entry.displayName;

        if (!string.IsNullOrEmpty(entry.id))
            return entry.id;

        if (entry.sprite != null && !string.IsNullOrEmpty(entry.sprite.name))
            return entry.sprite.name;

        return "Skin";
    }

    string GetRarityText(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Rare:
                return rareRarityText;
            case SkinRarity.Epic:
                return epicRarityText;
            case SkinRarity.Legendary:
                return legendaryRarityText;
            default:
                return commonRarityText;
        }
    }

    Color GetRarityColor(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Rare:
                return rareRarityColor;
            case SkinRarity.Epic:
                return epicRarityColor;
            case SkinRarity.Legendary:
                return legendaryRarityColor;
            default:
                return commonRarityColor;
        }
    }

    float GetRarityPulseScale(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Rare:
                return rareRarityPulseScale;
            case SkinRarity.Epic:
                return epicRarityPulseScale;
            case SkinRarity.Legendary:
                return legendaryRarityPulseScale;
            default:
                return commonRarityPulseScale;
        }
    }

    IEnumerator PlayRarityPulse(SkinRarity rarity, RectTransform targetRect)
    {
        float duration = Mathf.Max(0f, rarityPulseDuration);
        float scale = Mathf.Max(1f, GetRarityPulseScale(rarity));
        if (duration <= 0f)
            yield break;

        RectTransform pulseRect = skinResultRarityGlow != null ? skinResultRarityGlow.rectTransform : targetRect;
        if (pulseRect == null)
            yield break;

        Vector3 baseScale = pulseRect.localScale;
        if (skinResultRarityGlow != null && skinResultGlowRect != null)
            baseScale = skinResultGlowBaseScale;

        Vector3 targetScale = baseScale * scale;
        Color glowColor = GetRarityColor(rarity);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float pulseT = t < 0.5f ? t / 0.5f : (1f - t) / 0.5f;

            pulseRect.localScale = Vector3.Lerp(baseScale, targetScale, pulseT);

            if (skinResultRarityGlow != null)
            {
                Color c = glowColor;
                c.a = rarityGlowAlpha * pulseT;
                skinResultRarityGlow.color = c;
            }

            yield return null;
        }

        pulseRect.localScale = baseScale;

        if (skinResultRarityGlow != null)
        {
            Color c = glowColor;
            c.a = 0f;
            skinResultRarityGlow.color = c;
        }
    }

    IEnumerator PlaySkinResultSettle(RectTransform rect, Vector3 fromScale, Vector3 toScale)
    {
        float duration = Mathf.Max(0f, skinResultSettleDuration);
        if (duration <= 0f)
        {
            rect.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            rect.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        rect.localScale = toScale;
    }

    void CacheSkinResultTargets()
    {
        if (skinResultImage == null && skinResultRoot != null)
            skinResultImage = skinResultRoot.GetComponentInChildren<Image>(true);

        if (skinResultImage == null)
            return;

        if (skinResultRect == null)
            skinResultRect = skinResultImage.rectTransform;

        if (skinResultCanvasGroup == null)
        {
            skinResultCanvasGroup = skinResultImage.GetComponent<CanvasGroup>();
            if (skinResultCanvasGroup == null && skinResultRoot != null)
                skinResultCanvasGroup = skinResultRoot.GetComponent<CanvasGroup>();
        }

        if (skinResultGlowRect == null && skinResultRarityGlow != null)
            skinResultGlowRect = skinResultRarityGlow.rectTransform;

        if (!hasCachedSkinResult)
        {
            skinResultBaseScale = skinResultRect != null ? skinResultRect.localScale : skinResultImage.transform.localScale;
            if (skinResultGlowRect != null)
                skinResultGlowBaseScale = skinResultGlowRect.localScale;
            hasCachedSkinResult = true;
        }
    }

    void HideSkinResult(bool force)
    {
        if (!force && !showSkinResult)
            return;

        isSkinResultVisible = false;
        if (skinResultRoot != null)
            skinResultRoot.SetActive(false);

        SetSkinResultAlpha(0f);

        if (skinResultRect != null)
            skinResultRect.localScale = skinResultBaseScale;

        if (skinResultRarityGlow != null)
        {
            Color c = skinResultRarityGlow.color;
            c.a = 0f;
            skinResultRarityGlow.color = c;
        }

        if (skinResultGlowRect != null)
            skinResultGlowRect.localScale = skinResultGlowBaseScale;
    }

    public void OnSkinResultCloseClicked()
    {
        HideSkinResult(true);
    }

    bool IsAnyInputBegan()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        return false;
#else
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.touchCount <= 0)
            return false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
#endif
    }

    void SetSkinResultAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        if (skinResultCanvasGroup != null)
            skinResultCanvasGroup.alpha = alpha;

        if (skinResultImage == null)
            return;

        Color color = skinResultImage.color;
        color.a = alpha;
        skinResultImage.color = color;
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
