using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace UltimateController
{
    /// <summary>
    /// Manages level state, checkpoints, player spawning, and ability unlocks.
    /// Place ONE in each level scene.
    /// 
    /// Setup:
    /// 1. Create empty GameObject named "GameManager"
    /// 2. Add this script
    /// 3. Assign the player and spawn point
    /// 4. Configure which abilities are available in this level
    /// 5. Optionally assign unlock trigger zones
    /// 6. Assign level end trigger for completion
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
        
        [Tooltip("UI Panel to show on completion (optional - will create one if not assigned)")]
        [SerializeField] private GameObject _completionPanel;

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
        private float _levelStartTime;
        
        // Level complete state
        private bool _levelComplete;

        // Public accessors
        public string LevelName => _levelName;
        public int DeathCount => _deathCount;
        public float LevelTime => Time.time - _levelStartTime;
        public bool DashEnabled => _dashEnabled;
        public bool TimeCloneEnabled => _timeCloneEnabled;
        public bool LevelComplete => _levelComplete;

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
            _levelStartTime = Time.time;

            // Set initial checkpoint to spawn point (this is the default respawn before any checkpoint)
            if (_spawnPoint != null)
            {
                _currentCheckpoint = _spawnPoint.position;
                _hasCheckpoint = true;
            }
            else
            {
                // Fallback: use player's current position as spawn
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

            if (_showDebugMessages)
                Debug.Log($"GameManager: {_levelName} started. Dash: {_dashEnabled}, Clone: {_timeCloneEnabled}");
        }

        private void SetupUnlockTriggers()
        {
            // Setup dash unlock trigger
            if (_dashUnlockTrigger != null)
            {
                _dashUnlockTrigger.isTrigger = true;
                var dashTrigger = _dashUnlockTrigger.gameObject.AddComponent<AbilityUnlockTrigger>();
                dashTrigger.Initialize(this, AbilityUnlockTrigger.AbilityType.Dash);
            }

            // Setup time clone unlock trigger
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

        /// <summary>
        /// Unlock dash ability (persists through death)
        /// </summary>
        public void UnlockDash()
        {
            if (_dashEnabled) return;
            
            _dashEnabled = true;
            ApplyAbilitySettings();

            if (_showDebugMessages)
                Debug.Log("GameManager: DASH UNLOCKED!");
        }

        /// <summary>
        /// Unlock time clone ability (persists through death)
        /// </summary>
        public void UnlockTimeClone()
        {
            if (_timeCloneEnabled) return;
            
            _timeCloneEnabled = true;
            ApplyAbilitySettings();

            if (_showDebugMessages)
                Debug.Log("GameManager: TIME CLONE UNLOCKED!");
        }

        /// <summary>
        /// Apply current ability settings to player
        /// </summary>
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

        /// <summary>
        /// Spawn or respawn player at current checkpoint
        /// </summary>
        public void SpawnPlayer()
        {
            if (_player == null) return;

            // Always use _currentCheckpoint - it's set to spawn point at Start
            Vector2 spawnPos = _currentCheckpoint;

            // Teleport player
            if (_playerController != null)
            {
                _playerController.Teleport(spawnPos);
            }
            else
            {
                _player.transform.position = spawnPos;
            }

            // Ensure player is active
            _player.SetActive(true);

            // Re-apply ability settings (uses current unlocked state, not initial)
            ApplyAbilitySettings();

            if (_showDebugMessages)
                Debug.Log($"GameManager: Player spawned at {spawnPos}. Dash: {_dashEnabled}, Clone: {_timeCloneEnabled}");
        }

        /// <summary>
        /// Called when player dies - respawn at checkpoint
        /// </summary>
        public void OnPlayerDeath()
        {
            _deathCount++;

            if (_showDebugMessages)
                Debug.Log($"GameManager: Player died. Deaths: {_deathCount}");

            // Destroy any active clones
            if (_cloneRecorder != null)
            {
                _cloneRecorder.DestroyAllClones();
            }
            
            if (_inputCloneRecorder != null)
            {
                _inputCloneRecorder.DestroyAllClones();
            }

            // Respawn player
            SpawnPlayer();
        }

        /// <summary>
        /// Set a new checkpoint position
        /// </summary>
        public void SetCheckpoint(Vector2 position)
        {
            _currentCheckpoint = position;
            _hasCheckpoint = true;

            if (_showDebugMessages)
                Debug.Log($"GameManager: Checkpoint set at {position}");
        }

        /// <summary>
        /// Called when player reaches the level end
        /// </summary>
        public void CompleteLevel()
        {
            if (_levelComplete) return;
            
            _levelComplete = true;
            float completionTime = LevelTime;

            if (_showDebugMessages)
                Debug.Log($"GameManager: {_levelName} complete! Time: {completionTime:F2}s, Deaths: {_deathCount}");

            // Disable player movement
            if (_playerController != null)
            {
                _playerController.SetMovementEnabled(false);
            }

            // Show completion panel
            ShowCompletionPanel();
        }

        /// <summary>
        /// Show the completion UI panel
        /// </summary>
        private void ShowCompletionPanel()
        {
            // If panel is assigned, use it
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                return;
            }

            // Otherwise create one dynamically
            CreateCompletionPanel();
        }

        /// <summary>
        /// Creates a completion panel dynamically if none is assigned
        /// </summary>
        private void CreateCompletionPanel()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CompletionCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create panel
            var panel = new GameObject("CompletionPanel");
            panel.transform.SetParent(canvas.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);

            // Create text
            var textObj = new GameObject("CompletionText");
            textObj.transform.SetParent(panel.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800, 200);
            textRect.anchoredPosition = Vector2.zero;

            // Try TextMeshPro first, fall back to legacy Text
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "Congratulations!\nYou have completed the playtest";
                tmp.fontSize = 48;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            _completionPanel = panel;
            
            if (_showDebugMessages)
                Debug.Log("GameManager: Created completion panel");
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Get current checkpoint position
        /// </summary>
        public Vector2 GetCheckpoint()
        {
            return _hasCheckpoint ? _currentCheckpoint : (Vector2)_spawnPoint.position;
        }

        // Visualise spawn and checkpoint in editor
        private void OnDrawGizmos()
        {
            // Draw spawn point
            if (_spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_spawnPoint.position, 0.5f);
                Gizmos.DrawLine(_spawnPoint.position + Vector3.left * 0.3f, _spawnPoint.position + Vector3.right * 0.3f);
                Gizmos.DrawLine(_spawnPoint.position + Vector3.up * 0.3f, _spawnPoint.position + Vector3.down * 0.3f);
            }

            // Draw current checkpoint (play mode only)
            if (Application.isPlaying && _hasCheckpoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentCheckpoint, 0.4f);
            }

            // Draw unlock triggers
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
            if (_dashUnlockTrigger != null)
            {
                Gizmos.DrawWireCube(_dashUnlockTrigger.bounds.center, _dashUnlockTrigger.bounds.size);
            }
            
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f); // Purple
            if (_timeCloneUnlockTrigger != null)
            {
                Gizmos.DrawWireCube(_timeCloneUnlockTrigger.bounds.center, _timeCloneUnlockTrigger.bounds.size);
            }
            
            // Draw level end trigger
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f); // Cyan/Green
            if (_levelEndTrigger != null)
            {
                Gizmos.DrawWireCube(_levelEndTrigger.bounds.center, _levelEndTrigger.bounds.size);
            }
        }
    }

    /// <summary>
    /// Helper component added to unlock trigger colliders
    /// </summary>
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

            // Only trigger for real player (not clones, not DashSprite)
            if (!other.TryGetComponent<UltimatePlayerController>(out _)) return;
            if (other.GetComponent<TimeClone>() != null) return;
            if (other.GetComponent<CloneMovement>() != null) return;

            _hasTriggered = true;

            switch (_abilityType)
            {
                case AbilityType.Dash:
                    _gameManager.UnlockDash();
                    break;
                case AbilityType.TimeClone:
                    _gameManager.UnlockTimeClone();
                    break;
            }
        }
    }

    /// <summary>
    /// Helper component added to level end trigger collider
    /// </summary>
    public class LevelEndTrigger : MonoBehaviour
    {
        private GameManager _gameManager;
        private bool _hasTriggered;

        public void Initialize(GameManager manager)
        {
            _gameManager = manager;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;

            // Only trigger for real player (not clones)
            if (!other.TryGetComponent<UltimatePlayerController>(out _)) return;
            if (other.GetComponent<TimeClone>() != null) return;
            if (other.GetComponent<CloneMovement>() != null) return;

            _hasTriggered = true;
            _gameManager.CompleteLevel();
        }
    }
}