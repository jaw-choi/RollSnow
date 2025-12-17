using System.Collections.Generic;
using UnityEngine;

public class SegmentLooper : MonoBehaviour
{
    public Camera cam;
    public float segmentHeight = 10f;
    public float recycleOffset = 12f; // camera above threshold
    public List<Transform> segments;  // drag Segment_0..3
    public ObstaclePool obstaclePool;

    public Transform worldRoot;   // WorldRoot
    public Transform obstaclesRoot; // Obstacles

    // 한 세그먼트당 재활용되는 장애물들
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

        lowestY = segments[0].position.y;
        for (int i = 0; i < segments.Count; i++)
        {
            lowestY = Mathf.Min(lowestY, segments[i].position.y);
            SpawnObstaclesOnSegment(segments[i]);
        }
    }

    void Update()
    {
        float camY = cam.transform.position.y;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];

            // 세그먼트가 카메라 위로 충분히 올라갔으면(= 지나갔으면) 아래로 재배치
            if (seg.position.y > camY + recycleOffset)
            {
                // 아래로 하나 더 내림
                float newY = lowestY - segmentHeight;
                seg.position = new Vector3(seg.position.x, newY, seg.position.z);
                lowestY = newY;

                // 이 세그먼트의 장애물도 새로 배치
                RespawnObstacleOnSegment(seg);
            }
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
