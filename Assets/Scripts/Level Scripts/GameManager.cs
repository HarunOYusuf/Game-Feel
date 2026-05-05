using UnityEngine;
using UnityEngine.SceneManagement;

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

        // Current ability states (can be unlocked during gameplay)
        private bool _dashEnabled;
        private bool _timeCloneEnabled;

        // Stats
        private int _deathCount;
        private float _levelStartTime;

        // Public accessors
        public string LevelName => _levelName;
        public int DeathCount => _deathCount;
        public float LevelTime => Time.time - _levelStartTime;
        public bool DashEnabled => _dashEnabled;
        public bool TimeCloneEnabled => _timeCloneEnabled;

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

            // Apply ability settings
            ApplyAbilitySettings();

            // Spawn player at start
            SpawnPlayer();

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
            float completionTime = LevelTime;

            if (_showDebugMessages)
                Debug.Log($"GameManager: {_levelName} complete! Time: {completionTime:F2}s, Deaths: {_deathCount}");

            // Load next level if specified
            if (!string.IsNullOrEmpty(_nextLevelScene))
            {
                SceneManager.LoadScene(_nextLevelScene);
            }
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
}