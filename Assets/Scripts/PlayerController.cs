using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float sideSpeed = 8f;
    [SerializeField] private float maxSideVelocity = 6f;
    [SerializeField] private float screenPadding = 0.5f; // 화면 가장자리 여유

    private Rigidbody2D rb;
    private bool isAlive = true;
    private Camera mainCam;
    private float camDistance;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
        
        gameObject.tag = "Player";

        mainCam = Camera.main;
        camDistance = (mainCam != null) ? Mathf.Abs(mainCam.transform.position.z - transform.position.z) : 0f;
    }

    void Update()
    {
        if (!isAlive) return;
        
        // 좌우 입력만
        float horizontalInput = Input.GetAxis("Horizontal");
        float sideVel = rb.linearVelocity.x + (horizontalInput * sideSpeed * Time.deltaTime);
        sideVel = Mathf.Clamp(sideVel, -maxSideVelocity, maxSideVelocity);
        
        // 속도 적용 (X축만 변함)
        rb.linearVelocity = new Vector2(sideVel, 0);

        // 화면 경계 계산 및 위치 제한 (Camera 기준)
        if (mainCam != null)
        {
            Vector3 leftWorld = mainCam.ViewportToWorldPoint(new Vector3(0f, 0.5f, camDistance));
            Vector3 rightWorld = mainCam.ViewportToWorldPoint(new Vector3(1f, 0.5f, camDistance));
            float clampedX = Mathf.Clamp(rb.position.x, leftWorld.x + screenPadding, rightWorld.x - screenPadding);
            if (!Mathf.Approximately(rb.position.x, clampedX))
            {
                rb.position = new Vector2(clampedX, rb.position.y);
                rb.linearVelocity = new Vector2(0f, 0f); // 경계에서 관성 제거
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            isAlive = false;
            GameManager.Instance.GameOver();
        }
    }
}