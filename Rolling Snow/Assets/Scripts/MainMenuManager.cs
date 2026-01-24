using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private static MainMenuManager instance;

    [Header("Scene Prefabs")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private ScenePrefabMapping[] scenePrefabs;

    [System.Serializable]
    private struct ScenePrefabMapping
    {
        public string sceneName;
        public GameObject prefab;
    }

    private readonly Dictionary<string, GameObject> scenePrefabLookup = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> scenePrefabInstances = new Dictionary<string, GameObject>();
    private GameObject activeScenePrefab;
    private Transform currentSceneCanvasParent;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[MainMenuManager] Duplicate instance detected in scene '{gameObject.scene.name}', destroying this one.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        //Debug.Log($"[MainMenuManager] Initializing in scene '{gameObject.scene.name}'.");
        DontDestroyOnLoad(gameObject);

        BuildScenePrefabLookup();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        var currentScene = SceneManager.GetActiveScene();
        //Debug.Log($"[MainMenuManager] Awake complete. Forcing initial load for scene '{currentScene.name}'.");
        HandleSceneLoaded(currentScene, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            //Debug.Log("[MainMenuManager] Instance destroyed. Unsubscribing from sceneLoaded.");
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneCanvasParent = ResolveCanvasForScene(scene);
        //Debug.Log($"[MainMenuManager] Scene loaded: '{scene.name}' (mode: {mode}). Swapping prefab.");
        SwapScenePrefab(scene.name);
    }

    private GameObject ResolvePersistentRoot()
    {
        // Always use the actual hierarchy root because DontDestroyOnLoad only works on root objects.
        var rootTransform = transform.root != null ? transform.root : transform;
        //Debug.Log($"[MainMenuManager] Resolving persistent root. Result: '{rootTransform.name}'.");
        return rootTransform.gameObject;
    }

    private void BuildScenePrefabLookup()
    {
        scenePrefabLookup.Clear();

        if (scenePrefabs == null)
        {
            return;
        }

        foreach (var entry in scenePrefabs)
        {
            if (string.IsNullOrEmpty(entry.sceneName) || entry.prefab == null)
            {
                //Debug.LogWarning("[MainMenuManager] Skipping empty scene prefab mapping (missing name or prefab).");
                continue;
            }

            scenePrefabLookup[entry.sceneName] = entry.prefab;
            //Debug.Log($"[MainMenuManager] Registered prefab '{entry.prefab.name}' for scene '{entry.sceneName}'.");
        }
    }

    private void SwapScenePrefab(string sceneName)
    {
        //Debug.Log($"[MainMenuManager] Attempting to activate prefab for scene '{sceneName}'.");
        var nextPrefab = GetOrCreatePrefabInstance(sceneName);

        if (activeScenePrefab == nextPrefab)
        {
            //Debug.Log($"[MainMenuManager] Prefab for scene '{sceneName}' is already active.");
            return;
        }

        if (activeScenePrefab != null)
        {
            activeScenePrefab.SetActive(false);
            //Debug.Log($"[MainMenuManager] Deactivated previous prefab '{activeScenePrefab.name}'.");
        }

        activeScenePrefab = nextPrefab;

        if (activeScenePrefab != null)
        {
            activeScenePrefab.SetActive(true);
            //Debug.Log($"[MainMenuManager] Activated prefab '{activeScenePrefab.name}' for scene '{sceneName}'.");
        }
        else
        {
            //Debug.LogWarning($"[MainMenuManager] No prefab configured for scene '{sceneName}'.");
        }
    }

    private GameObject GetOrCreatePrefabInstance(string sceneName)
    {
        if (!scenePrefabLookup.TryGetValue(sceneName, out var prefab) || prefab == null)
        {
            //Debug.LogWarning($"[MainMenuManager] Prefab lookup failed for scene '{sceneName}'.");
            return null;
        }

        if (scenePrefabInstances.TryGetValue(sceneName, out var existingInstance) && existingInstance != null)
        {
            //Debug.Log($"[MainMenuManager] Reusing existing prefab instance '{existingInstance.name}' for scene '{sceneName}'.");
            return existingInstance;
        }

        var parent = DeterminePrefabParent();
        var instance = Instantiate(prefab, parent, false);
        instance.SetActive(false);
        scenePrefabInstances[sceneName] = instance;
        //Debug.Log($"[MainMenuManager] Instantiated new prefab '{instance.name}' for scene '{sceneName}' under parent '{parent.name}'.");
        return instance;
    }

    private Transform DeterminePrefabParent()
    {
        if (targetCanvas != null)
        {
            return targetCanvas.transform;
        }

        if (currentSceneCanvasParent != null)
        {
            //Debug.Log($"[MainMenuManager] Using scene canvas '{currentSceneCanvasParent.name}' as parent.");
            return currentSceneCanvasParent;
        }

        var fallback = ResolvePersistentRoot().transform;
        //Debug.LogWarning($"[MainMenuManager] No canvas found in current scene. Falling back to '{fallback.name}'.");
        return fallback;
    }

    private Transform ResolveCanvasForScene(Scene scene)
    {
        Transform fallback = null;
        foreach (var rootObject in scene.GetRootGameObjects())
        {
            var canvas = rootObject.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                continue;
            }

            if (canvas.enabled)
            {
                //Debug.Log($"[MainMenuManager] Found active canvas '{canvas.name}' in scene '{scene.name}'.");
                return canvas.transform;
            }

            if (fallback == null)
            {
                fallback = canvas.transform;
            }
        }

        if (fallback != null)
        {
            //Debug.LogWarning($"[MainMenuManager] Only inactive canvas '{fallback.name}' found in scene '{scene.name}'. Using it as parent.");
            return fallback;
        }

        //Debug.LogWarning($"[MainMenuManager] No canvas found in scene '{scene.name}'.");
        return null;
    }
}
