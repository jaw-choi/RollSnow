using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private static MainMenuManager instance;

    [Header("Persistence")]
    [SerializeField] private GameObject persistentRoot;

    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button achievementButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button settingsSceneButton;
    [SerializeField] private Canvas menuCanvas;

    private void Awake()
    {
        var root = ResolvePersistentRoot();

        if (instance != null && instance != this)
        {
            Destroy(root);
            return;
        }

        instance = this;
        DontDestroyOnLoad(root);

        RegisterButtonListeners();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        UpdateVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void RegisterButtonListeners()
    {
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => LoadMenuScene("01_MainMenu"));
        if (achievementButton) achievementButton.onClick.AddListener(() => LoadMenuScene("02_Achivement"));
        if (shopButton) shopButton.onClick.AddListener(() => LoadMenuScene("03_Shop"));
        //if (playButton) playButton.onClick.AddListener(() => LoadMenuScene("04_GameScene"));
        if (inventoryButton) inventoryButton.onClick.AddListener(() => LoadMenuScene("05_Inventory"));
        if (settingsSceneButton) settingsSceneButton.onClick.AddListener(() => LoadMenuScene("06_Settings"));
    }

    private void LoadMenuScene(string sceneName)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVisibility(scene.name);
    }

    private void UpdateVisibility(string sceneName)
    {
        if (menuCanvas == null)
        {
            menuCanvas = GetComponentInChildren<Canvas>(true);
        }

        bool shouldHide = sceneName == "04_GameScene";
        if (menuCanvas != null)
        {
            menuCanvas.enabled = !shouldHide;
        }
    }

    private GameObject ResolvePersistentRoot()
    {
        if (persistentRoot != null)
        {
            return persistentRoot;
        }

        var rootTransform = transform.root != null ? transform.root : transform;
        return rootTransform.gameObject;
    }
}
