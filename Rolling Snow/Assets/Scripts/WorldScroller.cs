using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a stack of vertical segments and reuses the one at the very top by
/// teleporting it to the very bottom whenever the follow camera catches up.
/// </summary>
public class WorldScroller : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private Camera followCamera;

    [Header("Segments")]
    [SerializeField] private List<Transform> segments = new List<Transform>();
    [SerializeField] private bool randomizeOrderOnStart = true;
    [SerializeField] private float firstSegmentTopX = 0f;
    [SerializeField] private float firstSegmentTopY = 0f;
    [SerializeField] private float cameraPassThreshold = 0.25f;
    [SerializeField] private float segmentOverlap = 0.1f;

    readonly List<SegmentInfo> orderedSegments = new List<SegmentInfo>();

    void Awake()
    {
        if (followCamera == null)
            followCamera = Camera.main;
    }

    void Start()
    {
        ResetWorld();
    }

    void Update()
    {
        if (followCamera == null || orderedSegments.Count == 0)
            return;

        var leadingSegment = orderedSegments[0];
        float cameraTop = GetCameraTop();
        float segmentBottom = leadingSegment.GetWorldBottomY();

        if (cameraTop <= segmentBottom - cameraPassThreshold)
            MoveLeadingSegmentToTail();
    }

    void BuildSegmentList()
    {
        orderedSegments.Clear();

        if (segments.Count == 0 && segmentsParent != null)
        {
            foreach (Transform child in segmentsParent)
            {
                if (child != null)
                    segments.Add(child);
            }
        }

        foreach (var segment in segments)
        {
            if (segment == null) continue;
            var info = new SegmentInfo(segment);
            info.CacheBounds();
            info.CacheResettable();
            orderedSegments.Add(info);
        }
    }

    public void ResetWorld()
    {
        BuildSegmentList();
        if (orderedSegments.Count == 0)
        {
            Debug.LogWarning($"{nameof(WorldScroller)} disabled: no segments found.", this);
            enabled = false;
            return;
        }

        if (randomizeOrderOnStart)
        {
            Shuffle(orderedSegments);
        }

        ArrangeSegmentsFromOrigin();
    }

    void ArrangeSegmentsFromOrigin()
    {
        float currentTop = firstSegmentTopY;
        float currentTopX = firstSegmentTopX;
        foreach (var info in orderedSegments)
        {
            info.MoveTopTo(currentTop);
            info.AlignX(currentTopX);
            info.ResetContents();
            currentTop = info.GetWorldBottomY() + segmentOverlap;
        }
    }

    void MoveLeadingSegmentToTail()
    {
        if (orderedSegments.Count == 0)
            return;

        var passedSegment = orderedSegments[0];
        orderedSegments.RemoveAt(0);

        float anchorTop = orderedSegments.Count > 0
            ? orderedSegments[orderedSegments.Count - 1].GetWorldBottomY() + segmentOverlap
            : passedSegment.GetWorldBottomY() - passedSegment.GetHeight();

        passedSegment.MoveTopTo(anchorTop);
        passedSegment.AlignX(firstSegmentTopX);
        passedSegment.ResetContents();
        orderedSegments.Add(passedSegment);
    }

    public bool TryGetSegmentHorizontalBounds(float sampleY, out float minX, out float maxX)
    {
        minX = maxX = 0f;
        if (orderedSegments.Count == 0)
            return false;

        SegmentInfo closest = null;
        float closestDistance = float.MaxValue;

        foreach (var info in orderedSegments)
        {
            float top = info.GetWorldTopY();
            float bottom = info.GetWorldBottomY();
            if (sampleY <= top && sampleY >= bottom)
            {
                minX = info.GetWorldLeftX();
                maxX = info.GetWorldRightX();
                return true;
            }

            float center = (top + bottom) * 0.5f;
            float distance = Mathf.Abs(sampleY - center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = info;
            }
        }

        if (closest != null)
        {
            minX = closest.GetWorldLeftX();
            maxX = closest.GetWorldRightX();
            return true;
        }

        return false;
    }

    float GetCameraTop()
    {
        if (followCamera == null)
            return 0f;

        return followCamera.transform.position.y + GetCameraHalfHeight();
    }

    float GetCameraHalfHeight()
    {
        if (followCamera == null)
            return 5f;

        if (followCamera.orthographic)
            return Mathf.Max(0.01f, followCamera.orthographicSize);

        float distance = 10f;
        if (orderedSegments.Count > 0)
        {
            float zDelta = Mathf.Abs(followCamera.transform.position.z - orderedSegments[0].transform.position.z);
            if (zDelta > 0f)
                distance = zDelta;
        }

        float halfAngle = followCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        return Mathf.Max(0.01f, Mathf.Tan(halfAngle) * distance);
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    class SegmentInfo
    {
        public readonly Transform transform;

        Vector3 localTop;
        Vector3 localBottom;
        Vector3 localLeft;
        Vector3 localRight;
        float cachedHeight = 1f;
        bool hasBounds;
        readonly List<SegmentResetEntry> resetEntries = new List<SegmentResetEntry>();
        readonly HashSet<Transform> resetEntryLookup = new HashSet<Transform>();

        public SegmentInfo(Transform transform)
        {
            this.transform = transform;
        }

        public void CacheBounds()
        {
            hasBounds = TryBuildBounds(transform, out var bounds);
            if (hasBounds)
            {
                Vector3 topWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                Vector3 bottomWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                Vector3 leftWorld = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);
                Vector3 rightWorld = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
                localTop = transform.InverseTransformPoint(topWorld);
                localBottom = transform.InverseTransformPoint(bottomWorld);
                localLeft = transform.InverseTransformPoint(leftWorld);
                localRight = transform.InverseTransformPoint(rightWorld);
                cachedHeight = Mathf.Abs(localTop.y - localBottom.y);
            }
            else
            {
                localTop = Vector3.up * 0.5f;
                localBottom = Vector3.down * 0.5f;
                localLeft = Vector3.left * 0.5f;
                localRight = Vector3.right * 0.5f;
                cachedHeight = 1f;
            }
        }

        public float GetWorldTopY()
        {
            if (!hasBounds)
                CacheBounds();

            return transform.TransformPoint(localTop).y;
        }

        public float GetWorldBottomY()
        {
            if (!hasBounds)
                CacheBounds();

            return transform.TransformPoint(localBottom).y;
        }

        public float GetWorldLeftX()
        {
            if (!hasBounds)
                CacheBounds();

            return transform.TransformPoint(localLeft).x;
        }

        public float GetWorldRightX()
        {
            if (!hasBounds)
                CacheBounds();

            return transform.TransformPoint(localRight).x;
        }

        public float GetHeight()
        {
            if (!hasBounds)
                CacheBounds();

            return Mathf.Max(0.01f, cachedHeight);
        }

        public void CacheResettable()
        {
            resetEntries.Clear();
            resetEntryLookup.Clear();

            if (transform == null)
                return;

            var pickups = transform.GetComponentsInChildren<ItemPickup>(true);
            for (int i = 0; i < pickups.Length; i++)
            {
                var pickup = pickups[i];
                if (pickup != null)
                    AddResetEntry(pickup.transform, pickup);
            }

            var allTransforms = transform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var child = allTransforms[i];
                if (child == null || child == transform)
                    continue;
                if (child.CompareTag("Obstacle"))
                    AddResetEntry(child, null);
            }
        }

        public void ResetContents()
        {
            for (int i = 0; i < resetEntries.Count; i++)
            {
                var entry = resetEntries[i];
                if (entry.transform == null)
                    continue;

                entry.transform.localPosition = entry.localPosition;
                entry.transform.localRotation = entry.localRotation;
                entry.transform.localScale = entry.localScale;

                if (entry.pickup != null)
                    entry.pickup.ResetPickup(entry.wasActive);
                else
                    entry.transform.gameObject.SetActive(entry.wasActive);

                var colliders3d = entry.transform.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders3d.Length; c++)
                    colliders3d[c].enabled = true;

                var colliders2d = entry.transform.GetComponentsInChildren<Collider2D>(true);
                for (int c = 0; c < colliders2d.Length; c++)
                    colliders2d[c].enabled = true;

                if (entry.rb3d != null)
                {
                    entry.rb3d.linearVelocity = Vector3.zero;
                    entry.rb3d.angularVelocity = Vector3.zero;
                    entry.rb3d.position = entry.transform.position;
                    entry.rb3d.rotation = entry.transform.rotation;
                }

                if (entry.rb2d != null)
                {
                    entry.rb2d.linearVelocity = Vector2.zero;
                    entry.rb2d.angularVelocity = 0f;
                    entry.rb2d.position = entry.transform.position;
                    entry.rb2d.rotation = entry.transform.eulerAngles.z;
                }
            }
        }

        void AddResetEntry(Transform target, ItemPickup pickup)
        {
            if (target == null || resetEntryLookup.Contains(target))
                return;

            resetEntryLookup.Add(target);
            resetEntries.Add(new SegmentResetEntry
            {
                transform = target,
                localPosition = target.localPosition,
                localRotation = target.localRotation,
                localScale = target.localScale,
                wasActive = target.gameObject.activeSelf,
                pickup = pickup,
                rb3d = target.GetComponent<Rigidbody>(),
                rb2d = target.GetComponent<Rigidbody2D>()
            });
        }

        public void MoveTopTo(float targetTopY)
        {
            if (!hasBounds)
                CacheBounds();

            float currentTop = GetWorldTopY();
            float delta = targetTopY - currentTop;
            transform.position += new Vector3(0f, delta, 0f);
        }

        public void AlignX(float targetX)
        {
            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;
        }

        class SegmentResetEntry
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public bool wasActive;
            public ItemPickup pickup;
            public Rigidbody rb3d;
            public Rigidbody2D rb2d;
        }

        static bool TryBuildBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool hasAny = false;

            var renderers = root.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                    continue;

                if (!hasAny)
                {
                    bounds = renderer.bounds;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            var colliders = root.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                if (!hasAny)
                {
                    bounds = collider.bounds;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return hasAny;
        }
    }
}
