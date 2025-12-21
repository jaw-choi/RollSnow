using UnityEngine;

/// <summary>
/// Gradually zooms the camera out once the score passes a threshold.
/// The player receives the inverse scale so it keeps the same on-screen size.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScoreBasedCameraZoom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Player player;

    [Header("Score Thresholds")]
    [Tooltip("Score required before zooming starts")]
    [Min(0f)] [SerializeField] private float zoomStartScore = 50f;
    [Tooltip("Score where the zoom reaches its maximum")]
    [Min(0f)] [SerializeField] private float maxZoomScore = 200f;

    [Header("Zoom Settings")]
    [Tooltip("Target FOV once fully zoomed (ignored for orthographic cameras)")]
    [SerializeField] private float targetFieldOfView = 80f;
    [Tooltip("Target orthographic size once fully zoomed (only used for orthographic cameras)")]
    [SerializeField] private float targetOrthographicSize = 10f;
    [Tooltip("Interpolation speed for the camera values")]
    [SerializeField] private float zoomLerpSpeed = 4f;

    float defaultFov;
    float defaultOrthoSize;
    float currentBlend;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        if (player == null)
            player = FindObjectOfType<Player>();

        if (targetCamera != null)
        {
            defaultFov = targetCamera.fieldOfView;
            defaultOrthoSize = targetCamera.orthographicSize;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null || GameManager.Instance == null) return;

        float score = GameManager.Instance.score;
        float desiredBlend = 0f;
        if (maxZoomScore > zoomStartScore)
            desiredBlend = Mathf.InverseLerp(zoomStartScore, maxZoomScore, score);
        else if (score >= zoomStartScore)
            desiredBlend = 1f;

        currentBlend = Mathf.MoveTowards(currentBlend, desiredBlend, zoomLerpSpeed * Time.deltaTime);
        ApplyZoom(currentBlend);
    }

    void ApplyZoom(float blend)
    {
        if (targetCamera.orthographic)
        {
            float desired = Mathf.Lerp(defaultOrthoSize, targetOrthographicSize, blend);
            targetCamera.orthographicSize = desired;
            float ratio = defaultOrthoSize > 0f ? desired / defaultOrthoSize : 1f;
            SendScaleMultiplier(ratio);
        }
        else
        {
            float desired = Mathf.Lerp(defaultFov, targetFieldOfView, blend);
            targetCamera.fieldOfView = desired;
            float defaultTan = Mathf.Tan(defaultFov * Mathf.Deg2Rad * 0.5f);
            float desiredTan = Mathf.Tan(desired * Mathf.Deg2Rad * 0.5f);
            float ratio = defaultTan > 0f ? desiredTan / defaultTan : 1f;
            SendScaleMultiplier(ratio);
        }
    }

    void SendScaleMultiplier(float multiplier)
    {
        if (player == null) return;
        player.SetExternalScaleMultiplier(multiplier);
    }
}
