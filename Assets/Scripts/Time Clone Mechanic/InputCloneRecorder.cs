using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Records player INPUTS for clone playback.
    /// The clone will replay these inputs with real physics.
    /// 
    /// Setup:
    /// 1. Add to Player GameObject (same as UltimatePlayerController)
    /// 2. Assign Clone Prefab (must have CloneMovement component)
    /// 3. Configure recording settings
    /// </summary>
    public class InputCloneRecorder : MonoBehaviour
    {
        [Header("Clone Prefab")]
        [Tooltip("Prefab with CloneMovement component")]
        [SerializeField] private GameObject _clonePrefab;

        [Header("Recording Settings")]
        [Tooltip("Maximum recording duration in seconds")]
        [SerializeField] private float _maxRecordingTime = 10f;

        [Header("Settings")]
        [Tooltip("Destroy clone when playback completes?")]
        [SerializeField] private bool _destroyOnComplete = false;
        
        [Tooltip("Maximum number of active clones")]
        [SerializeField] private int _maxClones = 3;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        // State
        private bool _isRecording;
        private float _recordingStartTime;
        private List<CloneInputSnapshot> _currentRecording = new List<CloneInputSnapshot>();
        private List<CloneMovement> _activeClones = new List<CloneMovement>();
        
        // Components
        private UltimatePlayerController _playerController;
        
        // Event-based action tracking (survives until FixedUpdate reads them)
        private bool _playerJumpedThisFrame;
        private bool _playerDashedThisFrame;
        private Vector2 _playerDashDirection;

        // Properties
        public bool IsRecording => _isRecording;
        public bool RecordingEnabled { get; private set; } = true;
        public float RecordingTime => _isRecording ? Time.time - _recordingStartTime : 0f;
        public int ActiveCloneCount => _activeClones.Count;

        private void Awake()
        {
            _playerController = GetComponent<UltimatePlayerController>();
            
            if (_playerController == null)
            {
                Debug.LogError("InputCloneRecorder: No UltimatePlayerController found on this GameObject!");
            }
        }

        private void OnEnable()
        {
            if (_playerController != null)
            {
                // Subscribe to player events
                _playerController.Jumped += OnPlayerJumped;
                _playerController.DashChanged += OnPlayerDashChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                // Unsubscribe from player events
                _playerController.Jumped -= OnPlayerJumped;
                _playerController.DashChanged -= OnPlayerDashChanged;
            }
        }

        private void OnPlayerJumped()
        {
            // Player actually jumped - mark it for recording
            _playerJumpedThisFrame = true;
            
            if (_showDebugMessages && _isRecording)
                Debug.Log($"InputCloneRecorder: Player JUMPED at {RecordingTime:F2}s");
        }

        private void OnPlayerDashChanged(bool isDashing)
        {
            // Player started dashing
            if (isDashing)
            {
                _playerDashedThisFrame = true;
                
                // Capture the dash direction from current input
                Vector2 moveInput = _playerController.Input;
                if (moveInput != Vector2.zero)
                {
                    _playerDashDirection = moveInput.normalized;
                }
                else
                {
                    // Dash in facing direction
                    _playerDashDirection = _playerController.FacingDirection > 0 ? Vector2.right : Vector2.left;
                }
                
                if (_showDebugMessages && _isRecording)
                    Debug.Log($"InputCloneRecorder: Player DASHED at {RecordingTime:F2}s, direction: {_playerDashDirection}");
            }
        }

        private void Update()
        {
            // Check clone button
            bool cloneButtonPressed = Input.GetKeyDown(KeyCode.Q);
            
            if (cloneButtonPressed && RecordingEnabled)
            {
                if (_isRecording)
                {
                    StopRecordingAndSpawn();
                }
                else
                {
                    StartRecording();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_isRecording)
            {
                RecordFrame();
                
                // Check max time
                if (RecordingTime >= _maxRecordingTime)
                {
                    StopRecordingAndSpawn();
                }
            }
            
            // Clean up destroyed clones
            _activeClones.RemoveAll(c => c == null);
        }

        public void SetRecordingEnabled(bool enabled)
        {
            RecordingEnabled = enabled;
            
            if (!enabled && _isRecording)
            {
                CancelRecording();
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            _recordingStartTime = Time.time;
            _currentRecording.Clear();
            _playerJumpedThisFrame = false;
            _playerDashedThisFrame = false;
            _playerDashDirection = Vector2.zero;

            if (_showDebugMessages)
                Debug.Log("InputCloneRecorder: Recording started");
        }

        private void RecordFrame()
        {
            if (_playerController == null) return;
            
            float timestamp = Time.time - _recordingStartTime;
            
            // Get movement input from player controller
            Vector2 moveInput = _playerController.Input;
            
            // Get player state
            bool isGrounded = _playerController.IsGrounded;
            bool isWallSliding = _playerController.IsWallSliding;
            int wallDirection = _playerController.WallDirection;
            
            // Check if jump is being held (for variable jump height)
            bool jumpHeld = Input.GetKey(KeyCode.Space);

            var snapshot = new CloneInputSnapshot(
                timestamp,
                moveInput.x,
                _playerJumpedThisFrame,    // Use event-based detection
                jumpHeld,
                _playerDashedThisFrame,    // Use event-based detection
                _playerDashDirection,       // Recorded dash direction
                isGrounded,
                isWallSliding,
                wallDirection,
                transform.position,
                _playerController.FacingDirection
            );

            _currentRecording.Add(snapshot);
            
            // Reset action flags AFTER recording
            _playerJumpedThisFrame = false;
            _playerDashedThisFrame = false;
            _playerDashDirection = Vector2.zero;
        }

        private void StopRecordingAndSpawn()
        {
            _isRecording = false;

            if (_currentRecording.Count < 2)
            {
                if (_showDebugMessages)
                    Debug.Log("InputCloneRecorder: Recording too short, cancelled");
                return;
            }

            // Count jumps and dashes for debug
            int jumpCount = 0;
            int dashCount = 0;
            foreach (var snap in _currentRecording)
            {
                if (snap.JumpPressed) jumpCount++;
                if (snap.DashPressed) dashCount++;
            }

            // Limit active clones
            while (_activeClones.Count >= _maxClones)
            {
                var oldest = _activeClones[0];
                _activeClones.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }

            // Spawn clone
            SpawnClone();

            if (_showDebugMessages)
                Debug.Log($"InputCloneRecorder: Spawned clone with {_currentRecording.Count} frames, {jumpCount} jumps, {dashCount} dashes");
        }

        private void SpawnClone()
        {
            if (_clonePrefab == null)
            {
                Debug.LogError("InputCloneRecorder: No clone prefab assigned!");
                return;
            }

            // Get starting position from first snapshot
            Vector2 startPos = _currentRecording[0].StartPosition;
            int startFacing = _currentRecording[0].StartFacingDirection;

            // Instantiate clone
            var cloneObj = Instantiate(_clonePrefab, startPos, Quaternion.identity);
            cloneObj.name = $"InputClone_{_activeClones.Count}";

            // Get CloneMovement and start playback
            var cloneMovement = cloneObj.GetComponent<CloneMovement>();
            if (cloneMovement != null)
            {
                cloneMovement.StartPlayback(new List<CloneInputSnapshot>(_currentRecording), startFacing);
                cloneMovement.OnPlaybackComplete += () => HandleCloneComplete(cloneMovement);
                _activeClones.Add(cloneMovement);
            }
            else
            {
                Debug.LogError("InputCloneRecorder: Clone prefab missing CloneMovement component!");
                Destroy(cloneObj);
            }
        }

        private void CancelRecording()
        {
            _isRecording = false;
            _currentRecording.Clear();

            if (_showDebugMessages)
                Debug.Log("InputCloneRecorder: Recording cancelled");
        }

        private void HandleCloneComplete(CloneMovement clone)
        {
            if (_destroyOnComplete && clone != null)
            {
                Destroy(clone.gameObject);
            }
        }

        public void DestroyAllClones()
        {
            foreach (var clone in _activeClones)
            {
                if (clone != null)
                {
                    Destroy(clone.gameObject);
                }
            }
            _activeClones.Clear();
        }

        private void OnDestroy()
        {
            DestroyAllClones();
        }
    }
}