using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1f;
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

    Rigidbody rb;
    Camera mainCam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;

        // begin with a gentle movement toward bottom-right
        startingMoveDir = moveDir;
        moveDir = startingMoveDir;
        dirValue = moveDir;
    }

    void Update()
    {
        if (!IsGameplayActive())
        {
            Debug.Log("Gameplay not active, skipping PlayerController Update.");
            return;
        }

        // 1) input: mouse click or touch
        bool pressedDown = false;
        bool released = false;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressedDown = true;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame) released = true;
        }
        if (!pressedDown && Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pressedDown = true;
            }
            if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) released = true;
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
    }

    void ApplyTransformMovement(float deltaTime)
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, groundY, descentSpeed * deltaTime);

        if (Mathf.Abs(dirValue) > 0.001f)
        {
            pos += Vector3.right * (dirValue * moveSpeed * deltaTime);
        }

        transform.position = pos;
    }

    void ApplyRigidbodyMovement(float deltaTime)
    {
        Vector3 newPos = rb.position;
        newPos.y = Mathf.MoveTowards(newPos.y, groundY, descentSpeed * deltaTime);

        if (Mathf.Abs(dirValue) > 0.001f)
        {
            newPos += Vector3.right * (dirValue * moveSpeed * deltaTime);
        }

        rb.MovePosition(newPos);
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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    bool IsGameplayActive()
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.IsPlaying();
    }
}
