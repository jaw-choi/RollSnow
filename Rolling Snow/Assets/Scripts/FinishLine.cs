using UnityEngine;

/// <summary>
/// Detects when the player passes through the finish line and triggers a clear state.
/// Supports both 2D and 3D trigger colliders.
/// </summary>
public class FinishLine : MonoBehaviour
{
    [Tooltip("Tag used to identify the player object")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Disable the finish line once it has been triggered")]
    [SerializeField] private bool disableAfterClear = true;

    bool triggered;

    void OnTriggerEnter(Collider other) => TryClear(other.gameObject);
    void OnTriggerEnter2D(Collider2D other) => TryClear(other.gameObject);

    void TryClear(GameObject other)
    {
        if (triggered) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        if (GameManager.Instance == null) return;

        triggered = true;
        GameManager.Instance.LevelClear();
        if (disableAfterClear)
        {
            gameObject.SetActive(false);
        }
    }
}
