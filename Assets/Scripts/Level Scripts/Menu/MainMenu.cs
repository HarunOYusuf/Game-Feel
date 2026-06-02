using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateController
{
    /// <summary>
    /// Simple main menu controller.
    /// 
    /// Setup:
    /// 1. Create a new scene called "MainMenu"
    /// 2. Create a Canvas with buttons
    /// 3. Attach this script to an empty GameObject
    /// 4. Link buttons to Play(), Quit(), OpenLeaderboard(), CloseLeaderboard()
    /// 5. Assign the Main Menu Panel and Leaderboard Panel references
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Name of the first level scene to load")]
        [SerializeField] private string _firstLevelScene = "Level1";

        [Header("Panels")]
        [Tooltip("The main menu buttons panel (Play, Leaderboard, Quit)")]
        [SerializeField] private GameObject _mainMenuPanel;

        [Tooltip("The leaderboard panel (with a LeaderboardUI component on it or a child)")]
        [SerializeField] private GameObject _leaderboardPanel;

        private void Start()
        {
            // Ensure we start on the main menu, not the leaderboard
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(true);
            if (_leaderboardPanel != null)
                _leaderboardPanel.SetActive(false);
        }

        /// <summary>
        /// Called when Play button is pressed
        /// </summary>
        public void Play()
        {
            SceneManager.LoadScene(_firstLevelScene);
        }

        /// <summary>
        /// Called when Quit button is pressed
        /// </summary>
        public void Quit()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// Open the leaderboard panel. Wire this to a "Leaderboard" button.
        /// </summary>
        public void OpenLeaderboard()
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(false);

            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(true);

                // Refresh the leaderboard contents
                var leaderboardUI = _leaderboardPanel.GetComponentInChildren<LeaderboardUI>();
                if (leaderboardUI != null)
                {
                    leaderboardUI.SetHighlightedPlacement(-1);
                    leaderboardUI.Refresh();
                }

                // Refresh controller navigation if present
                var navigator = _leaderboardPanel.GetComponent<UINavigator>();
                if (navigator != null)
                {
                    navigator.RefreshSelectables();
                }
            }
        }

        /// <summary>
        /// Close the leaderboard panel and return to the main menu.
        /// Wire this to the leaderboard's "Back" button.
        /// </summary>
        public void CloseLeaderboard()
        {
            if (_leaderboardPanel != null)
                _leaderboardPanel.SetActive(false);

            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(true);

                var navigator = _mainMenuPanel.GetComponent<UINavigator>();
                if (navigator != null)
                {
                    navigator.RefreshSelectables();
                }
            }
        }
    }
}