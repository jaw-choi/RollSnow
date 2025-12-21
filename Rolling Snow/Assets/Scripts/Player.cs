using UnityEngine;

public class Player : MonoBehaviour
{
    [Tooltip("Growth rate in scale units per second")]
    public float growthRate = 0.02f;
    [Tooltip("Maximum uniform scale")]
    public float maxScale = 1f;

    Vector3 baseScale;
    float externalScaleMultiplier = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.x > maxScale)
            baseScale = Vector3.one * maxScale;
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
                    Debug.Log("GAME OVER: left screen");
                    if (GameManager.Instance != null)
                        GameManager.Instance.GameOver();
                    else
                        Time.timeScale = 0f;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 장애물 타입에 따라 치명 여부가 달라질 수 있으므로 컴포넌트를 우선 확인
        var obstacle = other.GetComponent<ObstacleBehavior>();
        if (obstacle != null)
        {
            if (!obstacle.IsLethal()) return;
        }
        else if (!other.CompareTag("Obstacle"))
        {
            return;
        }

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
}
