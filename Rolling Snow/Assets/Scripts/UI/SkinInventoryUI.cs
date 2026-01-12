using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinInventoryUI : MonoBehaviour
{
    [Header("Skins")]
    [SerializeField] private SkinCatalog catalog;
    [SerializeField] private bool wrapNavigation = true;

    [Header("Preview")]
    [SerializeField] private Image previewImage;
    [SerializeField] private Sprite[] previewSprites;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Image statusImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite ownedSprite;
    [SerializeField] private Sprite equippedSprite;
    [SerializeField] private bool hideStatusTextWhenSprite = true;
    [SerializeField] private bool useLockedRoot = false;
    [SerializeField] private bool tintPreviewWhenLocked = true;
    [SerializeField] private Color32 lockedPreviewTint = new Color32(75, 75, 75, 255);
    [SerializeField] private GameObject lockedRoot;

    [Header("Apply")]
    [SerializeField] private Button applyButton;
    [SerializeField] private TextMeshProUGUI applyButtonLabel;
    [SerializeField] private Image applyButtonImage;
    [SerializeField] private Sprite applySprite;
    [SerializeField] private Sprite applyEquippedSprite;
    [SerializeField] private Sprite applyLockedSprite;
    [SerializeField] private bool hideApplyTextWhenSprite = true;
    [SerializeField] private string applyText = "Apply";
    [SerializeField] private string equippedText = "Equipped";
    [SerializeField] private string lockedText = "Locked";
    [SerializeField] private string ownedText = "Owned";

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    int currentIndex;

    void OnEnable()
    {
        HookButtons(true);
        SelectEquippedOrDefault();
    }

    void OnDisable()
    {
        HookButtons(false);
    }

    void HookButtons(bool on)
    {
        if (leftButton != null)
        {
            if (on) leftButton.onClick.AddListener(OnLeftClicked);
            else leftButton.onClick.RemoveListener(OnLeftClicked);
        }

        if (rightButton != null)
        {
            if (on) rightButton.onClick.AddListener(OnRightClicked);
            else rightButton.onClick.RemoveListener(OnRightClicked);
        }

        if (applyButton != null)
        {
            if (on) applyButton.onClick.AddListener(OnApplyClicked);
            else applyButton.onClick.RemoveListener(OnApplyClicked);
        }
    }

    public void OnLeftClicked()
    {
        Step(-1);
    }

    public void OnRightClicked()
    {
        Step(1);
    }

    void Step(int delta)
    {
        if (catalog == null || catalog.skins == null || catalog.skins.Length == 0)
            return;

        int next = currentIndex + delta;
        if (wrapNavigation)
        {
            int count = catalog.skins.Length;
            next = (next % count + count) % count;
        }
        else
        {
            next = Mathf.Clamp(next, 0, catalog.skins.Length - 1);
        }

        currentIndex = next;
        RefreshUI();
    }

    public void OnApplyClicked()
    {
        if (catalog == null || catalog.skins == null || catalog.skins.Length == 0)
            return;

        var entry = catalog.skins[currentIndex];
        string skinId = SkinStorage.GetSkinId(entry, currentIndex);
        string defaultId = catalog.GetDefaultSkinId();
        bool owned = SkinStorage.IsUnlocked(skinId, defaultId, catalog.unlockPrefix);
        if (!owned)
        {
            RefreshUI();
            return;
        }

        SkinStorage.SetEquippedSkinId(skinId, catalog.equippedKey);
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);
        RefreshUI();
    }

    void SelectEquippedOrDefault()
    {
        if (catalog == null || catalog.skins == null || catalog.skins.Length == 0)
        {
            RefreshUI();
            return;
        }

        string defaultId = catalog.GetDefaultSkinId();
        string equippedId = SkinStorage.GetEquippedSkinId(defaultId, catalog.equippedKey);
        if (!SkinStorage.IsUnlocked(equippedId, defaultId, catalog.unlockPrefix))
            equippedId = defaultId;

        int index = catalog.FindSkinIndex(equippedId);
        if (index < 0)
            index = Mathf.Clamp(catalog.defaultSkinIndex, 0, catalog.skins.Length - 1);

        currentIndex = index;
        RefreshUI();
    }

    void UpdateStatusVisual(bool owned, bool isEquipped)
    {
        if (statusLabel != null)
        {
            if (!owned)
                statusLabel.text = lockedText;
            else if (isEquipped)
                statusLabel.text = equippedText;
            else
                statusLabel.text = ownedText;
        }

        if (statusImage != null)
        {
            Sprite sprite = null;
            if (!owned)
                sprite = lockedSprite;
            else if (isEquipped)
                sprite = equippedSprite;
            else
                sprite = ownedSprite;

            statusImage.enabled = sprite != null;
            if (sprite != null)
                statusImage.sprite = sprite;
        }

        if (statusLabel != null)
        {
            bool hideText = hideStatusTextWhenSprite && statusImage != null && statusImage.enabled;
            statusLabel.gameObject.SetActive(!hideText);
        }
    }

    void UpdateApplyVisual(bool owned, bool isEquipped)
    {
        if (applyButtonLabel != null)
            applyButtonLabel.text = owned && isEquipped ? equippedText : applyText;

        if (applyButtonImage != null)
        {
            Sprite sprite = null;
            if (!owned)
                sprite = applyLockedSprite;
            else if (isEquipped)
                sprite = applyEquippedSprite;
            else
                sprite = applySprite;

            applyButtonImage.enabled = sprite != null;
            if (sprite != null)
                applyButtonImage.sprite = sprite;
        }

        if (applyButtonLabel != null)
        {
            bool hideText = hideApplyTextWhenSprite && applyButtonImage != null && applyButtonImage.enabled;
            applyButtonLabel.gameObject.SetActive(!hideText);
        }
    }

    void RefreshUI()
    {
        if (catalog == null || catalog.skins == null || catalog.skins.Length == 0)
        {
            if (previewImage != null)
                previewImage.enabled = false;
            UpdateStatusVisual(false, false);
            if (lockedRoot != null)
                lockedRoot.SetActive(useLockedRoot);
            if (applyButton != null)
                applyButton.interactable = false;
            UpdateApplyVisual(false, false);
            return;
        }

        var entry = catalog.skins[currentIndex];
        string skinId = SkinStorage.GetSkinId(entry, currentIndex);
        string defaultId = catalog.GetDefaultSkinId();
        string equippedId = SkinStorage.GetEquippedSkinId(defaultId, catalog.equippedKey);
        bool owned = SkinStorage.IsUnlocked(skinId, defaultId, catalog.unlockPrefix);
        bool isEquipped = skinId == equippedId;

        if (previewImage != null)
        {
            Sprite preview = null;
            if (previewSprites != null && currentIndex >= 0 && currentIndex < previewSprites.Length)
                preview = previewSprites[currentIndex];
            if (preview == null && entry != null)
                preview = entry.sprite;

            previewImage.enabled = preview != null;
            previewImage.sprite = preview;
            if (preview != null)
                previewImage.color = (!owned && tintPreviewWhenLocked) ? lockedPreviewTint : Color.white;
        }

        if (lockedRoot != null)
            lockedRoot.SetActive(useLockedRoot && !owned);

        UpdateStatusVisual(owned, isEquipped);

        if (applyButton != null)
            applyButton.interactable = owned && !isEquipped;

        UpdateApplyVisual(owned, isEquipped);
    }
}
