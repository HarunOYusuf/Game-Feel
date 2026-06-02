using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateController
{
    /// <summary>
    /// Manages the level-end completion screen.
    /// 
    /// Flow:
    /// 1. GameManager.CompleteLevel() activates this panel
    /// 2. Show final time + provisional placement ("You will place 8TH!")
    /// 3. Player enters their name in the input field
    /// 4. Player clicks Submit
    /// 5. Entry is added to the leaderboard, placement is finalised
    /// 6. Leaderboard is shown with their entry highlighted
    /// 7. Player can restart, go to main menu, or quit
    /// 
    /// Setup:
    /// 1. Build the completion panel UI in your scene
    /// 2. Add this script to the panel root
    /// 3. Wire up all the field references in the Inspector
    /// 4. Hook the Submit button's OnClick to OnSubmitName()
    /// 5. Hook Restart/Menu/Quit buttons to their methods
    /// 6. Assign this panel to GameManager._completionPanel
    /// </summary>
    public class LevelEndUI : MonoBehaviour
    {
        [Header("Time Display")]
        [Tooltip("Shows the player's completion time (MM:SS.mmm)")]
        [SerializeField] private TMP_Text _timeText;

        [Tooltip("Shows the player's provisional placement, e.g. 'You will place 8TH!'")]
        [SerializeField] private TMP_Text _placementText;

        [Header("Name Entry")]
        [Tooltip("Sub-panel shown before name has been submitted")]
        [SerializeField] private GameObject _nameEntryPanel;

        [Tooltip("Input field for the player's name")]
        [SerializeField] private TMP_InputField _nameInput;

        [Tooltip("Submit button (also wired manually via OnSubmitName)")]
        [SerializeField] private Button _submitButton;

        [Header("Result (after submit)")]
        [Tooltip("Sub-panel shown AFTER name has been submitted (e.g. 'You placed 8TH!')")]
        [SerializeField] private GameObject _resultPanel;

        [Tooltip("Final placement message, e.g. 'You placed 8TH!'")]
        [SerializeField] private TMP_Text _finalPlacementText;

        [Header("Leaderboard")]
        [Tooltip("The LeaderboardUI for the scrollable list of all entries")]
        [SerializeField] private LeaderboardUI _leaderboardUI;

        [Tooltip("The GameObject containing the leaderboard (panel root). Hidden until after submit + delay.")]
        [SerializeField] private GameObject _leaderboardContainer;

        [Tooltip("Seconds to wait after submit before revealing the leaderboard")]
        [SerializeField] private float _leaderboardRevealDelay = 5f;

        [Header("Configuration")]
        [Tooltip("Default placeholder text in the input field")]
        [SerializeField] private string _defaultPlaceholder = "Enter your name...";

        [Tooltip("Maximum name length")]
        [SerializeField] private int _maxNameLength = 16;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        private float _completionTime;
        private bool _hasSubmitted;
        private float _submitTime;
        private bool _leaderboardRevealed;

        private void OnEnable()
        {
            // When this panel becomes visible, populate it from GameManager
            var gm = GameManager.Instance;
            if (gm != null && gm.LevelComplete)
            {
                Show(gm.LevelTime);
            }
        }

        /// <summary>
        /// Display the completion screen for the given final time.
        /// </summary>
        public void Show(float completionTime)
        {
            _completionTime = completionTime;
            _hasSubmitted = false;
            _leaderboardRevealed = false;

            // Time display
            if (_timeText != null)
            {
                _timeText.text = LeaderboardEntry.FormatTime(_completionTime);
            }

            // Provisional placement
            int provisionalPlacement = LeaderboardManager.Instance.CalculatePlacement(_completionTime);
            if (_placementText != null)
            {
                _placementText.text = $"You will place {LeaderboardManager.FormatPlacement(provisionalPlacement)}!";
            }

            // Stage 1: show name entry, hide result panel AND leaderboard
            if (_nameEntryPanel != null) _nameEntryPanel.SetActive(true);
            if (_resultPanel != null) _resultPanel.SetActive(false);
            if (_leaderboardContainer != null) _leaderboardContainer.SetActive(false);

            // Pre-fill input with last used name
            if (_nameInput != null)
            {
                _nameInput.text = LeaderboardManager.Instance.LastPlayerName;
                _nameInput.characterLimit = _maxNameLength;
                
                if (_nameInput.placeholder is TMP_Text placeholder)
                {
                    placeholder.text = _defaultPlaceholder;
                }

                // Focus the input field so they can type immediately
                _nameInput.Select();
                _nameInput.ActivateInputField();
            }
        }

        /// <summary>
        /// Called by the Submit button. Adds the entry to the leaderboard,
        /// reveals the result panel, and schedules the leaderboard reveal after a delay.
        /// </summary>
        public void OnSubmitName()
        {
            if (_hasSubmitted) return;

            string name = _nameInput != null ? _nameInput.text : "Anonymous";

            int placement = LeaderboardManager.Instance.AddEntry(name, _completionTime);
            _hasSubmitted = true;
            _submitTime = Time.unscaledTime; // Use unscaled in case timescale is paused

            if (_showDebugMessages)
            {
                Debug.Log($"LevelEndUI: Submitted '{name}' → placed {LeaderboardManager.FormatPlacement(placement)}");
            }

            // Stage 2: hide name entry, show result. Leaderboard still hidden.
            if (_nameEntryPanel != null) _nameEntryPanel.SetActive(false);
            if (_resultPanel != null) _resultPanel.SetActive(true);
            if (_leaderboardContainer != null) _leaderboardContainer.SetActive(false);

            // Final placement message
            if (_finalPlacementText != null)
            {
                _finalPlacementText.text = $"You placed {LeaderboardManager.FormatPlacement(placement)}!";
            }

            // Prepare the leaderboard with the new entry highlighted, but don't show it yet
            if (_leaderboardUI != null)
            {
                _leaderboardUI.SetHighlightedPlacement(placement);
                // We'll refresh + reveal in Update() after the delay
            }
        }

        private void Update()
        {
            // Stage 3: reveal the leaderboard after the delay
            if (_hasSubmitted && !_leaderboardRevealed)
            {
                if (Time.unscaledTime - _submitTime >= _leaderboardRevealDelay)
                {
                    RevealLeaderboard();
                }
            }
        }

        private void RevealLeaderboard()
        {
            _leaderboardRevealed = true;

            if (_leaderboardContainer != null)
            {
                _leaderboardContainer.SetActive(true);
            }

            if (_leaderboardUI != null)
            {
                _leaderboardUI.Refresh();
            }

            if (_showDebugMessages)
            {
                Debug.Log("LevelEndUI: Leaderboard revealed");
            }
        }

        /// <summary>
        /// Restart the current level. Wire to a "Restart" button.
        /// </summary>
        public void OnRestart()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        /// <summary>
        /// Load the main menu scene. Wire to a "Main Menu" button.
        /// </summary>
        public void OnMainMenu(string mainMenuSceneName = "MainMenu")
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Quit the game. Wire to a "Quit" button.
        /// </summary>
        public void OnQuit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}