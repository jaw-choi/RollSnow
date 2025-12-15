using UnityEngine;

public class Player : MonoBehaviour
{
void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Debug.Log("GAME OVER: hit obstacle");
            Time.timeScale = 0f; // 테스트용 정지
        }
    }
}
