using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A door that opens when the player has the matching key.
    /// Stays solid until opened, then plays animation and disables collider.
    /// 
    /// Setup:
    /// 1. Create a sprite for the door with Animator
    /// 2. Add BoxCollider2D (NOT a trigger - it's solid)
    /// 3. Add this script
    /// 4. Set the Required Key ID to match your Key's ID
    /// 5. Assign the Animator (or it will auto-find on children)
    /// 6. Make sure your Animator has a trigger parameter called "Open"
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The key ID required to open this door (must match Key's ID)")]
        [SerializeField] private string _requiredKeyID = "key_1";
        
        [Tooltip("Does the key get consumed when opening the door?")]
        [SerializeField] private bool _consumeKey = true;

        [Header("Animation")]
        [Tooltip("Animator with opening animation (auto-finds in children if not set)")]
        [SerializeField] private Animator _animator;
        
        [Tooltip("Trigger parameter name in the Animator")]
        [SerializeField] private string _openTriggerName = "Open";
        
        [Tooltip("Destroy door after animation? (set to animation length)")]
        [SerializeField] private bool _destroyAfterAnimation = false;
        [SerializeField] private float _destroyDelay = 1f;

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _openParticles;
        [SerializeField] private AudioSource _openSound;
        [SerializeField] private AudioSource _lockedSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        // State
        private bool _isOpen;
        private Collider2D _collider;

        /// <summary>
        /// Is this door open?
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// The key ID required to open this door
        /// </summary>
        public string RequiredKeyID => _requiredKeyID;

        private void Start()
        {
            _collider = GetComponent<Collider2D>();
            
            // Door should be solid (not trigger)
            if (_collider.isTrigger)
            {
                _collider.isTrigger = false;
            }

            // Auto-find animator if not assigned
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isOpen) return;

            // Check if player touched the door
            var player = collision.collider.GetComponent<UltimatePlayerController>();
            if (player == null) return;

            TryOpen(collision.collider.gameObject);
        }

        private void TryOpen(GameObject playerObject)
        {
            // Get the player's inventory
            // First try the colliding object, then search for the player
            var inventory = playerObject.GetComponent<PlayerInventory>();
            
            if (inventory == null)
            {
                // Inventory might be on a different player object - find it
                var player = FindFirstObjectByType<UltimatePlayerController>();
                if (player != null)
                {
                    inventory = player.GetComponent<PlayerInventory>();
                }
            }
            
            if (inventory != null && inventory.HasKey(_requiredKeyID))
            {
                OpenDoor(inventory);
            }
            else
            {
                // Player doesn't have the key
                if (_showDebugMessages)
                {
                    if (inventory == null)
                        Debug.Log($"Door locked! No inventory found.");
                    else
                        Debug.Log($"Door locked! Requires key: {_requiredKeyID}. Player has: {string.Join(", ", inventory.GetAllKeys())}");
                }

                if (_lockedSound != null)
                {
                    _lockedSound.Play();
                }
            }
        }

        private void OpenDoor(PlayerInventory inventory)
        {
            _isOpen = true;

            if (_showDebugMessages)
                Debug.Log($"Door opened with key: {_requiredKeyID}");

            // Consume the key if configured
            if (_consumeKey)
            {
                inventory.RemoveKey(_requiredKeyID);
            }

            // Disable collider so player can walk through
            _collider.enabled = false;

            // Play opening animation
            if (_animator != null)
            {
                _animator.SetTrigger(_openTriggerName);
            }

            // Effects
            if (_openParticles != null)
            {
                _openParticles.Play();
            }

            if (_openSound != null)
            {
                _openSound.Play();
            }

            // Optionally destroy after animation
            if (_destroyAfterAnimation)
            {
                Destroy(gameObject, _destroyDelay);
            }
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = _isOpen ? Color.green : Color.red;
            
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.DrawCube(box.offset, box.size * 0.9f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Show required key ID above door
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"Requires: {_requiredKeyID}");
            #endif
        }
    }
}