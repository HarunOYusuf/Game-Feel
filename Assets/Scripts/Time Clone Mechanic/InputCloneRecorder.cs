using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        // Input tracking
        private bool _jumpPressedThisFrame;
        private bool _dashPressedThisFrame;
        private bool _lastJumpState;
        private bool _lastDashState;
        private bool _lastCloneButtonState;

        // Properties
        public bool IsRecording => _isRecording;
        public bool RecordingEnabled { get; private set; } = true;
        public float RecordingTime => _isRecording ? Time.time - _recordingStartTime : 0f;
        public int ActiveCloneCount => _activeClones.Count;

        private void Awake()
        {
            _playerController = GetComponent<UltimatePlayerController>();
        }

        private void Update()
        {
            // Check clone button (Square on PlayStation = JoystickButton2, but you said JoystickButton3)
            bool cloneButtonPressed = Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.Q);
            
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

            // Track jump/dash presses (edge detection)
            bool currentJump = Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.Space);
            bool currentDash = Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.LeftShift);
            
            _jumpPressedThisFrame = currentJump && !_lastJumpState;
            _dashPressedThisFrame = currentDash && !_lastDashState;
            
            _lastJumpState = currentJump;
            _lastDashState = currentDash;
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

            if (_showDebugMessages)
                Debug.Log("InputCloneRecorder: Recording started");
        }

        private void RecordFrame()
        {
            float timestamp = Time.time - _recordingStartTime;
            
            // Get current inputs
            float horizontal = Input.GetAxisRaw("Horizontal");
            bool jumpHeld = Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.Space);

            var snapshot = new CloneInputSnapshot(
                timestamp,
                horizontal,
                _jumpPressedThisFrame,
                jumpHeld,
                _dashPressedThisFrame,
                transform.position,
                _playerController != null ? _playerController.FacingDirection : 1
            );

            _currentRecording.Add(snapshot);
            
            // Reset edge detection
            _jumpPressedThisFrame = false;
            _dashPressedThisFrame = false;
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
                Debug.Log($"InputCloneRecorder: Spawned clone with {_currentRecording.Count} input frames");
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