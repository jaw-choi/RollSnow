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
    [Header("Zoom")]
    [SerializeField] private float startZoomValue = 5f;
    [SerializeField] private float maxZoomValue = 10f;
    [SerializeField] private float zoomOutSpeed = 0.5f;

    Camera targetCamera;
    float currentZoomValue;

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (player == null)
            player = FindObjectOfType<Player>();

        currentZoomValue = Mathf.Max(0.01f, startZoomValue);
        ApplyZoom(currentZoomValue);
    }

    void LateUpdate()
    {
        if (player == null || targetCamera == null) return;

        UpdateZoom(Time.deltaTime);

        Vector3 desiredPosition = player.transform.position + followOffset;
        targetCamera.transform.position = desiredPosition;
    }

    public void SnapToPlayerImmediately()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
        if (player == null || targetCamera == null) return;

        ApplyZoom(currentZoomValue);
        targetCamera.transform.position = player.transform.position + followOffset;
    }

    void UpdateZoom(float deltaTime)
    {
        if (zoomOutSpeed <= 0f)
            return;

        float clampedMax = Mathf.Max(0.01f, maxZoomValue);
        currentZoomValue = Mathf.MoveTowards(currentZoomValue, clampedMax, zoomOutSpeed * deltaTime);
        ApplyZoom(currentZoomValue);
    }

    void ApplyZoom(float value)
    {
        if (targetCamera == null)
            return;

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Max(0.01f, value);
        }
        else
        {
            targetCamera.fieldOfView = Mathf.Clamp(value, 1f, 179f);
        }
    }
}
