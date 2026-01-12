using UnityEngine;

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

        ApplyEffect(player);

        pickedUp = true;
        if (playSfx && AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.GetItem);
        if (useHaptics)
            Haptics.Tap(hapticCooldown);

        if (disableOnPickup)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
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
}
