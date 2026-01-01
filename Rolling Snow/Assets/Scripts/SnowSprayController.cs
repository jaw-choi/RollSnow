using UnityEngine;

public class SnowSprayController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform target;               // 기준이 되는 플레이어 Transform
    [Header("Particle Setup")]
    [SerializeField] private ParticleSystem snowPs;          // ?????? ?? ?????,
    [SerializeField] private GameObject snowParticlePrefab;  // ???? ?? ?? ??
    [SerializeField] private bool parentPrefabToEmitter = true;
    [SerializeField] private Transform emitPoint;            // 스키 접지점 (플레이어 Transform 미지정 시 본인 transform 사용)

    [Header("Tuning")]
    [SerializeField] private float minHorizontalSpeed = 0.5f;    // ignore tiny jitters
    [SerializeField] private int burstCountMin = 4;
    [SerializeField] private int burstCountMax = 18;
    [SerializeField] private float burstInterval = 0.05f;        // limit spam

    private Vector2 prevVel;
    private Vector3 prevPosition;
    private float burstTimer;

    private void Reset()
    {
        target = transform;
    }

    private void Awake()
    {
        if (target == null)
            target = transform;
        CreateParticleInstanceIfNeeded();
        prevPosition = target.position;
        prevVel = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (target == null || snowPs == null) return;

        float deltaTime = Time.fixedDeltaTime;
        if (deltaTime <= 0f)
            return;

        Vector3 currentPos = target.position;
        Vector3 frameDelta = currentPos - prevPosition;
        Vector2 v = new Vector2(frameDelta.x, frameDelta.y) / deltaTime;
        float speedSqr = v.sqrMagnitude;

        // Move emitter to contact point
        if (emitPoint != null)
            snowPs.transform.position = emitPoint.position;
        else if (target != null)
            snowPs.transform.position = target.position;
        else
            snowPs.transform.position = transform.position;

        // Rotate emitter to match velocity so 분사 방향이 기존과 반대로 보임
        if (speedSqr > 0.000001f)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            snowPs.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        burstTimer -= Time.fixedDeltaTime;

        bool reversedHorizontalDir = false;
        float prevX = prevVel.x;
        float currX = v.x;
        if (Mathf.Abs(prevX) >= minHorizontalSpeed && Mathf.Abs(currX) >= minHorizontalSpeed)
        {
            reversedHorizontalDir = Mathf.Sign(prevX) != Mathf.Sign(currX);
        }

        // Emit only when horizontal travel direction flips (left <-> right)
        if (reversedHorizontalDir && burstTimer <= 0f)
        {
            int count = Random.Range(burstCountMin, burstCountMax + 1);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Emitting snow burst (direction flip). Count: " + count);
            #endif
            snowPs.Emit(count);
            burstTimer = burstInterval;
        }

        prevPosition = currentPos;
        prevVel = v;
    }

    public void EmitManualBurst(int overrideCount = -1)
    {
        if (snowPs == null)
            return;

        int min = Mathf.Max(1, burstCountMin);
        int max = Mathf.Max(min, burstCountMax);
        int count = overrideCount > 0 ? overrideCount : Random.Range(min, max + 1);
        snowPs.Emit(count);
        burstTimer = burstInterval;
    }

    void CreateParticleInstanceIfNeeded()
    {
        if (snowPs != null || snowParticlePrefab == null)
            return;

        Transform anchor = emitPoint != null ? emitPoint : (target != null ? target : transform);
        Vector3 spawnPos = anchor != null ? anchor.position : transform.position;
        Quaternion spawnRot = anchor != null ? anchor.rotation : transform.rotation;

        Transform parent = parentPrefabToEmitter ? anchor : null;
        GameObject instance = Instantiate(snowParticlePrefab, spawnPos, spawnRot, parent);
        if (instance == null)
            return;

        snowPs = instance.GetComponentInChildren<ParticleSystem>();
        if (snowPs == null)
            snowPs = instance.GetComponent<ParticleSystem>();
    }
}
