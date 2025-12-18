using UnityEngine;

public class ObstacleBehavior : MonoBehaviour
{
    [Header("Behavior Type")]
    public bool alwaysLethal = true; // 항상 충돌 시 게임오버인지 여부
    public bool shrinkOverTime = false; // 시간이 지나며 크기가 줄어드는 타입인지 여부

    [Header("Shrink Settings")]
    public float startScale = 5f; // 시작 크기
    public float endScale = 1f; // 최소 크기
    public float shrinkDuration = 10f; // startScale -> endScale 까지 걸리는 시간
    public float safeScale = 3f; // 이 크기 이하에서는 충돌해도 게임오버되지 않음

    float spawnTime;

    void OnEnable()
    {
        // 풀에서 재사용될 때마다 초기 상태로 되돌림
        transform.localScale = Vector3.one * startScale;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (!shrinkOverTime) return;

        // 시간이 지날수록 점점 작아지도록 스케일 보간
        float t = (shrinkDuration <= 0f) ? 1f : Mathf.Clamp01((Time.time - spawnTime) / shrinkDuration);
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;
    }

    public bool IsLethal()
    {
        // 항상 치명적인 장애물
        if (alwaysLethal) return true;

        // 크기 감소 타입은 일정 크기 이하에서는 안전한 장애물로 판단
        if (shrinkOverTime)
            return transform.localScale.x > safeScale;

        // 기본값은 치명적 처리
        return true;
    }
}
