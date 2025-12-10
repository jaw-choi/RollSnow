using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float playerSpawnYPosition = -4f;
    
    private bool isGameOver = false;
    private float gameTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (playerPrefab != null)
            Instantiate(playerPrefab, new Vector2(0, playerSpawnYPosition), Quaternion.identity);
        
        if (enemySpawner == null)
            enemySpawner = GetComponent<EnemySpawner>();
    }

    void Update()
    {
        if (!isGameOver)
        {
            gameTime += Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        Debug.Log($"Game Over! Score: {gameTime:F1}");
    }

    public float GetGameTime()
    {
        return gameTime;
    }
}