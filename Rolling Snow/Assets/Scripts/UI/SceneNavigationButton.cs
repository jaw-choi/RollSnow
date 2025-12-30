using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneNavigationButton : MonoBehaviour
{
    [System.Serializable]
    private class ButtonSceneBinding
    {
        public Button button;
        public string targetSceneName;
    }

    [SerializeField] private List<ButtonSceneBinding> navigationButtons = new List<ButtonSceneBinding>();
    private readonly Dictionary<Button, UnityAction> buttonListeners = new Dictionary<Button, UnityAction>();

    private void Awake()
    {
        RegisterButtons();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        var currentScene = SceneManager.GetActiveScene().name;
        RefreshAllButtons(currentScene);
    }

    private void OnDestroy()
    {
        UnregisterButtons();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void RegisterButtons()
    {
        UnregisterButtons();

        foreach (var binding in navigationButtons)
        {
            if (binding == null || binding.button == null)
            {
                Debug.LogWarning($"[SceneNavigationButton] Missing button reference on '{name}'.");
                continue;
            }

            if (string.IsNullOrEmpty(binding.targetSceneName))
            {
                Debug.LogWarning($"[SceneNavigationButton] Missing scene name for button '{binding.button.name}'.");
                continue;
            }

            var capturedSceneName = binding.targetSceneName;
            UnityAction listener = () => HandleButtonClick(capturedSceneName);
            binding.button.onClick.AddListener(listener);
            buttonListeners[binding.button] = listener;
        }
    }

    private void UnregisterButtons()
    {
        foreach (var pair in buttonListeners)
        {
            if (pair.Key != null)
            {
                pair.Key.onClick.RemoveListener(pair.Value);
            }
        }

        buttonListeners.Clear();
    }

    private void HandleButtonClick(string targetScene)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAllButtons(scene.name);
    }

    private void RefreshAllButtons(string activeScene)
    {
        foreach (var binding in navigationButtons)
        {
            if (binding == null || binding.button == null)
            {
                continue;
            }

            bool isSameScene = string.Equals(activeScene, binding.targetSceneName);
            binding.button.interactable = !isSameScene;
        }
    }
}
