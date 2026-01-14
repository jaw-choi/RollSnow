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

        if (playSfx && AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.GetItem);
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

        if (controller != null)
            controller.ApplySpeedMultiplier(speedMultiplier, speedDuration);
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
            if (pickupEffectLifetime > 0f)
                Destroy(effect, pickupEffectLifetime);
        }

        if (onPickedUp != null)
            onPickedUp.Invoke();
    }
}
