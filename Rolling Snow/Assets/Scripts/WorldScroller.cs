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

    [Header("Segment Recycling")]
    [Tooltip("Parent transform that contains the repeating segments (defaults to this transform)")]
    [SerializeField] private Transform segmentParent;
    [Tooltip("Height of one segment (used when repositioning to the bottom)")]
    [Min(0.01f)]
    public float segmentHeight = 10f;
    [Tooltip("World-space Y value that triggers recycling; each recycle raises this by segmentHeight")]
    public float recycleThresholdY = 30f;

    float currentSpeed;
    bool hasStarted;
    Transform[] segments;
    Vector3[] segmentStartPositions;
    float initialParentY;
    float nextRecycleThreshold;

    void Awake()
    {
        currentSpeed = Mathf.Max(0f, initialSpeed);
        if (segmentParent == null)
            segmentParent = transform;

        CacheSegments();
        initialParentY = segmentParent != null ? segmentParent.position.y : 0f;
        nextRecycleThreshold = recycleThresholdY;
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
        HandleSegmentRecycling();
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

        RestoreSegments();
        if (segmentParent != null)
        {
            Vector3 parentPos = segmentParent.position;
            parentPos.y = initialParentY;
            segmentParent.position = parentPos;
        }
        nextRecycleThreshold = recycleThresholdY;
    }

    void CacheSegments()
    {
        if (segmentParent == null) return;

        int childCount = segmentParent.childCount;
        if (childCount == 0) return;

        segments = new Transform[childCount];
        segmentStartPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            Transform child = segmentParent.GetChild(i);
            segments[i] = child;
            segmentStartPositions[i] = child.position;
        }
    }

    void RestoreSegments()
    {
        if (segments == null || segmentStartPositions == null) return;
        for (int i = 0; i < segments.Length && i < segmentStartPositions.Length; i++)
        {
            if (segments[i] != null)
                segments[i].position = segmentStartPositions[i];
        }
    }

    void HandleSegmentRecycling()
    {
        if (segmentParent == null || segments == null || segments.Length == 0)
            return;

        if (segmentParent.position.y < nextRecycleThreshold)
            return;

        Transform highest = FindHighestSegment();
        if (highest == null)
            return;

        float lowestY = FindLowestSegmentY();
        float height = Mathf.Max(0.01f, segmentHeight);
        Vector3 pos = highest.position;
        pos.y = lowestY - height;
        highest.position = pos;

        nextRecycleThreshold += height;
    }

    Transform FindHighestSegment()
    {
        Transform result = null;
        float highestY = float.MinValue;
        foreach (var seg in segments)
        {
            if (seg == null) continue;
            float y = seg.position.y;
            if (y > highestY)
            {
                highestY = y;
                result = seg;
            }
        }
        return result;
    }

    float FindLowestSegmentY()
    {
        float lowest = float.MaxValue;
        foreach (var seg in segments)
        {
            if (seg == null) continue;
            float y = seg.position.y;
            if (y < lowest)
                lowest = y;
        }
        return lowest == float.MaxValue ? 0f : lowest;
    }
}
