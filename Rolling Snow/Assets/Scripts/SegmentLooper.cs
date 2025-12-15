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

    // 한 세그먼트당 장애물 1개를 붙여두고 재활용할 때 교체
    Dictionary<Transform, GameObject> segObstacle = new Dictionary<Transform, GameObject>();

    float lowestY;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        lowestY = segments[0].position.y;
        for (int i = 0; i < segments.Count; i++)
        {
            lowestY = Mathf.Min(lowestY, segments[i].position.y);
            SpawnOneObstacleOnSegment(segments[i]);
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

    void SpawnOneObstacleOnSegment(Transform seg)
    {
        var obs = obstaclePool.Get();
        obs.transform.SetParent(obstaclesRoot);

        // “세그먼트 중앙 근처”에 하나 배치 + X는 랜덤(좌/우 레인)
        float[] lanes = { -2f, 0f, 2f };
        float x = lanes[Random.Range(0, lanes.Length)];
        float y = seg.position.y - (segmentHeight * 0.5f); // 세그먼트 안쪽(대충 중앙)

        obs.transform.position = new Vector3(x, y, 0f);

        segObstacle[seg] = obs;
    }

    void RespawnObstacleOnSegment(Transform seg)
    {
        if (segObstacle.TryGetValue(seg, out var oldObs) && oldObs != null)
        {
            obstaclePool.Release(oldObs);
            segObstacle.Remove(seg);
        }

        SpawnOneObstacleOnSegment(seg);
    }
}
