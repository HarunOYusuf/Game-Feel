using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A block that slides between two positions based on pressure plate state.
    /// Use for doors, moving walls, platforms, etc.
    /// 
    /// Setup:
    /// 1. Create a sprite for the block
    /// 2. Add BoxCollider2D (solid, not trigger)
    /// 3. Add this script
    /// 4. Position the block at its CLOSED position
    /// 5. Set the Open Offset (how far it moves when plate is pressed)
    /// 6. Add this to the Pressure Plate's Connected Receivers list
    /// </summary>
    public class SlidingBlock : PressurePlateReceiver
    {
        [Header("Movement")]
        [Tooltip("How far the block moves when activated (relative to start position)")]
        [SerializeField] private Vector3 _openOffset = new Vector3(0f, 3f, 0f);
        
        [Tooltip("How fast the block moves")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Behaviour")]
        [Tooltip("If true, block starts in open position and closes when plate is pressed")]
        [SerializeField] private bool _invertBehaviour = false;
        
        [Tooltip("Ease the movement for smoother feel")]
        [SerializeField] private bool _useEasing = true;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource _moveSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // State
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Vector3 _targetPosition;
        private bool _isOpen;
        private bool _isMoving;

        /// <summary>
        /// Is the block currently in the open position?
        /// </summary>
        public bool IsOpen => _isOpen;

        private void Start()
        {
            // Store positions
            _closedPosition = transform.position;
            _openPosition = _closedPosition + _openOffset;

            // Set initial state based on invert setting
            if (_invertBehaviour)
            {
                transform.position = _openPosition;
                _targetPosition = _openPosition;
                _isOpen = true;
            }
            else
            {
                _targetPosition = _closedPosition;
                _isOpen = false;
            }
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, _targetPosition) > 0.01f)
            {
                if (!_isMoving)
                {
                    _isMoving = true;
                    if (_moveSound != null && !_moveSound.isPlaying)
                    {
                        _moveSound.Play();
                    }
                }

                if (_useEasing)
                {
                    // Smooth easing
                    transform.position = Vector3.Lerp(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
                }
                else
                {
                    // Linear movement
                    transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
                }
            }
            else
            {
                if (_isMoving)
                {
                    _isMoving = false;
                    transform.position = _targetPosition;
                    
                    if (_moveSound != null)
                    {
                        _moveSound.Stop();
                    }
                }
            }
        }

        public override void OnPressurePlateChanged(bool isPressed)
        {
            // Determine target based on plate state and invert setting
            bool shouldOpen = _invertBehaviour ? !isPressed : isPressed;

            if (shouldOpen != _isOpen)
            {
                _isOpen = shouldOpen;
                _targetPosition = _isOpen ? _openPosition : _closedPosition;

                if (_showDebugMessages)
                    Debug.Log($"SlidingBlock: Moving to {(_isOpen ? "OPEN" : "CLOSED")}");
            }
        }

        /// <summary>
        /// Manually set the block state
        /// </summary>
        public void SetOpen(bool open)
        {
            _isOpen = open;
            _targetPosition = _isOpen ? _openPosition : _closedPosition;
        }

        /// <summary>
        /// Instantly move to target position (no animation)
        /// </summary>
        public void SnapToTarget()
        {
            transform.position = _targetPosition;
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Vector3 closedPos = Application.isPlaying ? _closedPosition : transform.position;
            Vector3 openPos = closedPos + _openOffset;

            // Draw closed position
            Gizmos.color = Color.red;
            DrawBlockGizmo(closedPos);

            // Draw open position
            Gizmos.color = Color.green;
            DrawBlockGizmo(openPos);

            // Draw movement path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(closedPos, openPos);

            // Draw arrow showing direction
            Vector3 dir = (openPos - closedPos).normalized;
            Vector3 midPoint = (closedPos + openPos) / 2f;
            DrawArrow(midPoint, dir, 0.3f);
        }

        private void DrawBlockGizmo(Vector3 position)
        {
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Vector3 size = box.size;
                Gizmos.DrawWireCube(position + (Vector3)box.offset, size);
            }
            else
            {
                Gizmos.DrawWireCube(position, Vector3.one);
            }
        }

        private void DrawArrow(Vector3 position, Vector3 direction, float size)
        {
            Vector3 right = Vector3.Cross(direction, Vector3.forward).normalized;
            Vector3 tip = position + direction * size;
            Gizmos.DrawLine(position, tip);
            Gizmos.DrawLine(tip, tip - direction * size * 0.3f + right * size * 0.2f);
            Gizmos.DrawLine(tip, tip - direction * size * 0.3f - right * size * 0.2f);
        }

        private void OnDrawGizmosSelected()
        {
            // Show labels
            #if UNITY_EDITOR
            Vector3 closedPos = Application.isPlaying ? _closedPosition : transform.position;
            Vector3 openPos = closedPos + _openOffset;
            
            UnityEditor.Handles.Label(closedPos + Vector3.up * 0.5f, "CLOSED");
            UnityEditor.Handles.Label(openPos + Vector3.up * 0.5f, "OPEN");
            #endif
        }
    }
}