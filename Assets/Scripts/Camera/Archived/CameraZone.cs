using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Defines camera bounds for a section of the level.
    /// When the player enters this zone, the camera bounds change.
    /// 
    /// Setup:
    /// 1. Create empty GameObject for each section
    /// 2. Add BoxCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// 4. Resize the collider to cover the section
    /// 5. The bounds will automatically match the collider size
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraZone : MonoBehaviour
    {
        [Header("Bounds Override (Optional)")]
        [Tooltip("If true, use custom bounds instead of collider size")]
        [SerializeField] private bool _useCustomBounds = false;
        [SerializeField] private Vector2 _customMin;
        [SerializeField] private Vector2 _customMax;

        [Header("Transition")]
        [Tooltip("Snap instantly to new bounds (no smooth transition)")]
        [SerializeField] private bool _instantTransition = false;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        private BoxCollider2D _collider;

        private void Start()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only respond to player (main collider, not DashSprite)
            if (!other.TryGetComponent<UltimatePlayerController>(out _))
                return;

            ApplyBounds();

            if (_showDebugMessages)
                Debug.Log($"CameraZone: Player entered {gameObject.name}");
        }

        private void ApplyBounds()
        {
            if (CameraBounds.Instance == null)
            {
                Debug.LogWarning("CameraZone: No CameraBounds found on camera!");
                return;
            }

            Vector2 min, max;

            if (_useCustomBounds)
            {
                min = _customMin;
                max = _customMax;
            }
            else
            {
                // Use collider bounds
                Bounds bounds = _collider.bounds;
                min = bounds.min;
                max = bounds.max;
            }

            if (_instantTransition)
            {
                CameraBounds.Instance.SetBoundsImmediate(min, max);
            }
            else
            {
                CameraBounds.Instance.SetBounds(min, max);
            }
        }

        /// <summary>
        /// Manually get the bounds for this zone
        /// </summary>
        public void GetBounds(out Vector2 min, out Vector2 max)
        {
            if (_useCustomBounds)
            {
                min = _customMin;
                max = _customMax;
            }
            else
            {
                if (_collider == null) _collider = GetComponent<BoxCollider2D>();
                Bounds bounds = _collider.bounds;
                min = bounds.min;
                max = bounds.max;
            }
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            // Draw zone bounds
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(col.offset, col.size);
        }

        private void OnDrawGizmosSelected()
        {
            // Show zone name
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"Camera Zone: {gameObject.name}");
            #endif
        }
    }
}