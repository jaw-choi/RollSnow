using System.Collections.Generic;
using UnityEngine;

public class TrailLineDrawer : MonoBehaviour
{
    public Transform player;        // 공
    public Transform worldRoot;     // WorldRoot
    public LineRenderer line;

    public float interval = 0.03f;  // 점(버텍스) 찍는 간격
    public float xSmooth = 0.08f;   // 곡선을 부드럽게(작을수록 즉각, 클수록 완만)
    public float keepLength = 25f;  // 너무 길어지면 오래된 점 삭제(로컬 y 기준)

    float timer;
    float smoothX;
    readonly List<Vector3> points = new List<Vector3>();

    void Reset()
    {
        line = GetComponent<LineRenderer>();
    }

    void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
    }

    void Update()
    {
        if (player == null || worldRoot == null || line == null) return;

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;

        // 플레이어의 현재 위치를 WorldRoot 로컬 좌표로 변환
        Vector3 local = worldRoot.InverseTransformPoint(player.position);

        // X만 살짝 부드럽게(곡선 느낌)
        smoothX = Mathf.Lerp(smoothX, local.x, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, xSmooth)));
        local.x = smoothX;

        points.Add(local);

        // 너무 오래된 점(화면 위쪽으로 많이 올라간 것) 삭제
        // (worldRoot가 위로 올라가면, player의 local.y는 점점 작아지는 편이라
        //  "처음 찍힌 점"이 상대적으로 local.y가 큰 쪽에 남게 됩니다)
        float newestY = points[points.Count - 1].y;
        while (points.Count > 2 && points[0].y > newestY + keepLength)
        {
            points.RemoveAt(0);
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }
}
