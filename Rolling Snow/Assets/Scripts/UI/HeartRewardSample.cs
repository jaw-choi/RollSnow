using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeartRewardSample : MonoBehaviour
{
    [Header("Ad Reward")]
    [SerializeField] private int adRewardHearts = 1;
    [SerializeField] private RewardedAdManager rewardedAdManager;

    [Header("Ad Confirmation")]
    [SerializeField] private bool skipAdConfirm = true;
    [SerializeField] private GameObject adConfirmPanel;
    [SerializeField] private GameObject adConfirmPanelPrefab;
    [SerializeField] private Transform adConfirmParent;
    [SerializeField] private TMP_Text adConfirmMessageLabel;
    [SerializeField] private string adConfirmMessage = "광고를 보시겠습니까";
    [SerializeField] private Button adConfirmYesButton;
    [SerializeField] private Button adConfirmNoButton;
    [SerializeField] private string adConfirmYesButtonName = "Yes";
    [SerializeField] private string adConfirmNoButtonName = "No";
    [SerializeField] private Image adConfirmBlocker;
    [SerializeField] private bool createBlockerIfMissing = true;
    [SerializeField] private Color adConfirmBlockerColor = new Color(0f, 0f, 0f, 0.35f);

    [Header("Gold Reward")]
    [SerializeField] private int adRewardGold = 20;

    [Header("Ad Cooldown")]
    [SerializeField] private float adCooldownSeconds = 60f;
    [SerializeField] private Button heartAdButton;
    [SerializeField] private Button goldAdButton;
    [SerializeField] private TextMeshProUGUI heartCooldownLabel;
    [SerializeField] private TextMeshProUGUI goldCooldownLabel;
    [SerializeField] private string cooldownFormat = "{0}s";
    [SerializeField] private float cooldownTickSeconds = 1f;
    [SerializeField] private bool hideCooldownLabelWhenReady = true;

    [Header("Gem Reward (Sample Storage)")]
    [SerializeField] private int gemCost = 10;
    [SerializeField] private string gemKey = "Sample.Gems";

    [Header("Gold Spend Reward")]
    [SerializeField] private int goldCost = 50;
    [SerializeField] private int goldRewardHearts = 1;

    Action pendingAdAction;
    GameObject adConfirmInstance;
    bool confirmBindingsReady;
    Coroutine heartCooldownRoutine;
    Coroutine goldCooldownRoutine;

    void Awake()
    {
        if (adConfirmPanel != null && IsSceneObject(adConfirmPanel))
        {
            EnsureConfirmBindings(adConfirmPanel);
            EnsureConfirmBlocker(adConfirmPanel);
            SetConfirmVisible(false);
        }
    }

    public void OnAdRewardButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        ShowAdConfirm(() =>
        {
            var manager = rewardedAdManager;
            if (manager == null)
                manager = RewardedAdManager.Instance ?? FindObjectOfType<RewardedAdManager>();

            if (manager == null)
            {
                Debug.LogWarning("RewardedAdManager not found.");
                return;
            }

            manager.ShowRewardedAd(() =>
            {
                var system = HeartSystem.GetOrCreate();
                if (system == null)
                    return;

                system.GrantHearts(Mathf.Max(1, adRewardHearts));
                StartCooldown(heartAdButton, heartCooldownLabel, ref heartCooldownRoutine);
            });
        });
    }

    public void OnGemUseButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return;

        int currentGems = PlayerPrefs.GetInt(gemKey, 0);
        if (currentGems < gemCost)
            return;

        PlayerPrefs.SetInt(gemKey, currentGems - gemCost);
        PlayerPrefs.Save();

        system.GrantHearts(1);
    }

    public void OnAdRewardGoldButtonClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        ShowAdConfirm(() =>
        {
            var manager = rewardedAdManager;
            if (manager == null)
                manager = RewardedAdManager.Instance ?? FindObjectOfType<RewardedAdManager>();

            if (manager == null)
            {
                Debug.LogWarning("RewardedAdManager not found.");
                return;
            }

            manager.ShowRewardedAd(() =>
            {
                var gold = GoldSystem.GetOrCreate();
                if (gold == null)
                    return;

                gold.AddGold(Mathf.Max(1, adRewardGold));
                StartCooldown(goldAdButton, goldCooldownLabel, ref goldCooldownRoutine);
            });
        });
    }

    public void OnSpendGoldForHeartClicked()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        var heartSystem = HeartSystem.GetOrCreate();
        var goldSystem = GoldSystem.GetOrCreate();
        if (heartSystem == null || goldSystem == null)
            return;

        int cost = Mathf.Max(1, goldCost);
        if (!goldSystem.TrySpendGold(cost))
            return;

        heartSystem.GrantHearts(Mathf.Max(1, goldRewardHearts));
    }

    void ShowAdConfirm(Action onConfirm)
    {
        if (skipAdConfirm)
        {
            onConfirm?.Invoke();
            return;
        }

        var panel = GetOrCreateConfirmPanel();
        if (panel == null)
        {
            onConfirm?.Invoke();
            return;
        }

        pendingAdAction = onConfirm;
        EnsureConfirmBindings(panel);
        if (adConfirmMessageLabel != null)
            adConfirmMessageLabel.text = adConfirmMessage;
        EnsureConfirmBlocker(panel);
        SetConfirmVisible(true);
    }

    public void OnAdConfirmYes()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        SetConfirmVisible(false);
        var action = pendingAdAction;
        pendingAdAction = null;
        action?.Invoke();
    }

    public void OnAdConfirmNo()
    {
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
        pendingAdAction = null;
        SetConfirmVisible(false);
    }

    GameObject GetOrCreateConfirmPanel()
    {
        if (adConfirmPanel != null && IsSceneObject(adConfirmPanel))
            return adConfirmPanel;

        GameObject prefab = adConfirmPanelPrefab;
        if (prefab == null && adConfirmPanel != null && !IsSceneObject(adConfirmPanel))
            prefab = adConfirmPanel;

        if (prefab == null)
            return null;

        if (adConfirmInstance == null)
        {
            Transform parent = adConfirmParent != null ? adConfirmParent : transform;
            adConfirmInstance = Instantiate(prefab, parent);
            adConfirmPanel = adConfirmInstance;
        }

        return adConfirmPanel;
    }

    void EnsureConfirmBindings(GameObject panel)
    {
        if (panel == null)
            return;

        if (adConfirmMessageLabel == null || !adConfirmMessageLabel.transform.IsChildOf(panel.transform))
            adConfirmMessageLabel = panel.GetComponentInChildren<TMP_Text>(true);

        if (adConfirmYesButton == null || !adConfirmYesButton.transform.IsChildOf(panel.transform))
            adConfirmYesButton = FindButton(panel.transform, adConfirmYesButtonName);

        if (adConfirmNoButton == null || !adConfirmNoButton.transform.IsChildOf(panel.transform))
            adConfirmNoButton = FindButton(panel.transform, adConfirmNoButtonName);

        if ((adConfirmYesButton == null || adConfirmNoButton == null) && !confirmBindingsReady)
        {
            var buttons = panel.GetComponentsInChildren<Button>(true);
            if (adConfirmYesButton == null && buttons.Length > 0)
                adConfirmYesButton = buttons[0];
            if (adConfirmNoButton == null && buttons.Length > 1)
                adConfirmNoButton = buttons[1];
        }

        if (adConfirmYesButton != null)
        {
            adConfirmYesButton.onClick.RemoveListener(OnAdConfirmYes);
            adConfirmYesButton.onClick.AddListener(OnAdConfirmYes);
        }

        if (adConfirmNoButton != null)
        {
            adConfirmNoButton.onClick.RemoveListener(OnAdConfirmNo);
            adConfirmNoButton.onClick.AddListener(OnAdConfirmNo);
        }

        confirmBindingsReady = adConfirmYesButton != null && adConfirmNoButton != null;
    }

    void EnsureConfirmBlocker(GameObject panel)
    {
        if (adConfirmBlocker != null || !createBlockerIfMissing || panel == null)
            return;

        Transform parent = panel.transform.parent != null ? panel.transform.parent : panel.transform;
        var blockerObject = new GameObject("AdConfirmBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        blockerObject.transform.SetParent(parent, false);

        var rect = blockerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = blockerObject.GetComponent<Image>();
        image.color = adConfirmBlockerColor;
        image.raycastTarget = true;

        int panelIndex = panel.transform.GetSiblingIndex();
        blockerObject.transform.SetSiblingIndex(panelIndex);
        adConfirmBlocker = image;
        blockerObject.SetActive(false);
    }

    void SetConfirmVisible(bool visible)
    {
        if (adConfirmPanel != null)
            adConfirmPanel.SetActive(visible);
        if (adConfirmBlocker != null)
            adConfirmBlocker.gameObject.SetActive(visible);
    }

    Button FindButton(Transform root, string nameHint)
    {
        if (root == null || string.IsNullOrEmpty(nameHint))
            return null;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button != null && button.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0)
                return button;
        }

        return null;
    }

    bool IsSceneObject(GameObject obj)
    {
        return obj != null && obj.scene.IsValid();
    }

    void StartCooldown(Button target, TextMeshProUGUI label, ref Coroutine routine)
    {
        if (target == null && label == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(CooldownRoutine(target, label));
    }

    IEnumerator CooldownRoutine(Button target, TextMeshProUGUI label)
    {
        if (target != null)
            target.interactable = false;

        float duration = Mathf.Max(0.1f, adCooldownSeconds);
        float tick = Mathf.Max(0.1f, cooldownTickSeconds);
        float remaining = duration;
        UpdateCooldownLabel(label, remaining, true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSecondsRealtime(tick);
            elapsed += tick;
            remaining = Mathf.Max(0f, duration - elapsed);
            UpdateCooldownLabel(label, remaining, true);
        }

        if (target != null)
            target.interactable = true;

        UpdateCooldownLabel(label, 0f, false);
    }

    void UpdateCooldownLabel(TextMeshProUGUI label, float remaining, bool active)
    {
        if (label == null)
            return;

        if (active)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            label.text = string.Format(cooldownFormat, seconds);
            label.gameObject.SetActive(true);
            return;
        }

        if (hideCooldownLabelWhenReady)
        {
            label.gameObject.SetActive(false);
        }
        else
        {
            label.text = string.Empty;
        }
    }
}
