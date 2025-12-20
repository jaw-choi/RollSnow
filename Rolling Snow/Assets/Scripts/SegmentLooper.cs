using System.Collections.Generic;
using UnityEngine;

public class SegmentLooper : MonoBehaviour
{
    [Header("Scene References")]
    public Camera cam;
    public ObstaclePool obstaclePool;
    [SerializeField] private Transform obstaclesRoot;

    [Header("Segments")]
    [SerializeField] private List<SegmentLayout> segmentLayouts = new List<SegmentLayout>();

    readonly List<SegmentLayout> activeLayouts = new List<SegmentLayout>();
    readonly Dictionary<SegmentLayout, List<GameObject>> layoutObstacles = new Dictionary<SegmentLayout, List<GameObject>>();

    void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void Start()
    {
        BuildActiveLayoutList();

        if (activeLayouts.Count == 0 || obstaclePool == null)
        {
            Debug.LogWarning($"{nameof(SegmentLooper)} disabled: missing segments or obstacle pool.", this);
            enabled = false;
            return;
        }

        foreach (var layout in activeLayouts)
        {
            if (layout.segment == null) continue;

            PlaceObstacles(layout);
        }

        AlignSegmentsToCameraTop();
    }

    void Update()
    {
        if (cam == null) return;

        float camTop = GetCameraTopY();
        float camBottom = GetCameraBottomY();

        foreach (var layout in activeLayouts)
        {
            if (layout.segment == null) continue;

            if (layout.GetWorldBottomY() >= camBottom)
            {
                layout.MoveTopTo(camTop);
                RespawnLayout(layout);
            }
        }
    }

    void BuildActiveLayoutList()
    {
        activeLayouts.Clear();
        foreach (var layout in segmentLayouts)
        {
            if (layout != null && layout.segment != null)
            {
                layout.CacheBounds();
                activeLayouts.Add(layout);
            }
        }

        activeLayouts.Sort((a, b) => b.GetWorldTopY().CompareTo(a.GetWorldTopY()));
    }

    void AlignSegmentsToCameraTop()
    {
        if (cam == null) return;

        float currentTop = GetCameraTopY();
        foreach (var layout in activeLayouts)
        {
            if (layout.segment == null) continue;

            layout.MoveTopTo(currentTop);
            currentTop = layout.GetWorldBottomY();
        }
    }

    void RespawnLayout(SegmentLayout layout)
    {
        ReleaseObstacles(layout);
        PlaceObstacles(layout);
    }

    void ReleaseObstacles(SegmentLayout layout)
    {
        if (layout == null) return;

        if (layoutObstacles.TryGetValue(layout, out var spawned))
        {
            foreach (var obj in spawned)
            {
                if (obj != null)
                {
                    obstaclePool.Release(obj);
                }
            }

            layoutObstacles.Remove(layout);
        }
    }

    void PlaceObstacles(SegmentLayout layout)
    {
        if (layout == null || layout.segment == null || obstaclePool == null) return;

        var placements = layout.obstaclePositions;
        var spawned = new List<GameObject>();

        if (placements != null)
        {
            foreach (var placement in placements)
            {
                var obstacle = obstaclePool.Get();
                if (obstacle == null) break;

                Transform parent = obstaclesRoot != null ? obstaclesRoot : layout.segment;
                obstacle.transform.SetParent(parent, false);

                Vector3 local = new Vector3(placement.x, placement.y, 0f);
                Vector3 worldPos = layout.segment.TransformPoint(local);
                obstacle.transform.position = worldPos;

                obstaclePool.ApplyVariation(obstacle.transform);
                spawned.Add(obstacle);
            }
        }

        layoutObstacles[layout] = spawned;
    }

    float GetCameraTopY()
    {
        if (cam == null) return 0f;
        return cam.transform.position.y + GetCameraHalfHeight();
    }

    float GetCameraBottomY()
    {
        if (cam == null) return 0f;
        return cam.transform.position.y - GetCameraHalfHeight();
    }

    float GetCameraHalfHeight()
    {
        if (cam == null) return 5f;
        if (cam.orthographic) return Mathf.Max(0.01f, cam.orthographicSize);

        float distance = 10f;
        foreach (var layout in activeLayouts)
        {
            if (layout.segment != null)
            {
                distance = Mathf.Abs(cam.transform.position.z - layout.segment.position.z);
                break;
            }
        }
        float halfAngle = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        return Mathf.Max(0.01f, Mathf.Tan(halfAngle) * distance);
    }

    [System.Serializable]
    public class SegmentLayout
    {
        public Transform segment;
        public List<Vector2> obstaclePositions = new List<Vector2>();

        Vector3 localTopPoint;
        Vector3 localBottomPoint;
        float cachedHeight;
        bool boundsCached;

        public void CacheBounds()
        {
            boundsCached = true;
            cachedHeight = 0f;

            if (segment == null)
                return;

            if (TryGetSegmentBounds(segment, out var bounds))
            {
                Vector3 topWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                Vector3 bottomWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                localTopPoint = segment.InverseTransformPoint(topWorld);
                localBottomPoint = segment.InverseTransformPoint(bottomWorld);
                cachedHeight = Mathf.Abs(localTopPoint.y - localBottomPoint.y);
            }
            else
            {
                localTopPoint = Vector3.up * 0.5f;
                localBottomPoint = Vector3.down * 0.5f;
                cachedHeight = Mathf.Abs(segment.localScale.y);
            }

            if (cachedHeight <= 0f)
                cachedHeight = 1f;
        }

        public float GetHeight()
        {
            if (!boundsCached)
                CacheBounds();

            return cachedHeight;
        }

        public float GetWorldTopY()
        {
            if (segment == null)
                return float.MinValue;

            if (!boundsCached)
                CacheBounds();

            return segment.TransformPoint(localTopPoint).y;
        }

        public float GetWorldBottomY()
        {
            if (segment == null)
                return float.MaxValue;

            if (!boundsCached)
                CacheBounds();

            return segment.TransformPoint(localBottomPoint).y;
        }

        public void MoveTopTo(float targetTopY)
        {
            if (segment == null)
                return;

            if (!boundsCached)
                CacheBounds();

            float currentTop = GetWorldTopY();
            float delta = targetTopY - currentTop;
            segment.position += new Vector3(0f, delta, 0f);
        }
    }

    static bool TryGetSegmentBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        var renderers = root.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
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

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }
}
