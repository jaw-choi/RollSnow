using UnityEngine;

public class Player : MonoBehaviour
{
    [Tooltip("Growth rate in scale units per second")]
    public float growthRate = 0.02f;
    [Tooltip("Maximum uniform scale")]
    public float maxScale = 1f;

    void Update()
    {
        // Increase player scale over time (uniformly)
        if (transform.localScale.x < maxScale)
        {
            var delta = Vector3.one * growthRate * Time.deltaTime;
            transform.localScale += delta;
            if (transform.localScale.x > maxScale)
                transform.localScale = Vector3.one * maxScale;
        }

        // Restart moved to GameManager

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
                Time.timeScale = 0f; // fallback
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

        Debug.Log("GAME OVER: hit obstacle");
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else
            Time.timeScale = 0f; // fallback
    }
}
