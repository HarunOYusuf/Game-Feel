using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Handles player animations and visual effects.
    /// Connects the UltimatePlayerController to Unity's Animator.
    /// </summary>
    [RequireComponent(typeof(UltimatePlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _spriteTransform;

        [Header("Animation Parameter Names")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _isGroundedParam = "IsGrounded";
        [SerializeField] private string _verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string _isDashingParam = "IsDashing";
        [SerializeField] private string _isWallSlidingParam = "IsWallSliding";

        [Header("Squash & Stretch (Optional)")]
        [SerializeField] private bool _useSquashStretch = false;
        [SerializeField] private float _landSquashAmount = 0.7f;
        [SerializeField] private float _jumpStretchAmount = 1.2f;
        [SerializeField] private float _squashStretchSpeed = 10f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _runParticles;
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _landParticles;
        [SerializeField] private ParticleSystem _wallSlideParticles;

        [Header("Dash Afterimage")]
        [SerializeField] private bool _useDashAfterimage = true;
        [SerializeField] private GameObject _afterimagePrefab;
        [SerializeField] private float _afterimageSpawnRate = 0.02f;
        [SerializeField] private Color _afterimageColor = new Color(0.5f, 0.8f, 1f, 0.5f);
        [SerializeField] private float _afterimageFadeDuration = 0.3f;

        // Components
        private UltimatePlayerController _controller;
        
        // State
        private Vector3 _targetScale = Vector3.one;
        private float _lastAfterimageTime;

        private void Awake()
        {
            _controller = GetComponent<UltimatePlayerController>();
            
            if (_animator == null) 
                _animator = GetComponentInChildren<Animator>();
            
            if (_spriteRenderer == null) 
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (_spriteTransform == null && _spriteRenderer != null) 
                _spriteTransform = _spriteRenderer.transform;

            if (_animator == null)
                Debug.LogError("PlayerAnimator: No Animator found! Please assign one.", this);
        }

        private void OnEnable()
        {
            _controller.GroundedChanged += OnGroundedChanged;
            _controller.Jumped += OnJumped;
            _controller.DashChanged += OnDashChanged;
            _controller.WallSlideChanged += OnWallSlideChanged;
        }

        private void OnDisable()
        {
            _controller.GroundedChanged -= OnGroundedChanged;
            _controller.Jumped -= OnJumped;
            _controller.DashChanged -= OnDashChanged;
            _controller.WallSlideChanged -= OnWallSlideChanged;
        }

        private void Update()
        {
            HandleSpriteFlip();
            HandleAnimatorParameters();
            HandleSquashStretch();
            HandleRunParticles();
            HandleWallSlideParticlePosition();
            HandleDashAfterimage();
        }

        /// <summary>
        /// Flip the sprite based on movement direction
        /// When wall sliding, face away from the wall
        /// </summary>
        private void HandleSpriteFlip()
        {
            if (_spriteRenderer == null) return;

            // When wall sliding, face away from the wall
            if (_controller.IsWallSliding)
            {
                _spriteRenderer.flipX = _controller.WallDirection > 0;
                return;
            }

            // Use the controller's tracked facing direction
            _spriteRenderer.flipX = _controller.FacingDirection < 0;
        }

        /// <summary>
        /// Update all animator parameters every frame
        /// </summary>
        private void HandleAnimatorParameters()
        {
            if (_animator == null) return;

            float speed = Mathf.Abs(_controller.Velocity.x);
            _animator.SetFloat(_speedParam, speed);
            _animator.SetBool(_isGroundedParam, _controller.IsGrounded);
            _animator.SetFloat(_verticalVelocityParam, _controller.Velocity.y);
        }

        /// <summary>
        /// Smoothly apply and recover from squash/stretch effects
        /// </summary>
        private void HandleSquashStretch()
        {
            if (!_useSquashStretch || _spriteTransform == null) return;

            _spriteTransform.localScale = Vector3.Lerp(
                _spriteTransform.localScale, 
                _targetScale, 
                _squashStretchSpeed * Time.deltaTime
            );

            _targetScale = Vector3.Lerp(
                _targetScale, 
                Vector3.one, 
                _squashStretchSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Control run particle emission based on movement
        /// </summary>
        private void HandleRunParticles()
        {
            if (_runParticles == null) return;

            bool isMoving = Mathf.Abs(_controller.Velocity.x) > 0.5f;
            bool shouldEmit = _controller.IsGrounded && isMoving;
            
            if (shouldEmit && !_runParticles.isPlaying)
            {
                _runParticles.Play();
            }
            else if (!shouldEmit && _runParticles.isPlaying)
            {
                _runParticles.Stop();
            }
        }

        /// <summary>
        /// Position wall slide particles based on which wall we're on
        /// </summary>
        private void HandleWallSlideParticlePosition()
        {
            if (_wallSlideParticles == null || !_controller.IsWallSliding) return;

            float xPos;
            
            if (_controller.WallDirection == 1)
            {
                // Right wall (player facing left)
                xPos = 0.85f;
            }
            else
            {
                // Left wall (player facing right)
                xPos = -0.05f;
            }
            
            _wallSlideParticles.transform.localPosition = new Vector3(xPos, _wallSlideParticles.transform.localPosition.y, _wallSlideParticles.transform.localPosition.z);
        }

        /// <summary>
        /// Spawn afterimages during dash for speed blur effect
        /// </summary>
        private void HandleDashAfterimage()
        {
            if (!_useDashAfterimage || _afterimagePrefab == null) return;
            if (_animator == null || !_animator.GetBool(_isDashingParam)) return;

            if (Time.time - _lastAfterimageTime >= _afterimageSpawnRate)
            {
                SpawnAfterimage();
                _lastAfterimageTime = Time.time;
            }
        }

        private void SpawnAfterimage()
        {
            if (_spriteRenderer == null) return;

            // Spawn at sprite's exact position
            GameObject afterimage = Instantiate(_afterimagePrefab, _spriteRenderer.transform.position, Quaternion.identity);
            SpriteRenderer afterimageSprite = afterimage.GetComponent<SpriteRenderer>();
            
            if (afterimageSprite != null)
            {
                afterimageSprite.sprite = _spriteRenderer.sprite;
                afterimageSprite.flipX = _spriteRenderer.flipX;
                afterimageSprite.color = _afterimageColor;
                
                // Set sorting to be behind the player
                afterimageSprite.sortingLayerID = _spriteRenderer.sortingLayerID;
                afterimageSprite.sortingOrder = _spriteRenderer.sortingOrder - 1;
                
                // Match the player's scale
                afterimage.transform.localScale = _spriteRenderer.transform.lossyScale;
                
                // Start fading (stays in place, doesn't move)
                DashAfterimage fadeScript = afterimage.AddComponent<DashAfterimage>();
                fadeScript.Initialize(_afterimageFadeDuration);
            }
        }

        #region Event Handlers

        private void OnGroundedChanged(bool isGrounded, float impactVelocity)
        {
            if (isGrounded)
            {
                if (_useSquashStretch)
                {
                    float squashAmount = Mathf.Lerp(1f, _landSquashAmount, impactVelocity / 40f);
                    _targetScale = new Vector3(1f / squashAmount, squashAmount, 1f);
                }

                if (_landParticles != null && impactVelocity > 5f)
                {
                    _landParticles.Play();
                }
            }
        }

        private void OnJumped()
        {
            if (_useSquashStretch)
            {
                _targetScale = new Vector3(1f / _jumpStretchAmount, _jumpStretchAmount, 1f);
            }

            if (_jumpParticles != null)
            {
                _jumpParticles.Play();
            }
        }

        private void OnDashChanged(bool isDashing)
        {
            if (_animator != null)
            {
                _animator.SetBool(_isDashingParam, isDashing);
            }

            if (isDashing)
            {
                if (_useSquashStretch)
                {
                    _targetScale = new Vector3(1.3f, 0.8f, 1f);
                }
            }
        }

        private void OnWallSlideChanged(bool isWallSliding)
        {
            if (_animator != null)
            {
                _animator.SetBool(_isWallSlidingParam, isWallSliding);
            }

            if (_wallSlideParticles != null)
            {
                if (isWallSliding)
                {
                    _wallSlideParticles.Play();
                }
                else
                {
                    _wallSlideParticles.Stop();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple component to fade out and destroy dash afterimages
    /// </summary>
    public class DashAfterimage : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private float _fadeDuration;
        private float _startTime;
        private Color _startColor;

        public void Initialize(float fadeDuration)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _fadeDuration = fadeDuration;
            _startTime = Time.time;
            _startColor = _spriteRenderer.color;
        }

        private void Update()
        {
            if (_spriteRenderer == null) return;

            float elapsed = Time.time - _startTime;
            float t = elapsed / _fadeDuration;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Color newColor = _startColor;
            newColor.a = Mathf.Lerp(_startColor.a, 0, t);
            _spriteRenderer.color = newColor;
        }
    }
}