using UnityEngine;

/// <summary>
/// Scrolls the world upward once the player falls below a trigger height.
/// Scroll speed accelerates over time until reaching the optional cap.
/// </summary>
public class WorldScroller : MonoBehaviour
{
    [Header("Scroll Speed")]
    [Tooltip("Initial upward speed in units per second")]
    [Min(0f)]
    public float initialSpeed = 4f;

    [Tooltip("Speed gained each second while scrolling")]
    public float speedIncreasePerSecond = 0.5f;

    [Tooltip("Maximum speed (<= 0 means unlimited)")]
    public float maxSpeed = 0f;

    [Header("Trigger")]
    [Tooltip("Player transform that enables scrolling when it falls below triggerY")]
    public Transform player;

    [Tooltip("World-space Y value the player must fall to start scrolling")]
    public float triggerY = 1.2f;

    float currentSpeed;
    bool hasStarted;

    void Awake()
    {
        currentSpeed = Mathf.Max(0f, initialSpeed);
    }

    void Update()
    {
        if (!hasStarted && ShouldStartScrolling())
        {
            hasStarted = true;
        }

        if (!hasStarted) return;

        Accelerate(Time.deltaTime);
        transform.position += Vector3.up * (currentSpeed * Time.deltaTime);
    }

    bool ShouldStartScrolling()
    {
        if (player == null) return true;
        return player.position.y <= triggerY;
    }

    void Accelerate(float deltaTime)
    {
        if (speedIncreasePerSecond <= 0f) return;

        currentSpeed += speedIncreasePerSecond * deltaTime;
        if (maxSpeed > 0f)
        {
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }
    }

    public void ResetScrollState(float targetY = 0f)
    {
        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;

        currentSpeed = Mathf.Max(0f, initialSpeed);
        hasStarted = false;
    }
}
