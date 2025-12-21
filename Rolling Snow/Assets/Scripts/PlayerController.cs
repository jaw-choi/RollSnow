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
    private int moveDir = 0; // -1 = left, +1 = right, 0 = waiting

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
    }

    void Update()
    {
        // 1) input: mouse click or touch
        bool pressedDown = false;
        bool released = false;
        Vector2 pressPos = Vector2.zero;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressedDown = true;
                pressPos = Mouse.current.position.ReadValue();
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame) released = true;
        }
        if (!pressedDown && Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pressedDown = true;
                pressPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) released = true;
        }

        // 2) when pressed: if currently stopped, decide left/right using screen position;
        //    otherwise (already moving) flip the current direction.
        // Pressed down handling
        if (pressedDown)
        {
            // new press: allow flip for this press
            flipTriggeredThisPress = false;

            // If currently stopped, decide initial direction using screen X
            if (moveDir == 0)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    Vector3 myScreenPos = mainCam.WorldToScreenPoint(transform.position);
                    moveDir = (pressPos.x < myScreenPos.x) ? -1 : 1;
                    dirValue = moveDir;
                }
            }
            else
            {
                // already moving: start press timing for tap vs long-press
                isPressing = true;
                pressStartTime = Time.time;
            }
        }

        // Released handling (determine tap vs long press)
        if (released && isPressing)
        {
            float hold = Time.time - pressStartTime;
            // Start flip on release if not already triggered for this press
            if (!flipTriggeredThisPress)
            {
                if (!flipInProgress)
                {
                    StartFlip(slowFlipDuration);
                }
                // otherwise let existing flipInProgress continue
            }
            isPressing = false;
        }

        // If holding and reached tapThreshold, begin gradual flip while still holding
        if (isPressing && !flipInProgress && !flipTriggeredThisPress)
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
}
