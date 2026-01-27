using System;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Ultimate 2D Player Controller
    /// Features: Variable jump height, apex modifier, jump buffering, coyote time,
    /// clamped fall speed, edge detection, wall slide, wall jump, and more.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class UltimatePlayerController : MonoBehaviour, IPlayerController
    {
        [Header("Configuration")]
        [SerializeField] private PlayerStats _stats;

        // Components
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        
        // Input
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        
        // Cached
        private bool _cachedQueryStartInColliders;
        private float _time;

        #region Interface

        public Vector2 Input => _frameInput.Move;
        public Vector2 Velocity => _frameVelocity;
        public bool IsGrounded => _grounded;
        public bool IsJumping => !_grounded && _frameVelocity.y > 0;
        public bool IsFalling => !_grounded && _frameVelocity.y < 0;
        public bool IsAtApex => !_grounded && Mathf.Abs(_frameVelocity.y) < _stats.ApexThreshold;
        public bool IsWallSliding => _isWallSliding;
        public int WallDirection => _wallDirection;
        public int FacingDirection => _facingDirection;
        
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> DashChanged;
        public event Action<bool> WallSlideChanged;

        #endregion

        #region Collision State

        private bool _grounded;
        private float _frameLeftGrounded = float.MinValue;
        private bool _isOnEdge;
        private bool _isOnLeftEdge;
        private bool _isOnRightEdge;

        #endregion

        #region Jump State

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;
        private float _apexPoint;

        #endregion

        #region Dash State

        private bool _dashToConsume;
        private bool _canDash;
        private bool _isDashing;
        private float _dashStartTime;
        private Vector2 _dashDirection;

        #endregion

        #region Wall Slide State

        private bool _isWallSliding;
        private bool _isTouchingWall;
        private int _wallDirection;
        private float _timeLeftWall = float.MinValue;
        private bool _wallJumpCoyoteUsable;

        #endregion

        #region Facing Direction

        private int _facingDirection = 1;

        #endregion

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = UnityEngine.Input.GetButtonDown("Jump") || 
                           UnityEngine.Input.GetKeyDown(KeyCode.Space) ||
                           UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1),
                           
                JumpHeld = UnityEngine.Input.GetButton("Jump") || 
                           UnityEngine.Input.GetKey(KeyCode.Space) ||
                           UnityEngine.Input.GetKey(KeyCode.JoystickButton1),
                           
                DashDown = UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || 
                           UnityEngine.Input.GetKeyDown(KeyCode.K) ||
                           UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2),
                           
                Move = new Vector2(
                    UnityEngine.Input.GetAxisRaw("Horizontal"), 
                    UnityEngine.Input.GetAxisRaw("Vertical")
                )
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZone ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZone ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            if (_frameInput.DashDown)
            {
                _dashToConsume = true;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            CheckWallCollisions();
            CheckEdgeDetection();

            HandleDash();
            HandleWallSlide();
            
            if (!_isDashing)
            {
                HandleJump();
                HandleHorizontal();
                HandleGravity();
            }

            ApplyMovement();
        }

        #region Collisions

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(
                _col.bounds.center, 
                _col.size, 
                _col.direction, 
                0, 
                Vector2.down, 
                _stats.GrounderDistance, 
                _stats.GroundLayer
            );

            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center, 
                _col.size, 
                _col.direction, 
                0, 
                Vector2.up, 
                _stats.GrounderDistance, 
                _stats.GroundLayer
            );

            if (ceilingHit) 
            {
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                _canDash = true;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Wall Detection

        private void CheckWallCollisions()
        {
            if (!_stats.AllowWallSlide || _grounded)
            {
                if (_isWallSliding)
                {
                    _isWallSliding = false;
                    WallSlideChanged?.Invoke(false);
                }
                _isTouchingWall = false;
                _wallDirection = 0;
                return;
            }

            Physics2D.queriesStartInColliders = false;

            float castDistance = _stats.WallCheckDistance;
            Vector2 castSize = new Vector2(_col.size.x * 0.9f, _col.size.y * 0.8f);

            bool wallOnRight = Physics2D.BoxCast(
                _col.bounds.center,
                castSize,
                0,
                Vector2.right,
                castDistance,
                _stats.WallLayer
            );

            bool wallOnLeft = Physics2D.BoxCast(
                _col.bounds.center,
                castSize,
                0,
                Vector2.left,
                castDistance,
                _stats.WallLayer
            );

            bool wasTouchingWall = _isTouchingWall;
            _isTouchingWall = wallOnLeft || wallOnRight;
            
            if (wallOnRight)
                _wallDirection = 1;
            else if (wallOnLeft)
                _wallDirection = -1;
            else
                _wallDirection = 0;

            if (wasTouchingWall && !_isTouchingWall)
            {
                _timeLeftWall = _time;
                _wallJumpCoyoteUsable = true;
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;

            #if UNITY_EDITOR
            Vector3 rightOrigin = _col.bounds.center + Vector3.right * (_col.bounds.extents.x);
            Vector3 leftOrigin = _col.bounds.center + Vector3.left * (_col.bounds.extents.x);
            Debug.DrawRay(rightOrigin, Vector2.right * castDistance, wallOnRight ? Color.green : Color.red);
            Debug.DrawRay(leftOrigin, Vector2.left * castDistance, wallOnLeft ? Color.green : Color.red);
            #endif
        }

        #endregion

        #region Wall Slide

        private void HandleWallSlide()
        {
            if (!_stats.AllowWallSlide) return;

            bool wasWallSliding = _isWallSliding;

            _isWallSliding = _isTouchingWall && 
                            !_grounded && 
                            _frameVelocity.y <= 0;

            if (_isWallSliding != wasWallSliding)
            {
                WallSlideChanged?.Invoke(_isWallSliding);
                
                if (_isWallSliding)
                {
                    _canDash = true;
                }
            }
        }

        #endregion

        #region Edge Detection

        private void CheckEdgeDetection()
        {
            if (!_grounded || !_stats.UseEdgeDetection)
            {
                _isOnEdge = false;
                _isOnLeftEdge = false;
                _isOnRightEdge = false;
                return;
            }

            Vector2 colliderCenter = _col.bounds.center;
            float halfWidth = _col.bounds.extents.x;
            float rayLength = _stats.GrounderDistance + 0.1f;

            Vector2 leftRayOrigin = new Vector2(colliderCenter.x - halfWidth + _stats.EdgeDetectionOffset, colliderCenter.y);
            Vector2 rightRayOrigin = new Vector2(colliderCenter.x + halfWidth - _stats.EdgeDetectionOffset, colliderCenter.y);

            bool leftGrounded = Physics2D.Raycast(leftRayOrigin, Vector2.down, rayLength, _stats.GroundLayer);
            bool rightGrounded = Physics2D.Raycast(rightRayOrigin, Vector2.down, rayLength, _stats.GroundLayer);

            _isOnLeftEdge = !leftGrounded && rightGrounded;
            _isOnRightEdge = leftGrounded && !rightGrounded;
            _isOnEdge = _isOnLeftEdge || _isOnRightEdge;

            if (_stats.EdgeCorrection && _isOnEdge && _frameInput.Move.x == 0)
            {
                float correctionDirection = _isOnLeftEdge ? 1f : -1f;
                _frameVelocity.x += correctionDirection * _stats.EdgeCorrectionStrength * Time.fixedDeltaTime;
            }

            #if UNITY_EDITOR
            Debug.DrawRay(leftRayOrigin, Vector2.down * rayLength, leftGrounded ? Color.green : Color.red);
            Debug.DrawRay(rightRayOrigin, Vector2.down * rayLength, rightGrounded ? Color.green : Color.red);
            #endif
        }

        #endregion

        #region Jumping

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;
        private bool CanUseWallCoyote => _wallJumpCoyoteUsable && !_isTouchingWall && _time < _timeLeftWall + _stats.WallJumpCoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
            {
                _endedJumpEarly = true;
            }

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_stats.AllowWallSlide && (_isWallSliding || _isTouchingWall || CanUseWallCoyote))
            {
                ExecuteWallJump();
            }
            else if (_grounded || CanUseCoyote)
            {
                ExecuteJump();
            }

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        private void ExecuteWallJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _wallJumpCoyoteUsable = false;
            _isWallSliding = false;

            int jumpDirection = -_wallDirection;
            
            _frameVelocity = new Vector2(
                jumpDirection * _stats.WallJumpHorizontalPower,
                _stats.WallJumpVerticalPower
            );

            WallSlideChanged?.Invoke(false);
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal Movement

        private void HandleHorizontal()
        {
            if (_frameInput.Move.x != 0)
            {
                _facingDirection = _frameInput.Move.x > 0 ? 1 : -1;
            }

            if (!_grounded)
            {
                _apexPoint = Mathf.InverseLerp(_stats.ApexThreshold, 0, Mathf.Abs(_frameVelocity.y));
            }
            else
            {
                _apexPoint = 0;
            }

            if (_frameInput.Move.x == 0)
            {
                float deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                float targetSpeed = _frameInput.Move.x * _stats.MaxSpeed;
                
                if (_stats.UseApexModifier && _apexPoint > 0)
                {
                    targetSpeed *= 1 + (_stats.ApexSpeedBonus * _apexPoint);
                }

                float acceleration = _grounded ? _stats.Acceleration : _stats.AirAcceleration;
                
                if (_stats.UseApexModifier && _apexPoint > 0)
                {
                    acceleration *= 1 + (_stats.ApexAccelerationBonus * _apexPoint);
                }

                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else if (_isWallSliding)
            {
                _frameVelocity.y = Mathf.MoveTowards(
                    _frameVelocity.y, 
                    -_stats.WallSlideSpeed, 
                    _stats.WallSlideAcceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                float gravity = _stats.FallAcceleration;

                if (_endedJumpEarly && _frameVelocity.y > 0)
                {
                    gravity *= _stats.JumpCutMultiplier;
                }
                else if (_stats.UseApexModifier && _apexPoint > 0)
                {
                    gravity *= Mathf.Lerp(1f, _stats.ApexGravityMultiplier, _apexPoint);
                }

                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, gravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Dash

        private void HandleDash()
        {
            if (_dashToConsume && _canDash && _stats.AllowDash)
            {
                if (_isWallSliding)
                {
                    _isWallSliding = false;
                    WallSlideChanged?.Invoke(false);
                }

                _isDashing = true;
                _canDash = false;
                _dashStartTime = _time;
                
                _dashDirection = _frameInput.Move.normalized;
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = _facingDirection > 0 ? Vector2.right : Vector2.left;
                }
                
                DashChanged?.Invoke(true);
            }
            _dashToConsume = false;

            if (_isDashing)
            {
                if (_time >= _dashStartTime + _stats.DashDuration)
                {
                    _isDashing = false;
                    _frameVelocity = _dashDirection * _stats.DashEndSpeed;
                    DashChanged?.Invoke(false);
                }
                else
                {
                    _frameVelocity = _dashDirection * _stats.DashSpeed;
                }
            }
        }

        #endregion

        private void ApplyMovement()
        {
            _rb.linearVelocity = _frameVelocity;
        }

        #region External Methods

        public void ApplyForce(Vector2 force, bool resetVelocity = false)
        {
            if (resetVelocity) _frameVelocity = Vector2.zero;
            _frameVelocity += force;
        }

        public void Teleport(Vector2 position)
        {
            _rb.position = position;
            _frameVelocity = Vector2.zero;
        }

        public void ResetDash()
        {
            _canDash = true;
        }

        #endregion

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
            {
                Debug.LogWarning("Please assign a PlayerStats asset to the Player Controller", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_col == null) return;

            Gizmos.color = _grounded ? Color.green : Color.red;
            Vector3 groundCheckPos = _col.bounds.center + Vector3.down * _stats.GrounderDistance;
            Gizmos.DrawWireCube(groundCheckPos, new Vector3(_col.bounds.size.x, 0.1f, 0));
        }
        #endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public bool DashDown;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        Vector2 Input { get; }
        Vector2 Velocity { get; }
        bool IsGrounded { get; }
        bool IsJumping { get; }
        bool IsFalling { get; }
        bool IsAtApex { get; }
        bool IsWallSliding { get; }
        int WallDirection { get; }
        int FacingDirection { get; }
        
        event Action<bool, float> GroundedChanged;
        event Action Jumped;
        event Action<bool> DashChanged;
        event Action<bool> WallSlideChanged;
    }
}