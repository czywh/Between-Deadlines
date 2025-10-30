using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject settingsPanel;

    public void PlayGame()
    {
        // REMEMBER CHANGE BACK WHEN MERGING 
        //SceneManager.LoadScene("Scene test");

        SceneManager.LoadScene("UI Scene");
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
