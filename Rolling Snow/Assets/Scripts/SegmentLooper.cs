using System.Collections.Generic;
using UnityEngine;

public class SegmentLooper : MonoBehaviour
{
    public Camera cam;
    public float segmentHeight = 10f;
    public float recycleOffset = 12f; // camera above threshold
    [SerializeField] private Transform segment; // single segment to recycle
    [SerializeField] private List<Transform> segments; // optional legacy list (uses first item)
    public ObstaclePool obstaclePool;

    public Transform worldRoot;   // WorldRoot
    public Transform obstaclesRoot; // Obstacles

    Dictionary<Transform, List<GameObject>> segObstacle = new Dictionary<Transform, List<GameObject>>();

    [Tooltip("Lane X positions (e.g. -2,0,2)")]
    public float[] lanes = new float[] { -2f, 0f, 2f };

    [Tooltip("Base number of obstacles per segment (will scale with difficulty)")]
    public int baseObstaclesPerSegment = 1;

    [Tooltip("Seconds for difficulty to increase by one obstacle")]
    public float secondsPerDifficultyStep = 20f;

    // keep a safe lane index to guarantee a continuous path; it can change occasionally
    int safeLaneIndex = 1; // start at middle lane by default

    float lowestY;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (segment == null && segments != null && segments.Count > 0)
        {
            segment = segments[0];
        }

        if (segment == null)
        {
            enabled = false;
            return;
        }

        lowestY = segment.position.y;
        SpawnObstaclesOnSegment(segment);
    }

    void Update()
    {
        if (segment == null || cam == null) return;

        float camY = cam.transform.position.y;
        var seg = segment;

        if (seg.position.y > camY + recycleOffset)
        {
            float newY = lowestY - segmentHeight;
            seg.position = new Vector3(seg.position.x, newY, seg.position.z);
            lowestY = newY;

            RespawnObstacleOnSegment(seg);
        }
    }

    void SpawnObstaclesOnSegment(Transform seg)
    {
        // determine difficulty-scaled obstacle count but always leave at least one free lane
        int maxObstacles = Mathf.Max(0, lanes.Length - 1);
        int add = 0;
        if (GameManager.Instance != null)
            add = Mathf.FloorToInt(GameManager.Instance.score / secondsPerDifficultyStep);

        int desired = Mathf.Clamp(baseObstaclesPerSegment + add, 0, maxObstacles);

        var list = new List<GameObject>();

        // decide safe lane; occasionally shift it depending on difficulty
        if (Random.value < 0.2f + (add * 0.02f)) // small chance to switch safe lane, grows with difficulty
        {
            safeLaneIndex = Random.Range(0, lanes.Length);
        }

        // choose lanes to place obstacles in: pick `desired` lanes from lanes excluding safeLaneIndex
        var candidateIndices = new List<int>();
        for (int i = 0; i < lanes.Length; i++) if (i != safeLaneIndex) candidateIndices.Add(i);

        for (int i = 0; i < desired && candidateIndices.Count > 0; i++)
        {
            int pick = Random.Range(0, candidateIndices.Count);
            int laneIndex = candidateIndices[pick];
            candidateIndices.RemoveAt(pick);

            var obs = obstaclePool.Get();
            obs.transform.SetParent(obstaclesRoot);
            float x = lanes[laneIndex];
            float y = seg.position.y - (segmentHeight * 0.5f);
            obs.transform.position = new Vector3(x, y, 0f);
            list.Add(obs);
        }

        // store list (may be empty if desired was 0)
        segObstacle[seg] = list;
    }

    void RespawnObstacleOnSegment(Transform seg)
    {
        if (segObstacle.TryGetValue(seg, out var oldList) && oldList != null)
        {
            foreach (var o in oldList)
            {
                if (o != null) obstaclePool.Release(o);
            }
            segObstacle.Remove(seg);
        }

        SpawnObstaclesOnSegment(seg);
    }
}
