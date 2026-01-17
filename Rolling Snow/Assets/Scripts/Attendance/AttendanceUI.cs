using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class AttendanceUI : MonoBehaviour
{
    [Header("Service")]
    [SerializeField] private string day7SkinId = AttendanceService.DefaultDay7SkinId;

    [Header("Attendance Button")]
    [SerializeField] private Button attendanceButton;
    [SerializeField] private GameObject notificationDot;

    [Header("Panel")]
    [SerializeField] private GameObject attendancePanel;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button attendancePanelSelfCloseButton;

    [Header("Slots")]
    [SerializeField] private Transform slotsRoot;
    [SerializeField] private GameObject[] slotRoots = new GameObject[7];
    [SerializeField] private string claimedMarkerName = "Claimed";
    [SerializeField] private string claimedCircleImageName = "ClaimedCircle";
    [SerializeField] private Sprite claimedCircleSprite;
    [SerializeField] private string todayMarkerName = "Today";
    [SerializeField] private string alarmDotName = "Alram Dot";

    [Header("Reward Popup")]
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private Image rewardPopupTargetImage;
    [SerializeField] private Sprite[] rewardPopupSprites = new Sprite[7];
    [SerializeField] private Sprite[] rewardPopupSpritesKorean = new Sprite[7];
    [SerializeField] private Sprite[] rewardPopupSpritesEnglish = new Sprite[7];
    [SerializeField] private Sprite rewardPopupDay7SecondarySprite;
    [SerializeField] private Sprite rewardPopupDay7SecondarySpriteKorean;
    [SerializeField] private Sprite rewardPopupDay7SecondarySpriteEnglish;
    [SerializeField] private Sprite rewardPopupAltSprite;
    [SerializeField] private Sprite rewardPopupAltSpriteKorean;
    [SerializeField] private Sprite rewardPopupAltSpriteEnglish;
    [SerializeField] private Button rewardPopupSelfCloseButton;

    [Header("Debug")]
    [SerializeField] private Button debugClaimTodayButton;
    [SerializeField] private Button debugAdvanceDayButton;
    [SerializeField] private Button debugResetButton;

    private class SlotCache
    {
        public GameObject root;
        public GameObject claimedMarker;
        public Image[] claimedCircleImages;
        public GameObject todayMarker;
        public GameObject[] alarmDots;
    }

    AttendanceService service;
    bool isProcessing;
    SlotCache[] slotCache;
    Sprite pendingPopupSprite;

    void Awake()
    {
        EnsureService();
        BindButtons();
        CacheSlots();
        SetRewardPopupVisible(false);
        if (rewardPopupTargetImage != null)
            rewardPopupTargetImage.gameObject.SetActive(false);
        if (attendancePanel != null)
            attendancePanel.SetActive(false);
        RefreshAll();
    }

    void OnEnable()
    {
        RefreshAll();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            RefreshAll();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
            RefreshAll();
    }

    public void OnAttendanceButtonClicked()
    {
        if (attendancePanel != null)
            attendancePanel.SetActive(true);
        RefreshAll();
    }

    public void OnClosePanelClicked()
    {
        if (attendancePanel != null)
            attendancePanel.SetActive(false);
    }

    public void OnClaimButtonClicked()
    {
        if (isProcessing)
            return;

        isProcessing = true;
        if (claimButton != null)
            claimButton.interactable = false;

        var result = service.TryClaimToday();
        isProcessing = false;
        RefreshAll();
        if (result.success)
            ShowRewardPopup(result);
    }

    public void OnCloseRewardPopupClicked()
    {
        if (pendingPopupSprite != null)
        {
            Sprite nextSprite = pendingPopupSprite;
            pendingPopupSprite = null;
            ApplyPopupSprite(nextSprite);
            SetRewardPopupVisible(true);
            return;
        }

        SetRewardPopupVisible(false);
        if (rewardPopupTargetImage != null)
            rewardPopupTargetImage.gameObject.SetActive(false);
    }

    public void OnDebugClaimTodayClicked()
    {
        if (isProcessing)
            return;

        EnsureService();
        service.Load();
        var result = service.TryClaimToday();
        RefreshAll();
        if (result.success)
            ShowRewardPopup(result);
    }

    public void OnDebugAdvanceDayClicked()
    {
        EnsureService();
        service.Load();
        var data = service.Data;
        data.lastClaimDate = GetPreviousDateString(data.lastClaimDate);
        service.Save();
        RefreshAll();
    }

    public void OnDebugResetClicked()
    {
        EnsureService();
        service.Load();
        var data = service.Data;
        data.lastClaimDate = string.Empty;
        data.streak = 0;
        data.cycleIndex = 0;
        service.Save();
        RefreshAll();
    }

    void EnsureService()
    {
        if (service == null)
            service = new AttendanceService(day7SkinId);
        else
            service.Day7SkinId = day7SkinId;
    }

    void BindButtons()
    {
        if (attendanceButton != null)
        {
            attendanceButton.onClick.RemoveListener(OnAttendanceButtonClicked);
            attendanceButton.onClick.AddListener(OnAttendanceButtonClicked);
        }

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimButtonClicked);
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        BindSelfCloseButton(attendancePanelSelfCloseButton, attendancePanel, OnClosePanelClicked);

        BindSelfCloseButton(rewardPopupSelfCloseButton, rewardPopup, OnCloseRewardPopupClicked);

        BindDebugButtons();
    }

    void RefreshAll()
    {
        EnsureService();
        service.Load();
        if (slotCache == null || slotCache.Length == 0)
            CacheSlots();
        RefreshNotification();
        RefreshClaimButton();
        RefreshSlots();
    }

    void CacheSlots()
    {
        var roots = ResolveSlotRoots();
        if (roots == null || roots.Length == 0)
        {
            slotCache = null;
            return;
        }

        slotCache = new SlotCache[roots.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null)
                continue;

            slotCache[i] = new SlotCache
            {
                root = root,
                claimedMarker = FindChildByName(root.transform, claimedMarkerName),
                claimedCircleImages = FindChildImages(root.transform, claimedCircleImageName),
                todayMarker = FindChildByName(root.transform, todayMarkerName),
                alarmDots = FindChildObjectsByName(root.transform, alarmDotName)
            };
        }
    }

    GameObject[] ResolveSlotRoots()
    {
        if (slotRoots != null && slotRoots.Length > 0)
        {
            bool any = false;
            for (int i = 0; i < slotRoots.Length; i++)
            {
                if (slotRoots[i] != null)
                {
                    any = true;
                    break;
                }
            }

            if (any)
                return slotRoots;
        }

        if (slotsRoot == null)
            return Array.Empty<GameObject>();

        int count = Mathf.Min(7, slotsRoot.childCount);
        var roots = new GameObject[count];
        for (int i = 0; i < count; i++)
            roots[i] = slotsRoot.GetChild(i).gameObject;
        return roots;
    }

    void RefreshNotification()
    {
        if (notificationDot != null)
            notificationDot.SetActive(service.IsClaimableToday());
    }

    void RefreshClaimButton()
    {
        if (claimButton == null)
            return;

        bool canClaim = service.IsClaimableToday();
        claimButton.interactable = !isProcessing && canClaim;
    }

    void RefreshSlots()
    {
        if (slotCache == null || slotCache.Length == 0)
            return;

        int claimedCount = service.GetClaimedCountInCycle();
        bool claimedToday = service.HasClaimedToday();
        int highlightIndex = claimedToday ? service.GetPreviousClaimIndex() : service.GetCurrentCycleIndex();

        for (int i = 0; i < slotCache.Length; i++)
        {
            var slot = slotCache[i];
            if (slot == null)
                continue;

            if (slot.root != null && !slot.root.activeSelf)
                slot.root.SetActive(true);

            if (slot.claimedMarker != null)
                slot.claimedMarker.SetActive(i < claimedCount);

            if (slot.claimedCircleImages != null && slot.claimedCircleImages.Length > 0)
            {
                bool isClaimed = i < claimedCount;
                for (int j = 0; j < slot.claimedCircleImages.Length; j++)
                {
                    var image = slot.claimedCircleImages[j];
                    if (image == null)
                        continue;

                    if (isClaimed && claimedCircleSprite != null)
                        image.sprite = claimedCircleSprite;
                    image.gameObject.SetActive(isClaimed && image.sprite != null);
                }
            }

            if (slot.todayMarker != null)
                slot.todayMarker.SetActive(i == highlightIndex);

            if (slot.alarmDots != null && slot.alarmDots.Length > 0)
            {
                bool showAlarm = i == highlightIndex && service.IsClaimableToday();
                for (int j = 0; j < slot.alarmDots.Length; j++)
                {
                    var dot = slot.alarmDots[j];
                    if (dot != null)
                        dot.SetActive(showAlarm);
                }
            }
        }
    }

    void ShowRewardPopup(AttendanceService.ClaimResult result)
    {
        if (rewardPopup == null)
            return;

        Sprite primary = GetRewardPopupSpriteForResult(result, out Sprite secondary);
        if (primary == null && secondary != null)
        {
            primary = secondary;
            secondary = null;
        }

        pendingPopupSprite = secondary;
        ApplyPopupSprite(primary);
        SetRewardPopupVisible(primary != null);
    }

    void SetRewardPopupVisible(bool visible)
    {
        if (rewardPopup != null)
            rewardPopup.SetActive(visible);
        if (!visible && rewardPopupTargetImage != null)
            rewardPopupTargetImage.gameObject.SetActive(false);
    }

    void ApplyPopupSprite(Sprite sprite)
    {
        if (rewardPopupTargetImage == null)
            return;

        if (sprite != null)
            rewardPopupTargetImage.sprite = sprite;

        rewardPopupTargetImage.gameObject.SetActive(sprite != null);
    }

    Sprite GetRewardPopupSpriteForResult(AttendanceService.ClaimResult result, out Sprite secondary)
    {
        secondary = null;
        int dayIndex = Mathf.Clamp(result.dayIndex, 0, 6);
        Sprite primary = ResolveRewardPopupSprite(dayIndex, rewardPopupSprites);
        if (dayIndex == 6)
        {
            if (result.usedGoldInsteadOfSkin && rewardPopupAltSprite != null)
                secondary = ResolveRewardPopupAltSprite();
            else
                secondary = ResolveRewardPopupDay7SecondarySprite();
        }

        return primary;
    }

    static Sprite GetRewardPopupSpriteFromArray(Sprite[] sprites, int dayIndex)
    {
        if (sprites == null || dayIndex < 0 || dayIndex >= sprites.Length)
            return null;

        return sprites[dayIndex];
    }

    Sprite ResolveRewardPopupSprite(int dayIndex, Sprite[] fallbackSprites)
    {
        Sprite localized = ResolveLocalizedSprite(
            GetRewardPopupSpriteFromArray(rewardPopupSpritesKorean, dayIndex),
            GetRewardPopupSpriteFromArray(rewardPopupSpritesEnglish, dayIndex)
        );

        if (localized != null)
            return localized;

        return GetRewardPopupSpriteFromArray(fallbackSprites, dayIndex);
    }

    Sprite ResolveRewardPopupDay7SecondarySprite()
    {
        Sprite localized = ResolveLocalizedSprite(rewardPopupDay7SecondarySpriteKorean, rewardPopupDay7SecondarySpriteEnglish);
        if (localized != null)
            return localized;

        return rewardPopupDay7SecondarySprite;
    }

    Sprite ResolveRewardPopupAltSprite()
    {
        Sprite localized = ResolveLocalizedSprite(rewardPopupAltSpriteKorean, rewardPopupAltSpriteEnglish);
        if (localized != null)
            return localized;

        return rewardPopupAltSprite;
    }

    Sprite ResolveLocalizedSprite(Sprite korean, Sprite english)
    {
        var language = LocalizationUtility.GetCurrentLanguage();
        Sprite localized = language == GameLanguage.English ? english : korean;
        if (localized == null)
            localized = language == GameLanguage.English ? korean : english;
        return localized;
    }

    void BindDebugButtons()
    {
        BindButton(debugClaimTodayButton, OnDebugClaimTodayClicked);
        BindButton(debugAdvanceDayButton, OnDebugAdvanceDayClicked);
        BindButton(debugResetButton, OnDebugResetClicked);
    }

    static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    static string GetPreviousDateString(string dateString)
    {
        if (TryParseDate(dateString, out DateTime parsed))
            return parsed.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        DateTime today = AttendanceService.GetKstNow().Date;
        return today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    static bool TryParseDate(string dateString, out DateTime date)
    {
        return DateTime.TryParseExact(
            dateString,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    void BindSelfCloseButton(Button button, GameObject root, UnityEngine.Events.UnityAction action)
    {
        if (button == null && root != null)
            button = root.GetComponent<Button>();

        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    static GameObject FindChildByName(Transform root, string nameHint)
    {
        if (root == null || string.IsNullOrEmpty(nameHint))
            return null;

        if (root.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0)
            return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0)
                return child.gameObject;

            var nested = FindChildByName(child, nameHint);
            if (nested != null)
                return nested;
        }

        return null;
    }

    static Image[] FindChildImages(Transform root, string nameHint)
    {
        if (root == null || string.IsNullOrEmpty(nameHint))
            return Array.Empty<Image>();

        var results = new System.Collections.Generic.List<Image>();
        CollectImagesByName(root, nameHint, results);
        return results.ToArray();
    }

    static GameObject[] FindChildObjectsByName(Transform root, string nameHint)
    {
        if (root == null || string.IsNullOrEmpty(nameHint))
            return Array.Empty<GameObject>();

        var results = new System.Collections.Generic.List<GameObject>();
        CollectObjectsByName(root, nameHint, results);
        return results.ToArray();
    }

    static void CollectObjectsByName(Transform root, string nameHint, System.Collections.Generic.List<GameObject> results)
    {
        if (root == null)
            return;

        if (root.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0)
            results.Add(root.gameObject);

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child != null)
                CollectObjectsByName(child, nameHint, results);
        }
    }

    static void CollectImagesByName(Transform root, string nameHint, System.Collections.Generic.List<Image> results)
    {
        if (root == null)
            return;

        if (root.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var image = root.GetComponent<Image>();
            if (image != null)
                results.Add(image);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child != null)
                CollectImagesByName(child, nameHint, results);
        }
    }
}
