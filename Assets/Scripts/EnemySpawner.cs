using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnHeight = 8f;
    [SerializeField] private float spawnRangeX = 3f;
    
    private float spawnRate = 2f;
    private float spawnTimer = 0f;
    private float gameTime = 0f;

    void Update()
    {
        gameTime += Time.deltaTime;
        
        // 난이도 증가: 시간에 따라 장애물 생성 빈도 증가
        spawnRate = Mathf.Max(2f - (gameTime * 0.05f), 0.5f);
        
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnRate)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            float randomX = Random.Range(-spawnRangeX, spawnRangeX);
            Instantiate(enemyPrefab, new Vector2(randomX, spawnHeight), Quaternion.identity);
        }
    }
}