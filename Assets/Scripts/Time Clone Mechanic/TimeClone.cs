using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Clone entity that replays recorded player snapshots.
    /// Spawned by TimeCloneRecorder.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TimeClone : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private bool _interpolatePosition = true;
        [SerializeField] private float _playbackSpeed = 1f;

        [Header("Animation (Optional)")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _groundedParam = "IsGrounded";
        [SerializeField] private string _verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string _dashingParam = "IsDashing";
        [SerializeField] private string _wallSlidingParam = "IsWallSliding";
        [SerializeField] private string _jumpTrigger = "Jump";

        [Header("Visuals")]
        [SerializeField] private bool _flipSprite = true;
        [SerializeField] private float _ghostAlpha = 0.5f;

        [Header("Particles")]
        [Tooltip("Particle system to play when clone spawns (assign in prefab)")]
        [SerializeField] private ParticleSystem _spawnParticles;
        
        [Tooltip("Particle system to play when clone despawns (assign in prefab)")]
        [SerializeField] private ParticleSystem _despawnParticles;

        // Components
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Animator _animator;
        private Collider2D _collider;

        // Playback state
        private List<TimeSnapshot> _snapshots;
        private int _currentIndex;
        private float _playbackTime;
        private bool _isPlaying;
        private Vector2 _lastPosition;
        private bool _wasGrounded;

        // Events
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackComplete;

        // Public state
        public bool IsPlaying => _isPlaying;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            
            // Find sprite renderer (check children too)
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
            {
                _sr = GetComponentInChildren<SpriteRenderer>();
            }
            
            // Find animator (check children too)
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.None;

            // Make sure collider is a trigger so we can detect zones
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (_sr != null)
            {
                var c = _sr.color;
                c.a = _ghostAlpha;
                _sr.color = c;
            }
        }

        public void StartPlayback(List<TimeSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                Debug.LogWarning("TimeClone: No snapshots for playback!");
                Destroy(gameObject);
                return;
            }

            _snapshots = snapshots;
            _currentIndex = 0;
            _playbackTime = 0f;
            _isPlaying = true;

            var first = _snapshots[0];
            _rb.position = first.Position;
            _lastPosition = first.Position;
            _wasGrounded = first.IsGrounded;
            FacingDirection = first.FacingDirection;

            // Play spawn particles
            PlaySpawnEffect();

            OnPlaybackStarted?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!_isPlaying || _snapshots == null) return;

            _playbackTime += Time.fixedDeltaTime * _playbackSpeed;
            UpdatePlayback();
        }

        private void UpdatePlayback()
        {
            // Advance to correct snapshot
            while (_currentIndex < _snapshots.Count - 1 &&
                   _snapshots[_currentIndex + 1].Timestamp <= _playbackTime)
            {
                _currentIndex++;
            }

            // Check completion
            if (_currentIndex >= _snapshots.Count - 1)
            {
                CompletePlayback();
                return;
            }

            var current = _snapshots[_currentIndex];
            var next = _snapshots[_currentIndex + 1];

            // Update position
            Vector2 targetPos;
            if (_interpolatePosition)
            {
                float t = Mathf.InverseLerp(current.Timestamp, next.Timestamp, _playbackTime);
                targetPos = Vector2.Lerp(current.Position, next.Position, t);
            }
            else
            {
                targetPos = current.Position;
            }

            // Stabilize Y when grounded to prevent bouncing
            if (current.IsGrounded && next.IsGrounded)
            {
                targetPos.y = current.Position.y;
            }

            _rb.position = targetPos;

            // Update facing direction
            FacingDirection = current.FacingDirection;
            int visualDirection = FacingDirection;
            
            if (current.IsWallSliding)
            {
                visualDirection = -current.WallDirection;
            }
            
            if (_flipSprite && _sr != null)
            {
                Vector3 scale = _sr.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * visualDirection;
                _sr.transform.localScale = scale;
            }

            // Update animator if present
            if (_animator != null)
            {
                Vector2 velocity = (_rb.position - _lastPosition) / Time.fixedDeltaTime;
                
                _animator.SetFloat(_speedParam, Mathf.Abs(velocity.x));
                _animator.SetFloat(_verticalVelocityParam, current.Velocity.y);
                _animator.SetBool(_groundedParam, current.IsGrounded);
                _animator.SetBool(_wallSlidingParam, current.IsWallSliding);
                _animator.SetBool(_dashingParam, current.IsDashing);

                if (_wasGrounded && !current.IsGrounded && current.Velocity.y > 0)
                {
                    _animator.SetTrigger(_jumpTrigger);
                }
            }
            
            _wasGrounded = current.IsGrounded;
            _lastPosition = _rb.position;
        }

        private void CompletePlayback()
        {
            _isPlaying = false;

            if (_snapshots.Count > 0)
            {
                _rb.position = _snapshots[_snapshots.Count - 1].Position;
            }

            // Play despawn particles
            PlayDespawnEffect();

            OnPlaybackComplete?.Invoke();
        }

        public void StopPlayback()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                PlayDespawnEffect();
                OnPlaybackComplete?.Invoke();
            }
        }

        #region Colour Zone Detection

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if we entered a ColourZone
            var colourZone = other.GetComponent<ColourZone>();
            if (colourZone != null)
            {
                // Destroy clone if zone blocks clones (Blue or Purple)
                if (colourZone.DestroysClones)
                {
                    DestroyClone();
                }
            }
        }

        /// <summary>
        /// Immediately destroy this clone with despawn effect
        /// </summary>
        public void DestroyClone()
        {
            if (!_isPlaying) return;
            
            _isPlaying = false;
            PlayDespawnEffect();
            OnPlaybackComplete?.Invoke();
            
            // Destroy after a tiny delay to let particles detach
            Destroy(gameObject, 0.05f);
        }

        #endregion

        #region Particle Effects

        /// <summary>
        /// Play the spawn particle effect
        /// </summary>
        private void PlaySpawnEffect()
        {
            if (_spawnParticles != null)
            {
                _spawnParticles.Play();
            }
        }

        /// <summary>
        /// Play the despawn particle effect.
        /// Detaches particles so they finish playing after clone is destroyed.
        /// </summary>
        private void PlayDespawnEffect()
        {
            if (_despawnParticles != null)
            {
                // Detach from parent so particles survive after clone is destroyed
                _despawnParticles.transform.SetParent(null);
                _despawnParticles.Play();
                
                // Destroy particle system after it finishes
                float lifetime = _despawnParticles.main.duration + _despawnParticles.main.startLifetime.constantMax;
                Destroy(_despawnParticles.gameObject, lifetime);
            }
        }

        #endregion
    }
}