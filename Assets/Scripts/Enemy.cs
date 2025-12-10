using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(0, -fallSpeed);
    }

    void Update()
    {
        // 화면 밖으로 나가면 제거
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}