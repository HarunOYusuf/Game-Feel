using TMPro;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Displays the current level time in MM:SS.mmm format.
    /// Reads from GameManager.LevelTime, which is naturally pause-aware
    /// because it uses Time.time (frozen when Time.timeScale = 0).
    /// 
    /// Setup:
    /// 1. Create a UI Text (TMP) in your Canvas, anchored bottom-left
    /// 2. Add this script to it (or its parent if you have a styled container)
    /// 3. Assign the TMP_Text field
    /// 4. Optionally tick "Hide Before Timer Starts" to keep it invisible
    ///    until the player crosses the start trigger
    /// </summary>
    public class TimerDisplay : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TextMeshPro component to update each frame")]
        [SerializeField] private TMP_Text _timerText;

        [Header("Behaviour")]
        [Tooltip("Hide the timer until the timer has actually started")]
        [SerializeField] private bool _hideBeforeTimerStarts = true;

        [Tooltip("Keep the timer visible after level completes (shows final time)")]
        [SerializeField] private bool _showAfterLevelComplete = true;

        [Header("Style (Optional)")]
        [Tooltip("Text colour when timer is running")]
        [SerializeField] private Color _runningColour = Color.white;

        [Tooltip("Text colour when timer is paused (level complete or pause menu)")]
        [SerializeField] private Color _pausedColour = new Color(1f, 0.85f, 0.3f);

        private void Awake()
        {
            // Try to auto-find the text component if not assigned
            if (_timerText == null)
            {
                _timerText = GetComponent<TMP_Text>();
            }
        }

        private void Update()
        {
            if (_timerText == null) return;

            var gm = GameManager.Instance;

            if (gm == null)
            {
                _timerText.enabled = false;
                return;
            }

            // Hide until the timer has been started by the trigger zone
            if (!gm.IsTimerRunning && !gm.HasTimerEverStarted)
            {
                if (_hideBeforeTimerStarts)
                {
                    _timerText.enabled = false;
                    return;
                }
            }

            // Hide after level complete if configured
            if (gm.LevelComplete && !_showAfterLevelComplete)
            {
                _timerText.enabled = false;
                return;
            }

            _timerText.enabled = true;
            _timerText.text = LeaderboardEntry.FormatTime(gm.LevelTime);
            _timerText.color = gm.IsTimerRunning ? _runningColour : _pausedColour;
        }
    }
}