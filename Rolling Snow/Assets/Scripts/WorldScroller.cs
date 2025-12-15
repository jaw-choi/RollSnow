using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    public float worldSpeed = 4f; // how fast the map moves up

    void Update()
    {
        transform.position += Vector3.up * (worldSpeed * Time.deltaTime);
    }
}
