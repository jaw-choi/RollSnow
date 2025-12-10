using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float sideSpeed = 8f;
    [SerializeField] private float maxSideVelocity = 6f;
    
    private Rigidbody2D rb;
    private bool isAlive = true;

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