using UnityEngine;

public class TrailStampSpawner : MonoBehaviour
{
    public Transform trailRoot;          // assign WorldRoot/TrailRoot
    public GameObject stampPrefab;       // assign TrailStamp prefab

    public float interval = 0.06f;       // stamp frequency
    public float lifeTime = 6f;          // auto destroy after seconds
    public Vector3 offset = new Vector3(0f, 0.15f, 0f); // slightly behind (up)

    [Tooltip("Particle prefab to spawn when player changes horizontal direction")]
    public GameObject turnParticlePrefab;
    [Tooltip("Seconds to keep spawned turn particle before destroy (0 = let particle handle)")]
    public float turnParticleLife = 2f;
    [Tooltip("Minimum horizontal movement to consider (to avoid noise)")]
    public float turnDetectThreshold = 0.01f;
    [Tooltip("Cooldown between turn particle spawns")]
    public float turnCooldown = 0.2f;

    float timer;

    float prevX;
    int prevSign = 0;
    float turnTimer;

    void FixedUpdate()
    {
      if (trailRoot == null || stampPrefab == null) return;

      // stamping
      timer += Time.deltaTime;
      if (timer >= interval)
      {
        timer = 0f;
        Vector3 pos = transform.position + offset;
        GameObject stamp = Instantiate(stampPrefab, pos, Quaternion.identity, trailRoot);
        Destroy(stamp, lifeTime);
      }

      // direction change detection (horizontal)
      float curX = transform.position.x;
      float dx = curX - prevX;
      int sign = 0;
      if (Mathf.Abs(dx) > turnDetectThreshold) sign = (dx > 0f) ? 1 : -1;

      turnTimer -= Time.deltaTime;
      if (prevSign != 0 && sign != 0 && sign != prevSign && turnTimer <= 0f)
      {
        // direction flipped -> spawn particle at trail position
        if (turnParticlePrefab != null)
        {
          Vector3 p = transform.position + offset;
          var go = Instantiate(turnParticlePrefab, p, Quaternion.identity, trailRoot);
          if (turnParticleLife > 0f) Destroy(go, turnParticleLife);
        }
        turnTimer = turnCooldown;
      }

      if (sign != 0) prevSign = sign;
      prevX = curX;
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
