using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1f;
    [Header("Speed Scaling")]
    [SerializeField] private float moveSpeedIncreasePerSecond = 0f;
    [SerializeField] private float maxMoveSpeed = 3f;
    [Header("Horizontal Bounds")]
    [SerializeField] private float minX = -9.49f;
    [SerializeField] private float maxX = 9.49f;

    [Header("Vertical Descent")]
    [Tooltip("Speed at which the player falls until reaching groundY")]
    public float descentSpeed = 2f;
    [Tooltip("Lowest Y position the player can reach")]
    public float groundY = 1.2f;
    [SerializeField] private float downhillSteepnessMultiplier = 1.5f;
    [SerializeField] private float maxDownhillSpeedMultiplier = 1.6f;
    private int moveDir = 1; // start heading right; -1 = left, +1 = right
    int startingMoveDir = 1;

    [Header("Turn Settings")]
    public float tapThreshold = 0.18f; // seconds to consider a tap (quick flip)
    public float slowFlipDuration = 0.3f; // duration of slow flip when long-press
    [SerializeField] private float highSpeedTurnAccelerationMultiplier = 1.6f;
    [SerializeField] private float highSpeedFlipDurationMultiplier = 0.75f;
    [SerializeField] private bool preserveSpeedWhileTurning = true;
    [SerializeField] private bool preserveHorizontalSpeedWhileTurning = true;
    // quickFlipDuration removed ??short taps now use slowFlipDuration (curved behavior)

    // continuous direction value used for movement (-1..1)
    float dirValue = 0f;

    [Header("Ski Physics")]
    [SerializeField] private float downhillAcceleration = 5f;
    [SerializeField] private float horizontalAcceleration = 10f;

    [Header("Turn FX")]
    [SerializeField] private SnowSprayController snowSprayController;
    [SerializeField] private int turnBurstCount = 8;

    // press/flip state
    bool isPressing = false;
    float pressStartTime = 0f;
    bool flipInProgress = false;
    float flipStartValue = 0f;
    float flipTargetValue = 0f;
    float flipProgress = 0f;
    // prevent repeated flips while holding the same press
    bool flipTriggeredThisPress = false;
    float currentFlipDuration = 0.6f;
    float currentMoveSpeed;
    float baseMoveSpeed;
    float initialMoveSpeed;
    Vector3 currentVelocity;
    float speedMultiplier = 1f;
    float speedBaselineMultiplier = 1f;
    float speedBaselineMoveSpeed = 0f;
    bool hasSpeedEffectBaseline = false;
    Coroutine speedEffectRoutine;
    bool boundarySlideActive;
    bool boundaryClampLeftActive;
    bool boundaryClampRightActive;
    float boundaryClampMinX;
    float boundaryClampMaxX;

    Rigidbody rb;
    bool inputModeLogged;
    bool mouseUnavailableLogged;
    bool touchUnavailableLogged;

    public float CurrentSpeed => currentVelocity.magnitude;

#if UNITY_EDITOR
    void OnEnable()
    {
        TouchSimulation.Enable();
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        TouchSimulation.Disable();
    }
#endif

    void Awake()
    {
        initialMoveSpeed = moveSpeed;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // begin with a gentle movement toward bottom-right
        startingMoveDir = moveDir;
        moveDir = startingMoveDir;
        dirValue = moveDir;
        baseMoveSpeed = Mathf.Max(0f, moveSpeed);
        currentMoveSpeed = GetEffectiveBaseSpeed();
        currentVelocity = new Vector3(dirValue * currentMoveSpeed, GetDownhillSpeed(), 0f);
    }

    void Update()
    {
        if (!IsGameplayActive())
            return;

        UpdateMoveSpeed(Time.deltaTime);

        // 1) input: mouse click or touch
        bool pressedDown = false;
        bool released = false;

        if (!inputModeLogged)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("PlayerController input mode init: detecting Mouse/Touch availability.");
#endif
            inputModeLogged = true;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                pressedDown = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                //Debug.Log($"Input detected: Mouse Down @ {Time.time:F2}");
#endif
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                released = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                //Debug.Log($"Input detected: Mouse Up @ {Time.time:F2}");
#endif
            }
        }
        else if (!mouseUnavailableLogged)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Mouse device not found.");
#endif
            mouseUnavailableLogged = true;
        }

        var touch = Touchscreen.current;
        if (touch != null)
        {
            var primary = touch.primaryTouch;
            if (primary.press.wasPressedThisFrame)
            {
                pressedDown = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                //Debug.Log($"Input detected: Touch Down @ {Time.time:F2}");
#endif
            }
            if (primary.press.wasReleasedThisFrame)
            {
                released = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                //Debug.Log($"Input detected: Touch Up @ {Time.time:F2}");
#endif
            }
        }
        else if (!touchUnavailableLogged)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Touchscreen device not found.");
#endif
            touchUnavailableLogged = true;
        }

        // 2) when pressed, start tracking to differentiate tap vs hold.
        if (boundarySlideActive && (pressedDown || released))
        {
            ReleaseBoundarySlideToMap();
            pressedDown = false;
            released = false;
        }

        // Pressed down handling
        if (pressedDown)
        {
            // new press: allow flip for this press
            flipTriggeredThisPress = false;
            isPressing = true;
            pressStartTime = Time.time;
        }

        // Released handling (determine tap vs long press)
        if (released && isPressing)
        {
            // Start flip on release if not already triggered for this press
            if (!flipTriggeredThisPress)
            {
                StartFlip(slowFlipDuration);
            }
            isPressing = false;
        }

        // If holding and reached tapThreshold, begin gradual flip while still holding
        if (isPressing && !flipTriggeredThisPress)
        {
            if (Time.time - pressStartTime >= tapThreshold)
            {
                StartFlip(slowFlipDuration);
            }
        }

        // progress any gradual flip
        if (flipInProgress)
        {
            flipProgress += Time.deltaTime / Mathf.Max(0.0001f, currentFlipDuration);
            dirValue = Mathf.Lerp(flipStartValue, flipTargetValue, Mathf.Clamp01(flipProgress));
            if (flipProgress >= 1f)
            {
                dirValue = flipTargetValue;
                flipInProgress = false;
            }
        }

        if (rb == null)
        {
            ApplyTransformMovement(Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (!IsGameplayActive())
            return;

        if (rb != null)
        {
            ApplyRigidbodyMovement(Time.fixedDeltaTime);
        }
    }

    private void StartFlip(float duration)
    {
        flipStartValue = dirValue;
        moveDir = -moveDir;
        flipTargetValue = moveDir;
        flipProgress = 0f;
        float durationMultiplier = Mathf.Lerp(1f, highSpeedFlipDurationMultiplier, GetSpeedNormalized());
        currentFlipDuration = Mathf.Max(0.0001f, duration * Mathf.Max(0.01f, durationMultiplier));
        flipInProgress = true;
        flipTriggeredThisPress = true;
        EmitTurnBurst();
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Curve);
        //AudioManager.instance.BoostMusic(0.4f, 0.2f);
    }

    void ApplyTransformMovement(float deltaTime)
    {
        SimulateSkiMotion(deltaTime, false);
    }

    void ApplyRigidbodyMovement(float deltaTime)
    {
        SimulateSkiMotion(deltaTime, true);
    }

    void SimulateSkiMotion(float deltaTime, bool useRigidbody)
    {
        if (deltaTime <= 0f)
            return;

        UpdateSkiVelocity(deltaTime);

        Vector3 newPos = useRigidbody && rb != null ? rb.position : transform.position;
        newPos += currentVelocity * deltaTime;

        float minClamp = minX;
        float maxClamp = maxX;
        if (boundaryClampLeftActive)
            minClamp = Mathf.Max(minClamp, boundaryClampMinX);
        if (boundaryClampRightActive)
            maxClamp = Mathf.Min(maxClamp, boundaryClampMaxX);

        if (minClamp < maxClamp)
        {
            float clampedX = Mathf.Clamp(newPos.x, minClamp, maxClamp);
            if (!Mathf.Approximately(clampedX, newPos.x))
                currentVelocity.x = 0f;
            newPos.x = clampedX;
        }

        if (useRigidbody && rb != null)
        {
            rb.MovePosition(newPos);
            rb.linearVelocity = currentVelocity;
        }
        else
        {
            transform.position = newPos;
        }
    }

    void UpdateSkiVelocity(float deltaTime)
    {
        float targetLateral = boundarySlideActive ? 0f : dirValue * currentMoveSpeed;
        float targetDownhill = GetDownhillSpeed();
        if (preserveSpeedWhileTurning && currentMoveSpeed > 0f)
        {
            float baseSpeed = Mathf.Sqrt(targetDownhill * targetDownhill + currentMoveSpeed * currentMoveSpeed);
            float totalSpeed = Mathf.Max(baseSpeed, currentVelocity.magnitude);
            float downhillAbs = Mathf.Sqrt(Mathf.Max(0f, totalSpeed * totalSpeed - targetLateral * targetLateral));
            float downhillSign = targetDownhill >= 0f ? 1f : -1f;
            targetDownhill = downhillSign * downhillAbs;
        }

        currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, targetDownhill, downhillAcceleration * deltaTime);

        float turnAccelMultiplier = Mathf.Lerp(1f, highSpeedTurnAccelerationMultiplier, GetSpeedNormalized());
        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetLateral, horizontalAcceleration * turnAccelMultiplier * deltaTime);
    }

    public void ResetControllerState(Vector3 position, Quaternion rotation)
    {
        moveDir = startingMoveDir;
        dirValue = startingMoveDir;
        flipStartValue = startingMoveDir;
        flipTargetValue = startingMoveDir;
        flipProgress = 0f;
        currentFlipDuration = slowFlipDuration;
        isPressing = false;
        flipInProgress = false;
        flipTriggeredThisPress = false;
        pressStartTime = 0f;
        if (speedEffectRoutine != null)
        {
            StopCoroutine(speedEffectRoutine);
            speedEffectRoutine = null;
        }
        speedMultiplier = 1f;
        hasSpeedEffectBaseline = false;
        speedBaselineMultiplier = 1f;
        speedBaselineMoveSpeed = 0f;
        moveSpeed = initialMoveSpeed;
        baseMoveSpeed = Mathf.Max(0f, moveSpeed);
        currentMoveSpeed = GetEffectiveBaseSpeed();
        currentVelocity = new Vector3(dirValue * currentMoveSpeed, GetDownhillSpeed(), 0f);
        boundarySlideActive = false;
        boundaryClampLeftActive = false;
        boundaryClampRightActive = false;
        boundaryClampMinX = 0f;
        boundaryClampMaxX = 0f;

        if (rb != null)
        {
            rb.linearVelocity = currentVelocity;
            rb.angularVelocity = Vector3.zero;
            Vector3 clamped = position;
            if (minX < maxX)
                clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
            rb.position = clamped;
            rb.rotation = rotation;
        }

        Vector3 safePos = position;
        if (minX < maxX)
            safePos.x = Mathf.Clamp(safePos.x, minX, maxX);
        transform.SetPositionAndRotation(safePos, rotation);
    }

    public void ActivateBoundarySlide(float boundaryX, bool isLeftBoundary)
    {
        boundarySlideActive = true;
        SetBoundaryClamp(boundaryX, isLeftBoundary);
        currentVelocity.x = 0f;
        if (rb != null)
            rb.linearVelocity = currentVelocity;
    }

    public void ClearBoundaryClamp(bool isLeftBoundary)
    {
        if (isLeftBoundary)
            boundaryClampLeftActive = false;
        else
            boundaryClampRightActive = false;

        if (!boundaryClampLeftActive && !boundaryClampRightActive)
            boundarySlideActive = false;
    }

    public bool IsBoundarySliding => boundarySlideActive;
    public bool HasBoundaryClamp => boundaryClampLeftActive || boundaryClampRightActive;

    void ReleaseBoundarySlideToMap()
    {
        if (!boundarySlideActive)
            return;

        boundarySlideActive = false;
        int targetDir = GetBoundaryReturnDir();
        moveDir = targetDir;
        dirValue = targetDir;
        flipInProgress = false;
        flipTriggeredThisPress = true;
    }

    int GetBoundaryReturnDir()
    {
        if (boundaryClampLeftActive && !boundaryClampRightActive)
            return 1;
        if (boundaryClampRightActive && !boundaryClampLeftActive)
            return -1;

        if (minX >= maxX)
            return moveDir;

        float center = (minX + maxX) * 0.5f;
        return transform.position.x <= center ? 1 : -1;
    }

    void SetBoundaryClamp(float boundaryX, bool isLeftBoundary)
    {
        if (isLeftBoundary)
        {
            boundaryClampLeftActive = true;
            boundaryClampMinX = boundaryX;
        }
        else
        {
            boundaryClampRightActive = true;
            boundaryClampMaxX = boundaryX;
        }
    }

    void UpdateMoveSpeed(float deltaTime)
    {
        if (moveSpeedIncreasePerSecond <= 0f)
        {
            currentMoveSpeed = Mathf.Max(0f, GetEffectiveBaseSpeed());
            return;
        }

        float targetMax = GetEffectiveMaxSpeed();
        float accel = moveSpeedIncreasePerSecond * Mathf.Max(0.01f, speedMultiplier);
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetMax, accel * deltaTime);
    }

    float GetDownhillSpeed()
    {
        float baseDownhill = -Mathf.Abs(descentSpeed) * Mathf.Max(0.01f, downhillSteepnessMultiplier);
        float speedT = GetSpeedNormalized();
        float speedMultiplier = Mathf.Lerp(1f, Mathf.Max(1f, maxDownhillSpeedMultiplier), speedT);
        return baseDownhill * speedMultiplier;
    }

    float GetSpeedNormalized()
    {
        float baseSpeed = GetEffectiveBaseSpeed();
        float maxSpeed = GetEffectiveMaxSpeed();
        if (maxSpeed <= 0f || Mathf.Approximately(maxSpeed, baseSpeed))
            return 0f;

        return Mathf.InverseLerp(baseSpeed, maxSpeed, currentMoveSpeed);
    }

    public void ApplySpeedMultiplier(float multiplier, float duration, bool snapToMax = false, bool snapToMin = false)
    {
        multiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
        if (speedEffectRoutine != null)
        {
            StopCoroutine(speedEffectRoutine);
            speedEffectRoutine = null;
        }

        speedBaselineMultiplier = speedMultiplier;
        speedBaselineMoveSpeed = currentMoveSpeed;
        hasSpeedEffectBaseline = true;

        SetSpeedMultiplier(multiplier);

        if (snapToMax && multiplier > 1f)
        {
            currentMoveSpeed = GetEffectiveMaxSpeed();
            currentVelocity = new Vector3(dirValue * currentMoveSpeed, GetDownhillSpeed(), 0f);
            if (rb != null)
                rb.linearVelocity = currentVelocity;
        }
        else if (snapToMin && multiplier < 1f)
        {
            currentMoveSpeed = GetEffectiveBaseSpeed();
            currentVelocity = new Vector3(dirValue * currentMoveSpeed, GetDownhillSpeed(), 0f);
            if (rb != null)
                rb.linearVelocity = currentVelocity;
        }

        if (duration > 0f)
            speedEffectRoutine = StartCoroutine(ResetSpeedAfter(duration));
    }

    float GetEffectiveBaseSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }

    float GetEffectiveMaxSpeed()
    {
        float baseSpeed = GetEffectiveBaseSpeed();
        return Mathf.Max(baseSpeed, maxMoveSpeed * speedMultiplier);
    }

    void SetSpeedMultiplier(float multiplier)
    {
        if (Mathf.Approximately(speedMultiplier, multiplier))
            return;

        float ratio = multiplier / Mathf.Max(0.01f, speedMultiplier);
        speedMultiplier = multiplier;
        currentMoveSpeed = Mathf.Max(0f, currentMoveSpeed * ratio);
        float maxSpeed = GetEffectiveMaxSpeed();
        if (currentMoveSpeed > maxSpeed)
            currentMoveSpeed = maxSpeed;
    }

    System.Collections.IEnumerator ResetSpeedAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        RestoreSpeedBaseline();
        speedEffectRoutine = null;
    }

    void RestoreSpeedBaseline()
    {
        if (!hasSpeedEffectBaseline)
        {
            SetSpeedMultiplier(1f);
            return;
        }

        SetSpeedMultiplier(speedBaselineMultiplier);
        currentMoveSpeed = Mathf.Clamp(speedBaselineMoveSpeed, 0f, GetEffectiveMaxSpeed());
        currentVelocity = new Vector3(dirValue * currentMoveSpeed, GetDownhillSpeed(), 0f);
        if (rb != null)
            rb.linearVelocity = currentVelocity;
        hasSpeedEffectBaseline = false;
    }

    bool IsGameplayActive()
    {
        if (Time.timeScale <= 0f)
            return false;

        if (GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.IsPlaying();
    }

    void EmitTurnBurst()
    {
        if (snowSprayController == null || turnBurstCount <= 0)
            return;

        snowSprayController.EmitManualBurst(turnBurstCount);
    }
}
