using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject settingsPanel;

    public void PlayGame()
    {
        Time.timeScale = 1f;
        
        SceneManager.LoadScene("Scene test", LoadSceneMode.Single);
    }

    public void OpenSettings()
    {
        // show panel
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // hide panel
        settingsPanel.SetActive(false);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }


    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
