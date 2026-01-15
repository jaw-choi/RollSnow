using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        Gold,
        Point,
        SpeedUp,
        SpeedDown
    }

    [Header("Type")]
    [SerializeField] private ItemType itemType = ItemType.Gold;

    [Header("Player Filter")]
    [SerializeField] private string playerTag = "Player";

    [Header("Gold/Points")]
    [SerializeField] private int goldAmount = 1;
    [SerializeField] private float scoreAmount = 1f;

    [Header("Speed")]
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private float speedDuration = 2f;
    [SerializeField] private float speedUpDuration = 1f;
    [SerializeField] private bool grantInvincibilityOnSpeedUp = true;
    [SerializeField] private bool snapSpeedOnSpeedUp = true;
    [SerializeField] private bool snapSpeedOnSpeedDown = true;

    [Header("Feedback")]
    [SerializeField] private bool playSfx = true;
    [SerializeField] private bool useHaptics = true;
    [SerializeField] private float hapticCooldown = 0.12f;

    [Header("Reaction")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private Transform pickupEffectAnchor;
    [SerializeField] private bool spawnEffectOnPlayer = true;
    [SerializeField] private Vector3 pickupEffectOffset;
    [SerializeField] private bool parentEffectToAnchor = false;
    [SerializeField] private float pickupEffectLifetime = 1.2f;
    [SerializeField] private Vector3 pickupEffectFloatOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private UnityEvent onPickedUp;

    [Header("Pickup")]
    [SerializeField] private bool disableOnPickup = true;

    bool pickedUp;

    void OnTriggerEnter(Collider other) => TryPickup(other);
    void OnTriggerEnter2D(Collider2D other) => TryPickup(other);

    void TryPickup(Component other)
    {
        if (pickedUp || other == null)
            return;

        var player = other.GetComponentInParent<Player>();
        if (player == null)
            return;
        if (!string.IsNullOrEmpty(playerTag) && !player.CompareTag(playerTag))
            return;

        pickedUp = true;
        DisablePickupColliders();
        ApplyEffect(player);

        PlayPickupSfx();
        if (useHaptics)
            Haptics.Tap(hapticCooldown);
        PlayPickupReaction(player);

        if (disableOnPickup)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }

    void DisablePickupColliders()
    {
        foreach (var collider3D in GetComponents<Collider>())
            collider3D.enabled = false;
        foreach (var collider2D in GetComponents<Collider2D>())
            collider2D.enabled = false;
    }

    void EnablePickupColliders()
    {
        foreach (var collider3D in GetComponents<Collider>())
            collider3D.enabled = true;
        foreach (var collider2D in GetComponents<Collider2D>())
            collider2D.enabled = true;
    }

    public void ResetPickup(bool restoreActive = true)
    {
        pickedUp = false;
        if (restoreActive && !gameObject.activeSelf)
            gameObject.SetActive(true);
        EnablePickupColliders();
    }

    void ApplyEffect(Player player)
    {
        switch (itemType)
        {
            case ItemType.Gold:
            {
                var gold = GoldSystem.GetOrCreate();
                if (gold != null)
                {
                    var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
                    float multiplier = gm != null ? gm.GetGoldResultMultiplier() : 10f;
                    int amount = Mathf.RoundToInt(Mathf.Max(0, goldAmount) * multiplier);
                    gold.AddGold(Mathf.Max(0, amount));
                    if (gm != null)
                        gm.RegisterGoldItemPickup(goldAmount, amount, 1);
                }
                break;
            }
            case ItemType.Point:
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(scoreAmount);
                    GameManager.Instance.RegisterScoreItemPickup(1);
                }
                break;
            }
            case ItemType.SpeedUp:
            case ItemType.SpeedDown:
            {
                ApplySpeed(player);
                break;
            }
        }
    }

    void ApplySpeed(Player player)
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller == null)
            controller = player.GetComponentInChildren<PlayerController>();

        float duration = itemType == ItemType.SpeedUp ? speedUpDuration : speedDuration;
        if (itemType == ItemType.SpeedUp && duration <= 0f)
            duration = speedDuration;
        if (itemType == ItemType.SpeedDown)
            duration = 0f;

        if (controller != null)
        {
            float appliedMultiplier = speedMultiplier;
            if (itemType == ItemType.SpeedDown)
            {
                if (speedMultiplier > 1f)
                    appliedMultiplier = 1f / Mathf.Max(0.01f, speedMultiplier);
                else
                    appliedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            }

            bool snap = itemType == ItemType.SpeedUp && snapSpeedOnSpeedUp;
            bool snapDown = itemType == ItemType.SpeedDown && snapSpeedOnSpeedDown;
            controller.ApplySpeedMultiplier(appliedMultiplier, duration, snap, snapDown);
        }

        if (itemType == ItemType.SpeedUp && grantInvincibilityOnSpeedUp)
        {
            var invincibility = player.GetComponent<PlayerInvincibility>();
            if (invincibility == null)
                invincibility = player.GetComponentInChildren<PlayerInvincibility>();
            if (invincibility != null)
                invincibility.Activate(duration);
        }
    }

    void PlayPickupSfx()
    {
        if (!playSfx || AudioManager.instance == null)
            return;

        AudioManager.Sfx sfx = itemType == ItemType.SpeedUp
            ? AudioManager.Sfx.SpeedUp
            : AudioManager.Sfx.GetItem;
        AudioManager.instance.PlaySfx(sfx);
    }

    void PlayPickupReaction(Player player)
    {
        if (pickupEffectPrefab != null)
        {
            Transform anchor = pickupEffectAnchor;
            if (anchor == null && spawnEffectOnPlayer && player != null)
                anchor = player.transform;
            if (anchor == null)
                anchor = transform;

            Vector3 position = anchor.TransformPoint(pickupEffectOffset);
            Quaternion rotation = anchor.rotation;
            GameObject effect = Instantiate(pickupEffectPrefab, position, rotation);
            if (parentEffectToAnchor && anchor != transform)
                effect.transform.SetParent(anchor, true);
            var floatEffect = effect.GetComponent<FloatingFadeEffect>();
            if (floatEffect == null)
            {
                floatEffect = effect.AddComponent<FloatingFadeEffect>();
                float fadeDuration = pickupEffectLifetime > 0f ? pickupEffectLifetime : 1f;
                bool destroyOnComplete = pickupEffectLifetime > 0f;
                floatEffect.Configure(pickupEffectFloatOffset, fadeDuration, false, destroyOnComplete);
            }
            if (pickupEffectLifetime > 0f)
                Destroy(effect, pickupEffectLifetime);
        }

        if (onPickedUp != null)
            onPickedUp.Invoke();
    }
}
