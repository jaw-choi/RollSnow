using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private Player player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ScoreBasedCameraFollow cameraFollow;

    public WorldScroller WorldScroller => worldScroller;
    public Player Player => player;
    public PlayerController PlayerController => playerController;
    public ScoreBasedCameraFollow CameraFollow => cameraFollow;
}
