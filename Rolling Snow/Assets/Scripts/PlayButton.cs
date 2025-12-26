using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "04_GameScene";

    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}