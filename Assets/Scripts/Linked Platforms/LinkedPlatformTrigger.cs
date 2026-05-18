using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Trigger that activates a LinkedPlatformGroup when player or clone stands on it.
    /// Place this on the platform that acts as the "button" or "switch".
    /// 
    /// Setup:
    /// 1. Add this script to the trigger platform (e.g., Platform 1)
    /// 2. Add a BoxCollider2D and set it as a trigger
    /// 3. Drag the LinkedPlatformGroup into the Group field
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LinkedPlatformTrigger : MonoBehaviour
    {
        [Header("Link to Group")]
        [Tooltip("The LinkedPlatformGroup that this trigger controls")]
        [SerializeField] private LinkedPlatformGroup _group;

        [Header("What Can Activate")]
        [Tooltip("Can the player activate this trigger?")]
        [SerializeField] private bool _playerCanActivate = true;
        
        [Tooltip("Can time clones activate this trigger?")]
        [SerializeField] private bool _cloneCanActivate = true;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Change colour when activated")]
        [SerializeField] private bool _changeColourOnActivate = true;
        
        [SerializeField] private Color _inactiveColour = Color.grey;
        [SerializeField] private Color _activeColour = Color.green;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        // Components
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;

        // State
        private int _activatorCount = 0;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Ensure collider is a trigger
            _collider.isTrigger = true;
        }

        private void Start()
        {
            // Set initial colour
            if (_changeColourOnActivate && _spriteRenderer != null)
            {
                _spriteRenderer.color = _inactiveColour;
            }

            // Validate group reference
            if (_group == null)
            {
                Debug.LogError($"LinkedPlatformTrigger on {gameObject.name}: No LinkedPlatformGroup assigned!");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;

            _activatorCount++;

            if (_showDebugMessages)
            {
                Debug.Log($"LinkedPlatformTrigger: {other.name} entered. Activator count: {_activatorCount}");
            }

            // Notify the group
            if (_group != null)
            {
                _group.OnTriggerActivate();
            }

            // Update visual
            UpdateVisual();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;

            _activatorCount = Mathf.Max(0, _activatorCount - 1);

            if (_showDebugMessages)
            {
                Debug.Log($"LinkedPlatformTrigger: {other.name} exited. Activator count: {_activatorCount}");
            }

            // Notify the group
            if (_group != null)
            {
                _group.OnTriggerDeactivate();
            }

            // Update visual
            UpdateVisual();
        }

        /// <summary>
        /// Check if the collider belongs to a valid activator (player or clone)
        /// </summary>
        private bool IsValidActivator(Collider2D other)
        {
            // Check for player
            if (_playerCanActivate)
            {
                var player = other.GetComponent<UltimatePlayerController>();
                if (player != null)
                {
                    // Make sure it's the main player object, not a child collider
                    if (other.gameObject == player.gameObject)
                    {
                        return true;
                    }
                }
            }

            // Check for clone (new input-based system)
            if (_cloneCanActivate)
            {
                var clone = other.GetComponent<CloneMovement>();
                if (clone != null)
                {
                    return true;
                }

                // Also check for old position-based clone
                var oldClone = other.GetComponent<TimeClone>();
                if (oldClone != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateVisual()
        {
            if (!_changeColourOnActivate || _spriteRenderer == null) return;

            _spriteRenderer.color = _activatorCount > 0 ? _activeColour : _inactiveColour;
        }

        // Editor visualisation
        private void OnDrawGizmos()
        {
            // Draw trigger zone
            var col = GetComponent<Collider2D>();
            if (col != null && col is BoxCollider2D box)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(box.offset, box.size);
            }

            // Draw line to linked group
            if (_group != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
                Gizmos.DrawLine(transform.position, _group.transform.position);
                
                // Draw small sphere at group
                Gizmos.DrawWireSphere(_group.transform.position, 0.3f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            // Show label
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            
            string label = "TRIGGER";
            if (_group != null)
            {
                label += $"\n→ {_group.gameObject.name}";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, label, style);
            #endif
        }
    }
}