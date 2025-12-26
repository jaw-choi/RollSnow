using UnityEngine;

public class Player : MonoBehaviour
{
    [Tooltip("Growth rate in scale units per second")]
    public float growthRate = 0.02f;
    [Tooltip("Maximum uniform scale")]
    public float maxScale = 1f;

    Vector3 baseScale;
    Vector3 initialBaseScale;
    float externalScaleMultiplier = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.x > maxScale)
            baseScale = Vector3.one * maxScale;

        initialBaseScale = baseScale;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    void Update()
    {
        // Increase player scale over time (uniformly)
        if (baseScale.x < maxScale)
        {
            var delta = Vector3.one * growthRate * Time.deltaTime;
            baseScale += delta;
            if (baseScale.x > maxScale)
                baseScale = Vector3.one * maxScale;
        }

        ApplyScale();

        // Check if player left the camera viewport -> game over
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                {
                    Vector3 camPos = cam.transform.position;
                    Vector2 boundaryMin = Vector2.zero;
                    Vector2 boundaryMax = Vector2.one;
                    Debug.Log($"GAME OVER: left screen (CamPos={camPos}, PlayerPos={transform.position}, Viewport={vp}, BoundsMin={boundaryMin}, BoundsMax={boundaryMax})");
                    if (GameManager.Instance != null)
                        GameManager.Instance.GameOver();
                    else
                        Time.timeScale = 0f;
                }
            }
        }
    }

    public void ResetPlayerData()
    {
        baseScale = initialBaseScale;
        externalScaleMultiplier = 1f;
        ApplyScale();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleCollision(other);
    }

    void OnTriggerEnter(Collider other)
    {
        TryHandleCollision(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
            TryHandleCollision(collision.collider);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
            TryHandleCollision(collision.collider);
    }

    void TryHandleCollision(Component other)
    {
        if (other == null) return;

        var obstacle = other.GetComponent<ObstacleBehavior>();
        if (obstacle != null)
        {
            if (!obstacle.IsLethal())
                return;
        }
        else if (!other.CompareTag("Obstacle"))
        {
            return;
        }

        TriggerGameOver();
    }

    void TriggerGameOver()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else
            Time.timeScale = 0f; // fallback
    }

    void ApplyScale()
    {
        transform.localScale = baseScale * externalScaleMultiplier;
    }

    public void SetExternalScaleMultiplier(float multiplier)
    {
        externalScaleMultiplier = Mathf.Max(0.01f, multiplier);
        ApplyScale();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }
    }
}
