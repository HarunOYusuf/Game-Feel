using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseButton;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    private bool isPaused = false;

    private void Start()
    {
        ResumeGame();
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        pauseButton.SetActive(false);

        // Pauses the entire game world
        Time.timeScale = 0f;

        // Stops player input/movement script
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // Optional: pauses all game audio
        AudioListener.pause = true;

        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        pauseButton.SetActive(true);

        // Resumes the world
        Time.timeScale = 1f;

        // Re-enables player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        // Optional: resumes audio
        AudioListener.pause = false;

        isPaused = false;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Application.Quit();
        Debug.Log("Quit Game");
    }
}