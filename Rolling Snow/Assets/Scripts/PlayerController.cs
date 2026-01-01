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
    private int moveDir = 1; // start heading right; -1 = left, +1 = right
    int startingMoveDir = 1;

    [Header("Turn Settings")]
    public float tapThreshold = 0.18f; // seconds to consider a tap (quick flip)
    public float slowFlipDuration = 0.3f; // duration of slow flip when long-press
    // quickFlipDuration removed — short taps now use slowFlipDuration (curved behavior)

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
    Vector3 currentVelocity;

    Rigidbody rb;
    bool inputModeLogged;
    bool mouseUnavailableLogged;
    bool touchUnavailableLogged;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // begin with a gentle movement toward bottom-right
        startingMoveDir = moveDir;
        moveDir = startingMoveDir;
        dirValue = moveDir;
        baseMoveSpeed = Mathf.Max(0f, moveSpeed);
        currentMoveSpeed = baseMoveSpeed;
        currentVelocity = new Vector3(dirValue * currentMoveSpeed, -Mathf.Abs(descentSpeed), 0f);
    }

    void Update()
    {
        UpdateMoveSpeed(Time.deltaTime);

        // 1) input: mouse click or touch
        bool pressedDown = false;
        bool released = false;

        if (!inputModeLogged)
        {
            Debug.Log("PlayerController 입력 모드 초기화: 사용 가능한 Mouse/Touch 모두 감지");
            inputModeLogged = true;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                pressedDown = true;
                Debug.Log($"입력 감지: Mouse Down @ {Time.time:F2}");
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                released = true;
                Debug.Log($"입력 감지: Mouse Up @ {Time.time:F2}");
            }
        }
        else if (!mouseUnavailableLogged)
        {
            Debug.LogWarning("Mouse 장치를 찾을 수 없습니다.");
            mouseUnavailableLogged = true;
        }

        var touch = Touchscreen.current;
        if (touch != null)
        {
            var primary = touch.primaryTouch;
            if (primary.press.wasPressedThisFrame)
            {
                pressedDown = true;
                Debug.Log($"입력 감지: Touch Down @ {Time.time:F2}");
            }
            if (primary.press.wasReleasedThisFrame)
            {
                released = true;
                Debug.Log($"입력 감지: Touch Up @ {Time.time:F2}");
            }
        }
        else if (!touchUnavailableLogged)
        {
            Debug.LogWarning("Touchscreen 장치를 찾을 수 없습니다.");
            touchUnavailableLogged = true;
        }

        // 2) when pressed, start tracking to differentiate tap vs hold.
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
        currentFlipDuration = duration;
        flipInProgress = true;
        flipTriggeredThisPress = true;
        EmitTurnBurst();
        AudioManager.instance.BoostMusic(0.4f, 0.2f);
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

        if (minX < maxX)
        {
            float clampedX = Mathf.Clamp(newPos.x, minX, maxX);
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
        float targetDownhill = -Mathf.Abs(descentSpeed);
        currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, targetDownhill, downhillAcceleration * deltaTime);

        float targetLateral = dirValue * currentMoveSpeed;
        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetLateral, horizontalAcceleration * deltaTime);
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
        currentMoveSpeed = baseMoveSpeed;
        currentVelocity = new Vector3(dirValue * currentMoveSpeed, -Mathf.Abs(descentSpeed), 0f);

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

    void UpdateMoveSpeed(float deltaTime)
    {
        if (moveSpeedIncreasePerSecond <= 0f)
        {
            currentMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
            return;
        }

        float targetMax = Mathf.Max(baseMoveSpeed, maxMoveSpeed);
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetMax, moveSpeedIncreasePerSecond * deltaTime);
    }

    bool IsGameplayActive()
    {
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
