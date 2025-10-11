using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Ref")]
    public GameObject pauseMenuUI;
    public GameObject settingsPanel;

    private bool isPaused = false;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            } 
            else 
            {
                Pause();
            }
        }
    }

    // resume game set speed to normal 
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    // pause game set speed to 0 (everything freezes)
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        settingsPanel.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(true);
    }
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
