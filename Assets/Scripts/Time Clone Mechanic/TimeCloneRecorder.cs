using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Snapshot of player state at a moment in time.
    /// </summary>
    [System.Serializable]
    public struct TimeSnapshot
    {
        public float Timestamp;
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsGrounded;
        public bool IsWallSliding;
        public bool IsDashing;
        public int FacingDirection;
        public int WallDirection;
    }

    /// <summary>
    /// Records player movement and spawns clones that replay past actions.
    /// Attach to player alongside UltimatePlayerController.
    /// </summary>
    public class TimeCloneRecorder : MonoBehaviour
    {
        [Header("Recording")]
        [SerializeField, Tooltip("Maximum seconds of recording to keep")]
        private float _maxRecordTime = 5f;
        
        [SerializeField, Tooltip("Snapshots per second (higher = smoother)")]
        private int _snapshotsPerSecond = 60;

        [Header("Clone Settings")]
        [SerializeField, Tooltip("Clone prefab (needs TimeClone component)")]
        private GameObject _clonePrefab;
        
        [SerializeField, Tooltip("Seconds of recording to replay")]
        private float _cloneDuration = 5f;
        
        [SerializeField, Tooltip("Max simultaneous clones")]
        private int _maxClones = 3;
        
        [SerializeField, Tooltip("Destroy clone when playback ends")]
        private bool _destroyOnComplete = true;

        [Header("Input")]
        [SerializeField] private KeyCode _spawnKey = KeyCode.R;
        [SerializeField] private KeyCode _spawnJoystickButton = KeyCode.JoystickButton2;
        [SerializeField] private float _spawnCooldown = 0.5f;

        // Dependencies
        private IPlayerController _controller;
        private Rigidbody2D _rb;

        // Recording state
        private List<TimeSnapshot> _snapshots = new List<TimeSnapshot>();
        private float _recordInterval;
        private float _lastRecordTime;
        private float _currentTime;

        // Clone management
        private List<TimeClone> _activeClones = new List<TimeClone>();
        private float _lastSpawnTime = float.MinValue;

        // Events
        public event Action<TimeClone> OnCloneSpawned;
        public event Action<TimeClone> OnCloneDestroyed;

        // Public accessors
        public int ActiveCloneCount => _activeClones.Count;
        public bool CanSpawnClone => Time.time >= _lastSpawnTime + _spawnCooldown && 
                                     _activeClones.Count < _maxClones;
        public float CloneDuration { get => _cloneDuration; set => _cloneDuration = Mathf.Max(0.1f, value); }

        private void Awake()
        {
            _controller = GetComponent<IPlayerController>();
            _rb = GetComponent<Rigidbody2D>();

            if (_controller == null)
            {
                Debug.LogError("TimeCloneRecorder requires an IPlayerController!", this);
                enabled = false;
                return;
            }

            _recordInterval = 1f / _snapshotsPerSecond;
            _snapshots = new List<TimeSnapshot>(Mathf.CeilToInt(_maxRecordTime * _snapshotsPerSecond));
        }

        private void Update()
        {
            if (Input.GetKeyDown(_spawnKey) || Input.GetKeyDown(_spawnJoystickButton))
            {
                TrySpawnClone();
            }

            _activeClones.RemoveAll(c => c == null);
        }

        private void FixedUpdate()
        {
            _currentTime += Time.fixedDeltaTime;

            if (_currentTime >= _lastRecordTime + _recordInterval)
            {
                RecordSnapshot();
                _lastRecordTime = _currentTime;
            }

            TrimOldSnapshots();
        }

        private void RecordSnapshot()
        {
            // Check if player is dashing (via velocity or interface if available)
            bool isDashing = false;
            var playerController = GetComponent<UltimatePlayerController>();
            if (playerController != null)
            {
                // Access dash state through reflection or public property
                var dashField = typeof(UltimatePlayerController).GetField("_isDashing", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dashField != null)
                {
                    isDashing = (bool)dashField.GetValue(playerController);
                }
            }

            _snapshots.Add(new TimeSnapshot
            {
                Timestamp = _currentTime,
                Position = _rb.position,
                Velocity = _controller.Velocity,
                IsGrounded = _controller.IsGrounded,
                IsWallSliding = _controller.IsWallSliding,
                IsDashing = isDashing,
                FacingDirection = _controller.FacingDirection,
                WallDirection = _controller.WallDirection
            });
        }

        private void TrimOldSnapshots()
        {
            float cutoff = _currentTime - _maxRecordTime;
            while (_snapshots.Count > 0 && _snapshots[0].Timestamp < cutoff)
            {
                _snapshots.RemoveAt(0);
            }
        }

        private List<TimeSnapshot> GetSnapshots(float seconds)
        {
            float cutoff = _currentTime - Mathf.Min(seconds, _maxRecordTime);
            var result = new List<TimeSnapshot>();

            foreach (var snap in _snapshots)
            {
                if (snap.Timestamp >= cutoff)
                {
                    var adjusted = snap;
                    adjusted.Timestamp -= cutoff;
                    result.Add(adjusted);
                }
            }
            return result;
        }

        public TimeClone TrySpawnClone()
        {
            if (!CanSpawnClone) return null;

            var snapshots = GetSnapshots(_cloneDuration);
            if (snapshots.Count == 0)
            {
                Debug.LogWarning("No recorded snapshots available!");
                return null;
            }

            var clone = CreateClone(snapshots);
            if (clone != null)
            {
                _lastSpawnTime = Time.time;
                _activeClones.Add(clone);
                clone.OnPlaybackComplete += () => HandleCloneComplete(clone);
                OnCloneSpawned?.Invoke(clone);
            }

            return clone;
        }

        private TimeClone CreateClone(List<TimeSnapshot> snapshots)
        {
            GameObject cloneObj;

            if (_clonePrefab != null)
            {
                cloneObj = Instantiate(_clonePrefab);
            }
            else
            {
                cloneObj = CreateBasicClone();
            }

            cloneObj.name = $"TimeClone_{_activeClones.Count}";

            var clone = cloneObj.GetComponent<TimeClone>();
            if (clone == null) clone = cloneObj.AddComponent<TimeClone>();

            clone.StartPlayback(snapshots);
            return clone;
        }

        private GameObject CreateBasicClone()
        {
            var obj = new GameObject("TimeClone");

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Add collider but disable it - clone is visual only
            // Enable if you need clone to interact with pressure plates etc.
            var col = obj.AddComponent<CapsuleCollider2D>();
            col.isTrigger = true;
            col.enabled = false; // Prevents interfering with player collision

            var playerCol = GetComponent<CapsuleCollider2D>();
            if (playerCol != null)
            {
                col.size = playerCol.size;
                col.offset = playerCol.offset;
                col.direction = playerCol.direction;
            }

            // Find sprite renderer (check children too)
            var playerSr = GetComponent<SpriteRenderer>();
            if (playerSr == null)
            {
                playerSr = GetComponentInChildren<SpriteRenderer>();
            }

            // Find animator (check children too)
            var playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }

            // Create sprite child object (matching player structure)
            var spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(obj.transform);
            spriteObj.transform.localPosition = Vector3.zero;

            var sr = spriteObj.AddComponent<SpriteRenderer>();
            if (playerSr != null)
            {
                sr.sprite = playerSr.sprite;
                sr.color = new Color(1f, 1f, 1f, 0.5f);
                sr.sortingLayerID = playerSr.sortingLayerID;
                sr.sortingOrder = playerSr.sortingOrder - 1;
            }
            else
            {
                sr.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                sr.sortingOrder = -1;
            }

            // Copy animator controller if exists
            if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
            {
                var cloneAnimator = spriteObj.AddComponent<Animator>();
                cloneAnimator.runtimeAnimatorController = playerAnimator.runtimeAnimatorController;
            }

            return obj;
        }

        private void HandleCloneComplete(TimeClone clone)
        {
            if (_destroyOnComplete && clone != null)
            {
                OnCloneDestroyed?.Invoke(clone);
                Destroy(clone.gameObject);
            }
        }

        public void DestroyAllClones()
        {
            foreach (var clone in _activeClones)
            {
                if (clone != null)
                {
                    OnCloneDestroyed?.Invoke(clone);
                    Destroy(clone.gameObject);
                }
            }
            _activeClones.Clear();
        }

        private void OnDestroy() => DestroyAllClones();
    }
}