using UnityEngine;

/// <summary>
/// Always keeps the camera aligned with the player.
/// Optional offsets allow adjusting the camera's relative position.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScoreBasedCameraFollow : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1f, -10f);
    Camera targetCamera;

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (player == null)
            player = FindObjectOfType<Player>();

    }

    void LateUpdate()
    {
        if (player == null || targetCamera == null) return;

        Vector3 desiredPosition = player.transform.position + followOffset;
        targetCamera.transform.position = desiredPosition;
    }

    public void SnapToPlayerImmediately()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
        if (player == null || targetCamera == null) return;

        targetCamera.transform.position = player.transform.position + followOffset;
    }
}
