using System.Collections.Generic;
using UnityEngine;

public class ObstaclePool : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public List<GameObject> obstaclePrefabs = new List<GameObject>();
    public bool randomizePrefabs = true;

    [Header("Pooling")]
    public int prewarmCount = 20;

    [Header("Visual Variation")]
    [Tooltip("Uniform obstacle scale")]
    public float obstacleScale = 1f;
    public bool randomizeRotation = true;
    public Vector2 rotationRange = new Vector2(-10f, 10f);

    readonly Dictionary<GameObject, Queue<GameObject>> poolByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
    readonly Dictionary<int, GameObject> prefabByInstanceId = new Dictionary<int, GameObject>();
    readonly List<GameObject> activePrefabs = new List<GameObject>();

    void Awake()
    {
        BuildPrefabList();
        PrewarmPools();
    }

    void BuildPrefabList()
    {
        activePrefabs.Clear();

        if (obstaclePrefabs != null && obstaclePrefabs.Count > 0)
        {
            foreach (var prefab in obstaclePrefabs)
            {
                if (prefab != null && !activePrefabs.Contains(prefab))
                {
                    activePrefabs.Add(prefab);
                }
            }
        }
    }

    void PrewarmPools()
    {
        if (activePrefabs.Count == 0 || prewarmCount <= 0) return;

        foreach (var prefab in activePrefabs)
        {
            EnsureQueue(prefab);
            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                poolByPrefab[prefab].Enqueue(obj);
                prefabByInstanceId[obj.GetInstanceID()] = prefab;
            }
        }
    }

    void EnsureQueue(GameObject prefab)
    {
        if (prefab == null) return;
        if (!poolByPrefab.ContainsKey(prefab))
            poolByPrefab[prefab] = new Queue<GameObject>();
    }

    public GameObject Get()
    {
        var prefab = PickPrefab();
        if (prefab == null) return null;
        return GetFromPrefab(prefab);
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);

        var id = obj.GetInstanceID();
        if (prefabByInstanceId.TryGetValue(id, out var prefab) && prefab != null)
        {
            EnsureQueue(prefab);
            poolByPrefab[prefab].Enqueue(obj);
        }
    }

    public void ApplyVariation(Transform instance)
    {
        if (instance == null) return;

        if (randomizeRotation)
        {
            float minRot = Mathf.Min(rotationRange.x, rotationRange.y);
            float maxRot = Mathf.Max(rotationRange.x, rotationRange.y);
            instance.rotation = Quaternion.Euler(0f, 0f, Random.Range(minRot, maxRot));
        }

        instance.localScale = Vector3.one * Mathf.Max(0.01f, obstacleScale);
    }

    GameObject PickPrefab()
    {
        if (!randomizePrefabs || activePrefabs.Count == 1)
            return activePrefabs[0];

        return activePrefabs[Random.Range(0, activePrefabs.Count)];
    }

    GameObject GetFromPrefab(GameObject prefab)
    {
        EnsureQueue(prefab);

        GameObject obj = (poolByPrefab[prefab].Count > 0)
            ? poolByPrefab[prefab].Dequeue()
            : Instantiate(prefab, transform);

        obj.SetActive(true);
        prefabByInstanceId[obj.GetInstanceID()] = prefab;
        return obj;
    }
}
