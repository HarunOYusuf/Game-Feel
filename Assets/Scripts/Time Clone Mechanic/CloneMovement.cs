using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A physics-based clone that replays recorded inputs.
    /// Unlike position-based clones, this one actually moves with physics
    /// so it will fall if platforms are removed.
    /// 
    /// Setup:
    /// 1. Create a clone prefab with Rigidbody2D and CapsuleCollider2D
    /// 2. Add this script
    /// 3. Configure movement to match player
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class CloneMovement : MonoBehaviour
    {
        [Header("Movement (Match to Player)")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _acceleration = 50f;
        [SerializeField] private float _deceleration = 50f;

        [Header("Jump (Match to Player)")]
        [SerializeField] private float _jumpForce = 14f;
        [SerializeField] private float _gravityScale = 3f;
        [SerializeField] private float _fallGravityMultiplier = 1.5f;

        [Header("Wall Slide (Match to Player)")]
        [SerializeField] private float _wallSlideSpeed = 2f;
        [SerializeField] private float _wallJumpForce = 14f;
        [SerializeField] private Vector2 _wallJumpAngle = new Vector2(1f, 1.5f);

        [Header("Dash (Match to Player)")]
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _dashDuration = 0.15f;
        [SerializeField] private float _dashCooldown = 0.3f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private float _wallCheckDistance = 0.1f;

        [Header("Lifetime")]
        [Tooltip("Destroy clone after playback completes + this many seconds (0 = never)")]
        [SerializeField] private float _idleLifetime = 5f;

        [Header("Visuals")]
        [SerializeField] private float _ghostAlpha = 0.5f;
        [SerializeField] private bool _flipSprite = true;

        [Header("Animation")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _groundedParam = "IsGrounded";
        [SerializeField] private string _verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string _wallSlidingParam = "IsWallSliding";
        [SerializeField] private string _dashingParam = "IsDashing";

        // Components
        private Rigidbody2D _rb;
        private CapsuleCollider2D _capsule;
        private SpriteRenderer _sr;
        private Animator _animator;

        // Playback
        private List<CloneInputSnapshot> _inputs;
        private int _currentIndex;
        private float _playbackTime;
        private bool _isPlaying;
        private bool _playbackComplete;

        // State
        private int _facingDirection = 1;
        private bool _isGrounded;
        private bool _wasGrounded;
        private bool _isWallSliding;
        private int _wallDirection;
        private float _idleTimer;

        // Dash state
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;

        // Events
        public event Action OnPlaybackComplete;

        // Public state
        public bool IsPlaying => _isPlaying;
        public bool IsGrounded => _isGrounded;
        public bool IsWallSliding => _isWallSliding;
        public int FacingDirection => _facingDirection;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();

            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            // Setup rigidbody for physics movement
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = _gravityScale;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Collider must not be trigger
            _capsule.isTrigger = false;

            // Ghost visual
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = _ghostAlpha;
                _sr.color = c;
            }

            // Set default layers
            if (_groundLayer == 0)
            {
                _groundLayer = LayerMask.GetMask("Ground", "Default");
            }
            if (_wallLayer == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall", "Ground", "Default");
            }

            // Ignore collision with player
            IgnorePlayerCollision();
        }

        private void IgnorePlayerCollision()
        {
            var allControllers = FindObjectsByType<UltimatePlayerController>(FindObjectsSortMode.None);
            foreach (var controller in allControllers)
            {
                if (controller.GetComponent<CloneMovement>() != null) continue;

                var playerColliders = controller.GetComponentsInChildren<Collider2D>();
                var myColliders = GetComponentsInChildren<Collider2D>();

                foreach (var playerCol in playerColliders)
                {
                    foreach (var myCol in myColliders)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }
                }
            }
        }

        public void StartPlayback(List<CloneInputSnapshot> inputs, int startFacingDirection)
        {
            if (inputs == null || inputs.Count == 0)
            {
                Debug.LogWarning("CloneMovement: No inputs to playback!");
                Destroy(gameObject);
                return;
            }

            _inputs = inputs;
            _currentIndex = 0;
            _playbackTime = 0f;
            _isPlaying = true;
            _playbackComplete = false;
            _facingDirection = startFacingDirection;

            // Set starting position
            transform.position = inputs[0].StartPosition;

            Debug.Log($"CloneMovement: Starting playback with {inputs.Count} input frames");
        }

        private void FixedUpdate()
        {
            // Update timers
            if (_dashCooldownTimer > 0)
                _dashCooldownTimer -= Time.fixedDeltaTime;

            // Always do ground and wall check
            CheckGround();
            CheckWalls();

            // Handle dash
            if (_isDashing)
            {
                ProcessDash();
                UpdateAnimator();
                return;
            }

            if (_isPlaying && _inputs != null)
            {
                _playbackTime += Time.fixedDeltaTime;
                ProcessPlayback();
            }
            else if (_playbackComplete)
            {
                // Idle timer - destroy after timeout
                if (_idleLifetime > 0)
                {
                    _idleTimer += Time.fixedDeltaTime;
                    if (_idleTimer >= _idleLifetime)
                    {
                        Debug.Log("CloneMovement: Idle timeout, destroying clone.");
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // Apply wall slide
            if (_isWallSliding)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_wallSlideSpeed);
            }

            // Apply gravity multiplier when falling
            if (!_isGrounded && !_isWallSliding && _rb.linearVelocity.y < 0)
            {
                _rb.gravityScale = _gravityScale * _fallGravityMultiplier;
            }
            else
            {
                _rb.gravityScale = _gravityScale;
            }

            // Update animator
            UpdateAnimator();

            _wasGrounded = _isGrounded;
        }

        private void CheckGround()
        {
            Vector2 origin = (Vector2)transform.position + _capsule.offset;
            origin.y -= _capsule.size.y / 2f;

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _groundCheckDistance, _groundLayer);
            _isGrounded = hit.collider != null;

            // Debug
            Debug.DrawRay(origin, Vector2.down * _groundCheckDistance, _isGrounded ? Color.green : Color.red);
        }

        private void CheckWalls()
        {
            Vector2 origin = (Vector2)transform.position + _capsule.offset;
            float checkWidth = _capsule.size.x / 2f + _wallCheckDistance;

            // Check right
            RaycastHit2D rightHit = Physics2D.Raycast(origin, Vector2.right, checkWidth, _wallLayer);
            // Check left
            RaycastHit2D leftHit = Physics2D.Raycast(origin, Vector2.left, checkWidth, _wallLayer);

            if (rightHit.collider != null)
            {
                _wallDirection = 1;
            }
            else if (leftHit.collider != null)
            {
                _wallDirection = -1;
            }
            else
            {
                _wallDirection = 0;
            }

            // Debug
            Debug.DrawRay(origin, Vector2.right * checkWidth, rightHit.collider != null ? Color.blue : Color.grey);
            Debug.DrawRay(origin, Vector2.left * checkWidth, leftHit.collider != null ? Color.blue : Color.grey);
        }

        private void ProcessPlayback()
        {
            // Find current input frame
            while (_currentIndex < _inputs.Count - 1 &&
                   _inputs[_currentIndex + 1].Timestamp <= _playbackTime)
            {
                _currentIndex++;
            }

            // Check if playback complete
            if (_currentIndex >= _inputs.Count - 1)
            {
                CompletePlayback();
                return;
            }

            // Get current input
            var input = _inputs[_currentIndex];

            // Check for dash
            if (input.DashPressed && _dashCooldownTimer <= 0 && !_isDashing)
            {
                Debug.Log($"CloneMovement: DASH triggered at playback time {_playbackTime:F2}s");
                StartDash(input.HorizontalInput);
                return;
            }

            // Check for wall slide
            bool canWallSlide = !_isGrounded && _wallDirection != 0 &&
                                ((input.HorizontalInput > 0 && _wallDirection > 0) ||
                                 (input.HorizontalInput < 0 && _wallDirection < 0));
            _isWallSliding = canWallSlide;

            // Apply jump
            if (input.JumpPressed)
            {
                if (_isGrounded)
                {
                    Debug.Log($"CloneMovement: JUMP triggered at playback time {_playbackTime:F2}s (grounded)");
                    Jump();
                }
                else if (_isWallSliding)
                {
                    Debug.Log($"CloneMovement: WALL JUMP triggered at playback time {_playbackTime:F2}s");
                    WallJump();
                }
                else
                {
                    Debug.Log($"CloneMovement: Jump input received but not grounded or wall sliding");
                }
            }

            // Apply horizontal movement (reduced during wall slide)
            if (!_isWallSliding)
            {
                ApplyMovement(input.HorizontalInput);
            }

            // Update facing direction
            if (Mathf.Abs(input.HorizontalInput) > 0.1f)
            {
                _facingDirection = input.HorizontalInput > 0 ? 1 : -1;
            }

            // Flip sprite (face away from wall when wall sliding)
            if (_flipSprite && _sr != null)
            {
                int visualDirection = _isWallSliding ? -_wallDirection : _facingDirection;
                Vector3 scale = _sr.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * visualDirection;
                _sr.transform.localScale = scale;
            }
        }

        private void ApplyMovement(float horizontal)
        {
            float targetSpeed = horizontal * _moveSpeed;
            float currentSpeed = _rb.linearVelocity.x;

            float accel = Mathf.Abs(targetSpeed) > 0.1f ? _acceleration : _deceleration;
            float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector2(newSpeed, _rb.linearVelocity.y);
        }

        private void Jump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _isGrounded = false;
        }

        private void WallJump()
        {
            // Jump away from wall
            Vector2 jumpDirection = new Vector2(-_wallDirection * _wallJumpAngle.x, _wallJumpAngle.y).normalized;
            _rb.linearVelocity = jumpDirection * _wallJumpForce;

            _isWallSliding = false;
            _facingDirection = -_wallDirection;
        }

        private void StartDash(float horizontalInput)
        {
            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;

            // Dash in facing direction or input direction
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                _dashDirection = new Vector2(Mathf.Sign(horizontalInput), 0f);
            }
            else
            {
                _dashDirection = new Vector2(_facingDirection, 0f);
            }

            // Stop gravity during dash
            _rb.gravityScale = 0f;
            _rb.linearVelocity = _dashDirection * _dashSpeed;
        }

        private void ProcessDash()
        {
            _dashTimer -= Time.fixedDeltaTime;

            // Keep dash velocity
            _rb.linearVelocity = _dashDirection * _dashSpeed;

            if (_dashTimer <= 0)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            _isDashing = false;
            _rb.gravityScale = _gravityScale;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.5f, 0f);
        }

        private void CompletePlayback()
        {
            _isPlaying = false;
            _playbackComplete = true;

            Debug.Log("CloneMovement: Playback complete. Clone now idle with physics.");

            OnPlaybackComplete?.Invoke();
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            _animator.SetFloat(_speedParam, Mathf.Abs(_rb.linearVelocity.x));
            _animator.SetBool(_groundedParam, _isGrounded);
            _animator.SetFloat(_verticalVelocityParam, _rb.linearVelocity.y);
            _animator.SetBool(_wallSlidingParam, _isWallSliding);
            _animator.SetBool(_dashingParam, _isDashing);
        }

        // Trigger detection for keys, hazards, etc.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check for colour zones
            var colourZone = other.GetComponent<ColourZone>();
            if (colourZone != null && colourZone.DestroysClones)
            {
                Destroy(gameObject, 0.05f);
                return;
            }

            // Check for hazards (spikes, etc.)
            var hazard = other.GetComponent<Hazard>();
            if (hazard != null)
            {
                Debug.Log("CloneMovement: Hit hazard! Destroying clone.");
                Destroy(gameObject, 0.05f);
                return;
            }
        }

        // Also check collision (in case hazard uses collision not trigger)
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var hazard = collision.collider.GetComponent<Hazard>();
            if (hazard != null)
            {
                Debug.Log("CloneMovement: Hit hazard! Destroying clone.");
                Destroy(gameObject, 0.05f);
            }
        }
    }
}