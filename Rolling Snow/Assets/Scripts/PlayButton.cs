using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "04_GameScene";
    [SerializeField] private Button playButton;

    void Awake()
    {
        if (playButton == null)
            playButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        var system = HeartSystem.GetOrCreate();
        if (system != null)
            system.HeartsChanged += HandleHeartsChanged;
        RefreshInteractable();
    }

    void OnDisable()
    {
        if (HeartSystem.Instance != null)
            HeartSystem.Instance.HeartsChanged -= HandleHeartsChanged;
    }

    void HandleHeartsChanged(HeartSystem.HeartStatus status)
    {
        RefreshInteractable();
    }

    void RefreshInteractable()
    {
        if (playButton == null)
            return;

        var system = HeartSystem.GetOrCreate();
        if (system == null)
        {
            playButton.interactable = true;
            return;
        }

        playButton.interactable = system.GetStatus().Current > 0;
    }

    public void OnPlayButtonClicked()
    {
        var system = HeartSystem.GetOrCreate();
        if (system != null && !system.TryConsumeHeart())
        {
            AudioManager.instance?.PlaySfx(AudioManager.Sfx.No);
            return;
        }

        AudioManager.instance?.PlaySfx(AudioManager.Sfx.Select);

        SceneManager.LoadScene(nextSceneName);
    }
}
