using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject victoryUI;
    public MusicManager musicManager; 

    private bool hasWon = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasWon) return; // prevent repeating
        if (other.CompareTag("Player"))
        {
            hasWon = true;
            ShowVictory();
        }
    }

    private void ShowVictory()
    {
        if (musicManager != null)
            musicManager.OnVictory(); 

        // pause the game
        Time.timeScale = 0f;

        // show the UI
        if (victoryUI) victoryUI.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        // resume normal time before switching scenes
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}