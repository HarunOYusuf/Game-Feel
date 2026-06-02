using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause menu with PS5 controller support.
/// Options button (JoystickButton9) to pause/unpause.
/// 
/// Timer pausing is automatic: PauseGame() sets Time.timeScale = 0,
/// which freezes Time.time, which is what GameManager uses for its timer.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject leaderboardPanel;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    private bool isPaused = false;
    private bool inControlsMenu = false;
    private bool inLeaderboardMenu = false;

    private void Start()
    {
        ResumeGame();
        
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    private void Update()
    {
        // Get pause/start button from ControllerDatabase, fallback to JoystickButton9 (Options on PS)
        KeyCode pauseButton = KeyCode.JoystickButton9;
        if (ControllerDatabase.Instance != null)
        {
            pauseButton = ControllerDatabase.Instance.GetStartButton();
        }

        bool pausePressed = Input.GetKeyDown(pauseButton) || Input.GetKeyDown(KeyCode.Escape);
        
        if (pausePressed)
        {
            if (inControlsMenu)
            {
                CloseControlsMenu();
            }
            else if (inLeaderboardMenu)
            {
                CloseLeaderboardMenu();
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
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        // Sets Time.timeScale = 0, which automatically pauses GameManager's timer
        // because the timer uses Time.time (which is timescale-dependent)
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
        inLeaderboardMenu = false;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (pauseButton != null)
            pauseButton.SetActive(true);
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

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

    /// <summary>
    /// Open the leaderboard panel from the pause menu.
    /// Wire this to your "Leaderboard" button's OnClick.
    /// </summary>
    public void OpenLeaderboardMenu()
    {
        inLeaderboardMenu = true;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);

            // Refresh the leaderboard contents
            var leaderboardUI = leaderboardPanel.GetComponentInChildren<UltimateController.LeaderboardUI>();
            if (leaderboardUI != null)
            {
                leaderboardUI.SetHighlightedPlacement(-1);
                leaderboardUI.Refresh();
            }
            
            var navigator = leaderboardPanel.GetComponent<UINavigator>();
            if (navigator != null)
            {
                navigator.RefreshSelectables();
            }
        }
    }

    /// <summary>
    /// Close the leaderboard panel and return to the pause menu.
    /// </summary>
    public void CloseLeaderboardMenu()
    {
        inLeaderboardMenu = false;
        
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
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