using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause menu with PS5 controller support.
/// Options button (JoystickButton9) to pause/unpause.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject controlsPanel;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    private bool isPaused = false;
    private bool inControlsMenu = false;

    private void Start()
    {
        ResumeGame();
        
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    private void Update()
    {
        // Options button (JoystickButton9) or Escape to toggle pause
        bool pausePressed = Input.GetKeyDown(KeyCode.JoystickButton9) || Input.GetKeyDown(KeyCode.Escape);
        
        if (pausePressed)
        {
            if (inControlsMenu)
            {
                CloseControlsMenu();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        if (pauseButton != null)
            pauseButton.SetActive(false);
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        Time.timeScale = 0f;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        AudioListener.pause = true;

        // Refresh navigator
        var navigator = pauseMenu?.GetComponent<UINavigator>();
        if (navigator != null)
        {
            navigator.RefreshSelectables();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        inControlsMenu = false;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (pauseButton != null)
            pauseButton.SetActive(true);
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        Time.timeScale = 1f;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        AudioListener.pause = false;
    }

    public void OpenControlsMenu()
    {
        inControlsMenu = true;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
            
            var navigator = controlsPanel.GetComponent<UINavigator>();
            if (navigator != null)
            {
                navigator.RefreshSelectables();
            }
        }
    }

    public void CloseControlsMenu()
    {
        inControlsMenu = false;
        
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
            
            var navigator = pauseMenu.GetComponent<UINavigator>();
            if (navigator != null)
            {
                navigator.RefreshSelectables();
            }
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}