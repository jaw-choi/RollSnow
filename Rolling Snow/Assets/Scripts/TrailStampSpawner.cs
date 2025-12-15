using UnityEngine;

public class TrailStampSpawner : MonoBehaviour
{
    public Transform trailRoot;          // assign WorldRoot/TrailRoot
    public GameObject stampPrefab;       // assign TrailStamp prefab

    public float interval = 0.06f;       // stamp frequency
    public float lifeTime = 6f;          // auto destroy after seconds
    public Vector3 offset = new Vector3(0f, 0.15f, 0f); // slightly behind (up)

    float timer;

  void FixedUpdate()
  {
            if (trailRoot == null || stampPrefab == null) return;

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;

        // create a stamp at ball position, parented under the moving world
        Vector3 pos = transform.position + offset;
        GameObject stamp = Instantiate(stampPrefab, pos, Quaternion.identity, trailRoot);

        Destroy(stamp, lifeTime);
  }
//   void Update()
//     {
//         if (trailRoot == null || stampPrefab == null) return;

//         timer += Time.deltaTime;
//         if (timer < interval) return;
//         timer = 0f;

//         // create a stamp at ball position, parented under the moving world
//         Vector3 pos = transform.position + offset;
//         GameObject stamp = Instantiate(stampPrefab, pos, Quaternion.identity, trailRoot);

//         Destroy(stamp, lifeTime);
//     }
}
