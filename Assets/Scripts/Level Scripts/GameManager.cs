using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace UltimateController
{
    /// <summary>
    /// Manages level state, checkpoints, player spawning, ability unlocks, and the level timer.
    /// Place ONE in each level scene.
    /// 
    /// Setup:
    /// 1. Create empty GameObject named "GameManager"
    /// 2. Add this script
    /// 3. Assign the player and spawn point
    /// 4. Configure which abilities are available in this level
    /// 5. Optionally assign unlock trigger zones
    /// 6. Assign level end trigger for completion
    /// 7. Place a TimerStartTrigger at the end of the tutorial to start the timer
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player GameObject")]
        [SerializeField] private GameObject _player;
        
        [Tooltip("Where player spawns at level start")]
        [SerializeField] private Transform _spawnPoint;

        [Header("Level Settings")]
        [Tooltip("Name of this level (for UI/debug)")]
        [SerializeField] private string _levelName = "Level 1";
        
        [Tooltip("Scene to load when level is complete")]
        [SerializeField] private string _nextLevelScene;

        [Header("Initial Ability Settings")]
        [Tooltip("Can the player dash at level start?")]
        [SerializeField] private bool _dashEnabledAtStart = true;
        
        [Tooltip("Can the player use time clone at level start?")]
        [SerializeField] private bool _timeCloneEnabledAtStart = false;

        [Header("Ability Unlock Triggers (Optional)")]
        [Tooltip("When player enters this trigger, dash is unlocked")]
        [SerializeField] private Collider2D _dashUnlockTrigger;
        
        [Tooltip("When player enters this trigger, time clone is unlocked")]
        [SerializeField] private Collider2D _timeCloneUnlockTrigger;

        [Header("Level End")]
        [Tooltip("When player enters this trigger, level is complete")]
        [SerializeField] private Collider2D _levelEndTrigger;
        
        [Tooltip("UI Panel to show on completion (LevelEndUI script should be on this panel)")]
        [SerializeField] private GameObject _completionPanel;

        [Header("Timer Settings")]
        [Tooltip("Start the timer immediately at scene load (skip the trigger zone). Useful for testing.")]
        [SerializeField] private bool _startTimerAtSceneLoad = false;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        // Singleton
        public static GameManager Instance { get; private set; }

        // Current checkpoint
        private Vector2 _currentCheckpoint;
        private bool _hasCheckpoint;

        // Player components
        private UltimatePlayerController _playerController;
        private TimeCloneRecorder _cloneRecorder;
        private InputCloneRecorder _inputCloneRecorder;

        // Current ability states (can be unlocked during gameplay)
        private bool _dashEnabled;
        private bool _timeCloneEnabled;

        // Stats
        private int _deathCount;
        
        // Level complete state
        private bool _levelComplete;

        // Timer state
        // Uses Time.time accumulation, which is naturally pause-aware because
        // PauseMenu sets Time.timeScale = 0 (Time.time freezes during pause).
        private bool _timerRunning;
        private bool _timerEverStarted;
        private float _timerStartTime;
        private float _frozenTime; // The time captured when timer stops (so it doesn't tick after level end)

        // Public accessors
        public string LevelName => _levelName;
        public int DeathCount => _deathCount;
        public bool DashEnabled => _dashEnabled;
        public bool TimeCloneEnabled => _timeCloneEnabled;
        public bool LevelComplete => _levelComplete;

        /// <summary>
        /// Current level time in seconds. Pause-aware (frozen when Time.timeScale = 0).
        /// Returns 0 before the timer has started, and the frozen final time after level complete.
        /// </summary>
        public float LevelTime
        {
            get
            {
                if (!_timerEverStarted) return 0f;
                if (!_timerRunning) return _frozenTime;
                return Time.time - _timerStartTime;
            }
        }

        /// <summary>
        /// True if the timer is currently counting up.
        /// </summary>
        public bool IsTimerRunning => _timerRunning;

        /// <summary>
        /// True if the timer has been started at any point (used by UI to know whether to show).
        /// </summary>
        public bool HasTimerEverStarted => _timerEverStarted;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find player if not assigned
            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
            }

            if (_player != null)
            {
                _playerController = _player.GetComponent<UltimatePlayerController>();
                _cloneRecorder = _player.GetComponent<TimeCloneRecorder>();
                _inputCloneRecorder = _player.GetComponent<InputCloneRecorder>();
                
                // Ensure player has an inventory
                if (_player.GetComponent<PlayerInventory>() == null)
                {
                    _player.AddComponent<PlayerInventory>();
                }
            }

            // Set initial ability states
            _dashEnabled = _dashEnabledAtStart;
            _timeCloneEnabled = _timeCloneEnabledAtStart;
        }

        private void Start()
        {
            // Set initial checkpoint to spawn point
            if (_spawnPoint != null)
            {
                _currentCheckpoint = _spawnPoint.position;
                _hasCheckpoint = true;
            }
            else
            {
                if (_player != null)
                {
                    _currentCheckpoint = _player.transform.position;
                    _hasCheckpoint = true;
                }
                Debug.LogWarning("GameManager: No spawn point assigned! Using player's starting position.");
            }

            // Setup unlock triggers
            SetupUnlockTriggers();
            
            // Setup level end trigger
            SetupLevelEndTrigger();

            // Apply ability settings
            ApplyAbilitySettings();

            // Spawn player at start
            SpawnPlayer();
            
            // Hide completion panel at start
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(false);
            }

            // Optionally start the timer immediately (for testing or levels without a tutorial zone)
            if (_startTimerAtSceneLoad)
            {
                StartTimer();
            }

            if (_showDebugMessages)
                Debug.Log($"GameManager: {_levelName} started. Dash: {_dashEnabled}, Clone: {_timeCloneEnabled}");
        }

        private void SetupUnlockTriggers()
        {
            if (_dashUnlockTrigger != null)
            {
                _dashUnlockTrigger.isTrigger = true;
                var dashTrigger = _dashUnlockTrigger.gameObject.AddComponent<AbilityUnlockTrigger>();
                dashTrigger.Initialize(this, AbilityUnlockTrigger.AbilityType.Dash);
            }

            if (_timeCloneUnlockTrigger != null)
            {
                _timeCloneUnlockTrigger.isTrigger = true;
                var cloneTrigger = _timeCloneUnlockTrigger.gameObject.AddComponent<AbilityUnlockTrigger>();
                cloneTrigger.Initialize(this, AbilityUnlockTrigger.AbilityType.TimeClone);
            }
        }

        private void SetupLevelEndTrigger()
        {
            if (_levelEndTrigger != null)
            {
                _levelEndTrigger.isTrigger = true;
                var endTrigger = _levelEndTrigger.gameObject.AddComponent<LevelEndTrigger>();
                endTrigger.Initialize(this);
            }
        }

        #region Timer

        /// <summary>
        /// Start the level timer. Called by TimerStartTrigger when the player crosses
        /// the trigger zone at the end of the tutorial. Safe to call multiple times — only
        /// the first call has an effect.
        /// </summary>
        public void StartTimer()
        {
            if (_timerEverStarted) return;

            _timerRunning = true;
            _timerEverStarted = true;
            _timerStartTime = Time.time;
            _frozenTime = 0f;

            if (_showDebugMessages)
                Debug.Log("GameManager: Timer STARTED");
        }

        /// <summary>
        /// Stop the timer and freeze the final time. Called automatically when the level completes.
        /// </summary>
        public void StopTimer()
        {
            if (!_timerRunning) return;

            _frozenTime = Time.time - _timerStartTime;
            _timerRunning = false;

            if (_showDebugMessages)
                Debug.Log($"GameManager: Timer STOPPED at {LeaderboardEntry.FormatTime(_frozenTime)}");
        }

        #endregion

        public void UnlockDash()
        {
            if (_dashEnabled) return;
            _dashEnabled = true;
            ApplyAbilitySettings();
            if (_showDebugMessages) Debug.Log("GameManager: DASH UNLOCKED!");
        }

        public void UnlockTimeClone()
        {
            if (_timeCloneEnabled) return;
            _timeCloneEnabled = true;
            ApplyAbilitySettings();
            if (_showDebugMessages) Debug.Log("GameManager: TIME CLONE UNLOCKED!");
        }

        private void ApplyAbilitySettings()
        {
            if (_playerController != null)
            {
                _playerController.SetDashEnabled(_dashEnabled);
            }

            if (_cloneRecorder != null)
            {
                _cloneRecorder.enabled = _timeCloneEnabled;
                _cloneRecorder.SetRecordingEnabled(_timeCloneEnabled);
            }
            
            if (_inputCloneRecorder != null)
            {
                _inputCloneRecorder.enabled = _timeCloneEnabled;
                _inputCloneRecorder.SetRecordingEnabled(_timeCloneEnabled);
            }
        }

        public void SpawnPlayer()
        {
            if (_player == null) return;

            Vector2 spawnPos = _currentCheckpoint;

            if (_playerController != null)
            {
                _playerController.Teleport(spawnPos);
            }
            else
            {
                _player.transform.position = spawnPos;
            }

            _player.SetActive(true);

            ApplyAbilitySettings();

            if (_showDebugMessages)
                Debug.Log($"GameManager: Player spawned at {spawnPos}. Dash: {_dashEnabled}, Clone: {_timeCloneEnabled}");
        }

        public void OnPlayerDeath()
        {
            _deathCount++;

            if (_showDebugMessages)
                Debug.Log($"GameManager: Player died. Deaths: {_deathCount}");

            if (_cloneRecorder != null) _cloneRecorder.DestroyAllClones();
            if (_inputCloneRecorder != null) _inputCloneRecorder.DestroyAllClones();

            SpawnPlayer();
        }

        public void SetCheckpoint(Vector2 position)
        {
            _currentCheckpoint = position;
            _hasCheckpoint = true;
            if (_showDebugMessages) Debug.Log($"GameManager: Checkpoint set at {position}");
        }

        /// <summary>
        /// Called when player reaches the level end. Stops the timer and shows the completion panel.
        /// </summary>
        public void CompleteLevel()
        {
            if (_levelComplete) return;
            
            _levelComplete = true;

            // Stop the timer and capture the final time
            StopTimer();

            float completionTime = LevelTime;

            if (_showDebugMessages)
                Debug.Log($"GameManager: {_levelName} complete! Time: {LeaderboardEntry.FormatTime(completionTime)}, Deaths: {_deathCount}");

            if (_playerController != null)
            {
                _playerController.SetMovementEnabled(false);
            }

            ShowCompletionPanel();
        }

        private void ShowCompletionPanel()
        {
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                return;
            }

            CreateCompletionPanel();
        }

        private void CreateCompletionPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CompletionCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var panel = new GameObject("CompletionPanel");
            panel.transform.SetParent(canvas.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);

            var textObj = new GameObject("CompletionText");
            textObj.transform.SetParent(panel.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800, 200);
            textRect.anchoredPosition = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"Level Complete!\nTime: {LeaderboardEntry.FormatTime(LevelTime)}";
                tmp.fontSize = 48;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            _completionPanel = panel;
            
            if (_showDebugMessages)
                Debug.Log("GameManager: Created fallback completion panel (no LevelEndUI assigned)");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public Vector2 GetCheckpoint()
        {
            return _hasCheckpoint ? _currentCheckpoint : (Vector2)_spawnPoint.position;
        }

        private void OnDrawGizmos()
        {
            if (_spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_spawnPoint.position, 0.5f);
                Gizmos.DrawLine(_spawnPoint.position + Vector3.left * 0.3f, _spawnPoint.position + Vector3.right * 0.3f);
                Gizmos.DrawLine(_spawnPoint.position + Vector3.up * 0.3f, _spawnPoint.position + Vector3.down * 0.3f);
            }

            if (Application.isPlaying && _hasCheckpoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentCheckpoint, 0.4f);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            if (_dashUnlockTrigger != null)
            {
                Gizmos.DrawWireCube(_dashUnlockTrigger.bounds.center, _dashUnlockTrigger.bounds.size);
            }
            
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f);
            if (_timeCloneUnlockTrigger != null)
            {
                Gizmos.DrawWireCube(_timeCloneUnlockTrigger.bounds.center, _timeCloneUnlockTrigger.bounds.size);
            }
            
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            if (_levelEndTrigger != null)
            {
                Gizmos.DrawWireCube(_levelEndTrigger.bounds.center, _levelEndTrigger.bounds.size);
            }
        }
    }

    public class AbilityUnlockTrigger : MonoBehaviour
    {
        public enum AbilityType { Dash, TimeClone }
        
        private GameManager _gameManager;
        private AbilityType _abilityType;
        private bool _hasTriggered;

        public void Initialize(GameManager manager, AbilityType type)
        {
            _gameManager = manager;
            _abilityType = type;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;
            if (!other.TryGetComponent<UltimatePlayerController>(out _)) return;
            if (other.GetComponent<TimeClone>() != null) return;
            if (other.GetComponent<CloneMovement>() != null) return;

            _hasTriggered = true;

            switch (_abilityType)
            {
                case AbilityType.Dash: _gameManager.UnlockDash(); break;
                case AbilityType.TimeClone: _gameManager.UnlockTimeClone(); break;
            }
        }
    }

    public class LevelEndTrigger : MonoBehaviour
    {
        private GameManager _gameManager;
        private bool _hasTriggered;

        public void Initialize(GameManager manager) { _gameManager = manager; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;
            if (!other.TryGetComponent<UltimatePlayerController>(out _)) return;
            if (other.GetComponent<TimeClone>() != null) return;
            if (other.GetComponent<CloneMovement>() != null) return;

            _hasTriggered = true;
            _gameManager.CompleteLevel();
        }
    }
}