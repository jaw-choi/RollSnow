using System.Collections.Generic;
using UnityEngine;

public class ObstaclePool : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject obstaclePrefab; // 기존 단일 프리팹(하위 호환)
    public List<GameObject> obstaclePrefabs = new List<GameObject>(); // 여러 타입 장애물용 프리팹 목록
    public bool randomizePrefabs = true; // 활성화 시 매번 랜덤 프리팹 선택

    [Header("Pooling")]
    public int prewarmCount = 20; // 프리팹 1개당 미리 만들어 둘 개수

    readonly Dictionary<GameObject, Queue<GameObject>> poolByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
    readonly Dictionary<int, GameObject> prefabByInstanceId = new Dictionary<int, GameObject>();
    List<GameObject> activePrefabs = new List<GameObject>();

    void Awake()
    {
        // 사용할 프리팹 목록 확정(여러 개가 없으면 기존 단일 프리팹 사용)
        if (obstaclePrefabs != null && obstaclePrefabs.Count > 0)
        {
            activePrefabs.AddRange(obstaclePrefabs);
        }
        else if (obstaclePrefab != null)
        {
            activePrefabs.Add(obstaclePrefab);
        }

        // 각 프리팹별로 풀을 준비
        for (int p = 0; p < activePrefabs.Count; p++)
        {
            var prefab = activePrefabs[p];
            if (!poolByPrefab.ContainsKey(prefab))
                poolByPrefab[prefab] = new Queue<GameObject>();

            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                poolByPrefab[prefab].Enqueue(obj);
                prefabByInstanceId[obj.GetInstanceID()] = prefab;
            }
        }
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
            if (!poolByPrefab.ContainsKey(prefab))
                poolByPrefab[prefab] = new Queue<GameObject>();
            poolByPrefab[prefab].Enqueue(obj);
        }
    }

    GameObject PickPrefab()
    {
        if (activePrefabs == null || activePrefabs.Count == 0) return obstaclePrefab;
        if (!randomizePrefabs || activePrefabs.Count == 1) return activePrefabs[0];
        return activePrefabs[Random.Range(0, activePrefabs.Count)];
    }

    GameObject GetFromPrefab(GameObject prefab)
    {
        if (!poolByPrefab.ContainsKey(prefab))
            poolByPrefab[prefab] = new Queue<GameObject>();

        GameObject obj = (poolByPrefab[prefab].Count > 0)
            ? poolByPrefab[prefab].Dequeue()
            : Instantiate(prefab, transform);

        obj.SetActive(true);
        prefabByInstanceId[obj.GetInstanceID()] = prefab;
        return obj;
    }
}
