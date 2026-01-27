using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Smooth camera that looks ahead of the player.
    /// Shows more of what's in front of the player rather than behind.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private UltimatePlayerController _controller;

        [Header("Base Offset")]
        [Tooltip("Base offset from the player (usually just Y to keep camera above)")]
        [SerializeField] private Vector3 _baseOffset = new Vector3(0, 2f, -10f);

        [Header("Look-Ahead")]
        [Tooltip("How far ahead of the player to look (in the direction they're facing)")]
        [SerializeField] private float _lookAheadDistance = 3f;
        
        [Tooltip("How fast the look-ahead adjusts when changing direction")]
        [SerializeField] private float _lookAheadSmoothing = 5f;

        [Header("Smoothing")]
        [Tooltip("How smoothly the camera follows horizontally")]
        [SerializeField] private float _horizontalSmoothing = 8f;
        
        [Tooltip("How smoothly the camera follows vertically")]
        [SerializeField] private float _verticalSmoothing = 6f;

        [Header("Dead Zone (Optional)")]
        [Tooltip("Player can move this far before camera starts following")]
        [SerializeField] private float _deadZoneX = 0.5f;
        [SerializeField] private float _deadZoneY = 0.5f;

        // Internal state
        private Vector3 _currentLookAhead;
        private Vector3 _targetLookAhead;
        private float _lastFacingDirection = 1f;

        private void Start()
        {
            // Try to find target if not assigned
            if (_target == null)
            {
                var player = FindObjectOfType<UltimatePlayerController>();
                if (player != null)
                {
                    _target = player.transform;
                    _controller = player;
                }
            }

            if (_controller == null && _target != null)
            {
                _controller = _target.GetComponent<UltimatePlayerController>();
            }

            // Start at target position
            if (_target != null)
            {
                transform.position = _target.position + _baseOffset;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Get facing direction from controller, or use velocity
            float facingDirection = GetFacingDirection();
            
            // Calculate look-ahead target
            _targetLookAhead = new Vector3(facingDirection * _lookAheadDistance, 0, 0);
            
            // Smoothly interpolate look-ahead
            _currentLookAhead = Vector3.Lerp(
                _currentLookAhead, 
                _targetLookAhead, 
                _lookAheadSmoothing * Time.deltaTime
            );

            // Calculate target position
            Vector3 targetPosition = _target.position + _baseOffset + _currentLookAhead;

            // Apply dead zone
            Vector3 currentPos = transform.position;
            float deltaX = targetPosition.x - currentPos.x;
            float deltaY = targetPosition.y - currentPos.y;

            // Only move if outside dead zone
            if (Mathf.Abs(deltaX) < _deadZoneX) deltaX = 0;
            if (Mathf.Abs(deltaY) < _deadZoneY) deltaY = 0;

            // Smooth follow with separate horizontal/vertical speeds
            float newX = currentPos.x + deltaX * _horizontalSmoothing * Time.deltaTime;
            float newY = currentPos.y + deltaY * _verticalSmoothing * Time.deltaTime;

            // Apply position (keep Z from base offset)
            transform.position = new Vector3(newX, newY, _baseOffset.z);
        }

        private float GetFacingDirection()
        {
            if (_controller != null)
            {
                // Use controller's facing direction
                int facing = _controller.FacingDirection;
                if (facing != 0)
                {
                    _lastFacingDirection = facing;
                }
            }
            else if (_target != null)
            {
                // Fallback: use velocity direction
                Rigidbody2D rb = _target.GetComponent<Rigidbody2D>();
                if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                {
                    _lastFacingDirection = Mathf.Sign(rb.linearVelocity.x);
                }
            }

            return _lastFacingDirection;
        }

        /// <summary>
        /// Instantly snap camera to target (useful for respawns, teleports)
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            
            _currentLookAhead = _targetLookAhead;
            transform.position = _target.position + _baseOffset + _currentLookAhead;
        }

        /// <summary>
        /// Change the look-ahead distance at runtime
        /// </summary>
        public void SetLookAheadDistance(float distance)
        {
            _lookAheadDistance = distance;
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;

            // Draw dead zone
            Gizmos.color = Color.yellow;
            Vector3 center = _target.position + _baseOffset;
            Gizmos.DrawWireCube(center, new Vector3(_deadZoneX * 2, _deadZoneY * 2, 0));

            // Draw look-ahead range
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center + Vector3.left * _lookAheadDistance, center + Vector3.right * _lookAheadDistance);
            Gizmos.DrawWireSphere(center + Vector3.right * _lookAheadDistance, 0.2f);
            Gizmos.DrawWireSphere(center + Vector3.left * _lookAheadDistance, 0.2f);
        }
        #endif
    }
}