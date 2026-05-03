using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Checkpoint that saves player's respawn position.
    /// Place these after each section of your level.
    /// 
    /// Setup:
    /// 1. Create empty GameObject
    /// 2. Add BoxCollider2D or CircleCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// 4. Position at the checkpoint location
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Can this checkpoint only be activated once?")]
        [SerializeField] private bool _oneTimeOnly = true;
        
        [Tooltip("Optional: Custom respawn position (if empty, uses this object's position)")]
        [SerializeField] private Transform _respawnPoint;

        [Header("Visuals (Optional)")]
        [Tooltip("Sprite renderer to change when activated")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        [SerializeField] private Color _inactiveColour = Color.grey;
        [SerializeField] private Color _activeColour = Color.green;

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _activationParticles;
        [SerializeField] private AudioSource _activationSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // State
        private bool _isActivated;

        /// <summary>
        /// Has this checkpoint been activated?
        /// </summary>
        public bool IsActivated => _isActivated;

        private void Start()
        {
            // Ensure collider is trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }

            // Set initial colour
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _inactiveColour;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only activate for player
            if (other.GetComponent<UltimatePlayerController>() == null)
                return;

            // Check if already activated (and one-time only)
            if (_isActivated && _oneTimeOnly)
                return;

            ActivateCheckpoint();
        }

        private void ActivateCheckpoint()
        {
            _isActivated = true;

            // Get respawn position
            Vector2 respawnPos = _respawnPoint != null 
                ? (Vector2)_respawnPoint.position 
                : (Vector2)transform.position;

            // Tell GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(respawnPos);
            }
            else
            {
                Debug.LogWarning("Checkpoint: No GameManager found!");
            }

            // Visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _activeColour;
            }

            // Effects
            if (_activationParticles != null)
            {
                _activationParticles.Play();
            }

            if (_activationSound != null)
            {
                _activationSound.Play();
            }

            if (_showDebugMessages)
                Debug.Log($"Checkpoint activated: {gameObject.name} at {respawnPos}");
        }

        /// <summary>
        /// Reset this checkpoint (for level restart)
        /// </summary>
        public void ResetCheckpoint()
        {
            _isActivated = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _inactiveColour;
            }
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Vector3 pos = _respawnPoint != null ? _respawnPoint.position : transform.position;

            // Draw checkpoint marker
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(pos, new Vector3(0.5f, 1f, 0f));

            // Draw flag pole
            Gizmos.DrawLine(pos, pos + Vector3.up * 1.5f);
            
            // Draw flag
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Vector3 flagTop = pos + Vector3.up * 1.5f;
            Vector3 flagBottom = pos + Vector3.up * 1f;
            Vector3 flagRight = flagTop + Vector3.right * 0.4f;
            Gizmos.DrawLine(flagTop, flagRight);
            Gizmos.DrawLine(flagRight, flagBottom);
            Gizmos.DrawLine(flagBottom, flagTop);
        }
    }
}