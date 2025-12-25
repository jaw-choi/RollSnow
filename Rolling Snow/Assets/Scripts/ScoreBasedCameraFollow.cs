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

        Vector3 targetPos = player.transform.position + followOffset;
        Transform camTransform = targetCamera.transform;
        if (followLerpSpeed <= 0f)
        {
            camTransform.position = targetPos;
        }
        else
        {
            float t = Mathf.Clamp01(followLerpSpeed * Time.deltaTime);
            camTransform.position = Vector3.Lerp(camTransform.position, targetPos, t);
        }
    }
}
