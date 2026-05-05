using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Clone entity that replays recorded player snapshots.
    /// After playback ends, clone stays in place and can be affected by physics.
    /// Spawned by TimeCloneRecorder.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TimeClone : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private bool _interpolatePosition = true;
        [SerializeField] private float _playbackSpeed = 1f;

        [Header("Physics During Playback")]
        [Tooltip("Check for ground during playback - if no ground, clone falls")]
        [SerializeField] private bool _checkGroundDuringPlayback = true;
        
        [Tooltip("How far below to check for ground")]
        [SerializeField] private float _groundCheckDistance = 0.2f;
        
        [Tooltip("Layers considered as ground")]
        [SerializeField] private LayerMask _groundLayer;

        [Header("After Playback")]
        [Tooltip("Enable gravity after playback ends (clone can fall)")]
        [SerializeField] private bool _enableGravityAfterPlayback = true;
        
        [Tooltip("Gravity scale when falling")]
        [SerializeField] private float _gravityScale = 3f;

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

        [Header("Ground Detection")]
        [SerializeField] private float _fallCheckWidth = 0.3f;

        // Components
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Animator _animator;
        private Collider2D _collider;
        private CapsuleCollider2D _capsuleCollider;

        // Playback state
        private List<TimeSnapshot> _snapshots;
        private int _currentIndex;
        private float _playbackTime;
        private bool _isPlaying;
        private bool _playbackComplete;
        private bool _isFalling;
        private bool _hasFallenOff;
        private Vector2 _lastPosition;
        private bool _wasGrounded;

        // Events
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackComplete;

        // Public state
        public bool IsPlaying => _isPlaying;
        public bool PlaybackComplete => _playbackComplete;
        public bool IsFalling => _isFalling;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            
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

            // IMPORTANT: Start as kinematic during playback - we control position manually
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = true; // Make sure physics is simulated
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Collider must NOT be trigger - clone needs to stand on platforms and fall
            if (_collider != null)
            {
                _collider.isTrigger = false;
            }

            // Ignore collision between clone and player so they don't push each other
            IgnorePlayerCollision();

            // Ghost visual
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = _ghostAlpha;
                _sr.color = c;
            }

            // Set default ground layer if not set
            if (_groundLayer == 0)
            {
                _groundLayer = LayerMask.GetMask("Ground", "Default");
            }
        }

        private void IgnorePlayerCollision()
        {
            // Find the real player and ignore collision with them
            var allControllers = FindObjectsByType<UltimatePlayerController>(FindObjectsSortMode.None);
            foreach (var controller in allControllers)
            {
                // Skip if this is a clone
                if (controller.GetComponent<TimeClone>() != null)
                    continue;

                // Get all colliders on the player
                var playerColliders = controller.GetComponents<Collider2D>();
                var playerChildColliders = controller.GetComponentsInChildren<Collider2D>();

                // Get all colliders on this clone
                var myColliders = GetComponents<Collider2D>();
                var myChildColliders = GetComponentsInChildren<Collider2D>();

                // Ignore collision between all player colliders and all clone colliders
                foreach (var playerCol in playerColliders)
                {
                    foreach (var myCol in myColliders)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }
                    foreach (var myCol in myChildColliders)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }
                }
                foreach (var playerCol in playerChildColliders)
                {
                    foreach (var myCol in myColliders)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }
                    foreach (var myCol in myChildColliders)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }
                }
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
            _playbackComplete = false;

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
            // If falling, just let physics handle it
            if (_isFalling)
            {
                UpdateIdleAnimator();
                return;
            }

            if (_isPlaying && _snapshots != null)
            {
                _playbackTime += Time.fixedDeltaTime * _playbackSpeed;
                UpdatePlayback();
            }
            else if (_playbackComplete)
            {
                // After playback, check if ground disappears
                if (!_hasFallenOff && !CheckGroundExists())
                {
                    _hasFallenOff = true;
                    _isFalling = true;
                    Debug.Log("TimeClone: Ground gone after playback! Falling now.");
                }
                
                UpdateIdleAnimator();
            }
        }

        private bool IsGroundBelow()
        {
            Vector2 origin = (Vector2)transform.position;
            
            // Adjust origin based on collider
            if (_capsuleCollider != null)
            {
                origin += _capsuleCollider.offset;
                origin.y -= _capsuleCollider.size.y / 2f;
            }
            else if (_collider != null)
            {
                origin.y -= _collider.bounds.extents.y;
            }

            // Cast a box downward to check for ground
            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                new Vector2(_fallCheckWidth, 0.05f),
                0f,
                Vector2.down,
                _groundCheckDistance,
                _groundLayer
            );

            // Debug visualization - GREEN = ground found, RED = no ground
            Debug.DrawRay(origin, Vector2.down * _groundCheckDistance, hit.collider != null ? Color.green : Color.red, 0.1f);

            return hit.collider != null;
        }

        private void StartFalling()
        {
            _isPlaying = false;
            _isFalling = true;
            
            // Enable physics
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = _gravityScale;
            _rb.freezeRotation = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.linearVelocity = Vector2.zero;
            
            // Make sure collider is solid
            if (_collider != null)
            {
                _collider.isTrigger = false;
            }

            Debug.Log("TimeClone: Ground disappeared! Clone is now falling.");
        }

        private void UpdateFallingAnimator()
        {
            if (_animator == null) return;

            bool isGrounded = IsGroundBelow() && Mathf.Abs(_rb.linearVelocity.y) < 0.1f;
            
            _animator.SetFloat(_speedParam, 0f);
            _animator.SetFloat(_verticalVelocityParam, _rb.linearVelocity.y);
            _animator.SetBool(_groundedParam, isGrounded);
            _animator.SetBool(_wallSlidingParam, false);
            _animator.SetBool(_dashingParam, false);
        }

        private void UpdatePlayback()
        {
            // If we've switched to falling mode, don't update position from snapshots
            if (_isFalling)
            {
                return;
            }

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

            // CHECK: If recording says we're grounded but there's no ground, start falling
            if (current.IsGrounded && Mathf.Abs(current.Velocity.y) < 0.1f)
            {
                if (!CheckGroundExists())
                {
                    StartFallingMidPlayback();
                    return;
                }
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

        private bool CheckGroundExists()
        {
            Vector2 origin = (Vector2)transform.position;
            
            // Adjust origin to feet
            if (_capsuleCollider != null)
            {
                origin += _capsuleCollider.offset;
                origin.y -= _capsuleCollider.size.y / 2f;
            }

            // Raycast down to check for ground
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _groundCheckDistance, _groundLayer);
            
            return hit.collider != null;
        }

        private void StartFallingMidPlayback()
        {
            Debug.Log("TimeClone: Ground disappeared during playback! Switching to physics.");
            
            _isPlaying = false;
            _isFalling = true;
            _playbackComplete = true;
            
            // Enable physics
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = _gravityScale;
            _rb.simulated = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.WakeUp();
            _rb.linearVelocity = new Vector2(0f, -0.5f); // Small downward nudge
            
            if (_collider != null)
            {
                _collider.isTrigger = false;
            }
        }

        private void CompletePlayback()
        {
            _isPlaying = false;
            _playbackComplete = true;

            if (_snapshots.Count > 0)
            {
                _rb.position = _snapshots[_snapshots.Count - 1].Position;
            }

            // Enable physics after playback so clone can fall
            if (_enableGravityAfterPlayback)
            {
                // Switch to Dynamic so physics takes over
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = _gravityScale;
                _rb.simulated = true;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.angularVelocity = 0f;
                
                // IMPORTANT: Wake up the rigidbody and give it a tiny nudge
                // This forces Unity physics to start simulating it
                _rb.WakeUp();
                _rb.linearVelocity = new Vector2(0f, -0.1f);
                
                // Collider must be solid (not trigger) to stand on things and fall
                if (_collider != null)
                {
                    _collider.isTrigger = false;
                }
                
                Debug.Log($"TimeClone: Playback complete. Physics enabled - BodyType: {_rb.bodyType}, GravityScale: {_gravityScale}, Simulated: {_rb.simulated}");
            }

            OnPlaybackComplete?.Invoke();
        }

        private void UpdateIdleAnimator()
        {
            if (_animator == null) return;

            // Check if grounded
            bool isGrounded = CheckGrounded();
            
            _animator.SetFloat(_speedParam, 0f);
            _animator.SetFloat(_verticalVelocityParam, _rb.linearVelocity.y);
            _animator.SetBool(_groundedParam, isGrounded);
            _animator.SetBool(_wallSlidingParam, false);
            _animator.SetBool(_dashingParam, false);
        }

        private bool CheckGrounded()
        {
            if (_capsuleCollider != null)
            {
                Vector2 origin = (Vector2)transform.position + _capsuleCollider.offset;
                origin.y -= _capsuleCollider.size.y / 2f;
                
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _groundCheckDistance, _groundLayer);
                return hit.collider != null;
            }
            
            // Fallback: simple raycast from center
            RaycastHit2D fallbackHit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, _groundLayer);
            return fallbackHit.collider != null;
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

        #region Trigger Detection (Colour Zones, Keys, etc.)

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

        #endregion

        /// <summary>
        /// Immediately destroy this clone with despawn effect
        /// </summary>
        public void DestroyClone()
        {
            _isPlaying = false;
            _playbackComplete = false;
            PlayDespawnEffect();
            OnPlaybackComplete?.Invoke();
            
            // Destroy after a tiny delay to let particles detach
            Destroy(gameObject, 0.05f);
        }

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