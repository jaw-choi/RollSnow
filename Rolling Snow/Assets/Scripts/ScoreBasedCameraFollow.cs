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
    [SerializeField] private float followLerpSpeed = 10f;
    [Header("Dead Zone")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(1f, 1f);
    [SerializeField] private bool applyDeadZoneY = true;
    [Header("Segment Clamp")]
    [SerializeField] private WorldScroller segmentProvider;
    [SerializeField] private float boundaryPadding = 0.1f;
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

        Vector3 desiredPosition = GetDeadZoneAdjustedPosition(Time.deltaTime);
        desiredPosition = ClampToSegmentBounds(desiredPosition);

        Transform camTransform = targetCamera.transform;
        if (followLerpSpeed <= 0f)
        {
            camTransform.position = desiredPosition;
        }
        else
        {
            float t = Mathf.Clamp01(followLerpSpeed * Time.deltaTime);
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPosition, t);
        }
    }

    public void SnapToPlayerImmediately()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
        if (player == null || targetCamera == null) return;

        ApplyZoom(currentZoomValue);
        targetCamera.transform.position = player.transform.position + followOffset;
    }

    Vector3 GetDeadZoneAdjustedPosition(float deltaTime)
    {
        Vector3 targetPos = player.transform.position + followOffset;
        Vector3 current = targetCamera.transform.position;
        Vector2 halfDeadZone = new Vector2(Mathf.Max(0.01f, deadZoneSize.x) * 0.5f,
            Mathf.Max(0.01f, deadZoneSize.y) * 0.5f);

        Vector3 adjusted = current;
        float deltaX = targetPos.x - current.x;
        if (Mathf.Abs(deltaX) > halfDeadZone.x)
        {
            float extra = deltaX - Mathf.Sign(deltaX) * halfDeadZone.x;
            adjusted.x += extra;
        }

        if (applyDeadZoneY)
        {
            float deltaY = targetPos.y - current.y;
            if (Mathf.Abs(deltaY) > halfDeadZone.y)
            {
                float extraY = deltaY - Mathf.Sign(deltaY) * halfDeadZone.y;
                adjusted.y += extraY;
            }
        }
        else
        {
            adjusted.y = targetPos.y;
        }

        adjusted.z = targetPos.z;

        return adjusted;
    }

    Vector3 ClampToSegmentBounds(Vector3 desired)
    {
        if (segmentProvider == null || targetCamera == null)
        {
            return desired;
        }

        if (!segmentProvider.TryGetSegmentHorizontalBounds(desired.y, out float minX, out float maxX))
        {
            return desired;
        }

        float halfWidth = GetCameraHalfWidth();
        float clampedMin = minX + halfWidth + boundaryPadding;
        float clampedMax = maxX - halfWidth - boundaryPadding;

        if (clampedMin > clampedMax)
        {
            float center = (minX + maxX) * 0.5f;
            desired.x = center;
        }
        else
        {
            desired.x = Mathf.Clamp(desired.x, clampedMin, clampedMax);
        }

        return desired;
    }

    float GetCameraHalfWidth()
    {
        if (targetCamera == null)
            return 5f;

        if (targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * targetCamera.aspect;
        }

        Vector3 camPos = targetCamera.transform.position;
        float referenceZ = player != null ? player.transform.position.z : camPos.z + 1f;
        float distance = Mathf.Abs(referenceZ - camPos.z);
        if (distance <= Mathf.Epsilon)
            distance = 1f;

        float halfAngle = targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(halfAngle) * distance;
        return halfHeight * targetCamera.aspect;
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
