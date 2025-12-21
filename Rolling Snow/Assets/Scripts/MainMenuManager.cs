using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button startButton;
    //[SerializeField] private Button upgradeButton;
    [SerializeField] private Button settingsButton;
    //[SerializeField] private Button quitButton;
    //[SerializeField] private Button loginButton;

    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private Transform uiRoot; // Canvas �Ʒ� �� ������Ʈ
    //[SerializeField] private GameObject characterSelectPanel;
    private GameObject settingsInstance;

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(OnClickStart);
        //if (upgradeButton) upgradeButton.onClick.AddListener(OnClickUpGrade);
        if (settingsButton) settingsButton.onClick.AddListener(OnClickSettings);
        //if (quitButton) quitButton.onClick.AddListener(OnClickQuit);
        //if (loginButton) loginButton.onClick.AddListener(OnClickLogin);
    }



    public void OnClickStart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        //characterSelectPanel.SetActive(true);
        SceneManager.LoadScene("GameScene");
    }
    public void OnClickCharacter(int Id)
    {
       //PlayerStats.Instance.playerID = Id;
        // ���� ����
        SceneManager.LoadScene("GameScene");
    }
    public void OnClickUpGrade()
    {
        //SceneManager.LoadScene("UpgradeScene");
    }

    public void OnClickSettings()
    {
        if (uiRoot == null) uiRoot = FindFirstObjectByType<Canvas>()?.transform; // ������ġ
        if (settingsInstance == null)
            settingsInstance = Instantiate(settingsPanelPrefab, uiRoot);
        settingsInstance.SetActive(true);
        var panel = settingsInstance.GetComponent<SettingsUI>();
        panel.SetMode(SettingsUI.SettingsMode.MainMenu);

    }

    public void OnClickBack()
    {
        if (settingsInstance) settingsInstance.SetActive(false);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnClickLogin()
    {
        ///SceneManager.LoadScene("Login");
    }

}
