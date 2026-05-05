using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A collectible key that allows the player to open doors.
    /// Can be set to only be collectible by clones.
    /// 
    /// Setup:
    /// 1. Create a sprite for the key
    /// 2. Add BoxCollider2D or CircleCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// 4. For clone-only keys, check "Clone Only"
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Key : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Unique ID for this key (match with Door's Required Key ID)")]
        [SerializeField] private string _keyID = "key_1";

        [Header("Collection Rules")]
        [Tooltip("If true, only clones can collect this key (not the player)")]
        [SerializeField] private bool _cloneOnly = false;

        [Header("Visuals")]
        [Tooltip("Colour tint for clone-only keys (green by default)")]
        [SerializeField] private Color _cloneOnlyColour = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _collectParticles;
        [SerializeField] private AudioSource _collectSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// This key's unique ID
        /// </summary>
        public string KeyID => _keyID;

        /// <summary>
        /// Is this key only collectible by clones?
        /// </summary>
        public bool CloneOnly => _cloneOnly;

        private void Start()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }

            // Apply colour tint for clone-only keys
            if (_cloneOnly)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                    _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                    
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _cloneOnlyColour;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if it's a clone
            var clone = other.GetComponent<TimeClone>();
            bool isClone = clone != null;

            // Check if it's the player
            var player = other.GetComponent<UltimatePlayerController>();
            bool isPlayer = player != null;

            // Determine if collection is allowed
            if (_cloneOnly)
            {
                // Clone-only key: only clones can collect
                if (!isClone)
                {
                    if (isPlayer && _showDebugMessages)
                        Debug.Log($"Key {_keyID}: Only clones can collect this key!");
                    return;
                }
            }
            else
            {
                // Normal key: only player can collect (not clones)
                if (!isPlayer)
                    return;
            }

            // Find the player's inventory (clones give keys to the player)
            PlayerInventory inventory = null;
            
            if (isClone)
            {
                // Clone collected - find the REAL player (not another clone)
                UltimatePlayerController playerController = null;
                
                // Find all UltimatePlayerControllers and pick the one that's NOT a clone
                var allControllers = FindObjectsByType<UltimatePlayerController>(FindObjectsSortMode.None);
                foreach (var controller in allControllers)
                {
                    // Skip if this object also has TimeClone component (it's a clone, not the player)
                    if (controller.GetComponent<TimeClone>() != null)
                        continue;
                    
                    playerController = controller;
                    break;
                }
                
                // Fallback: Find by tag (player should be tagged, clones aren't)
                if (playerController == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null && playerObj.GetComponent<TimeClone>() == null)
                        playerController = playerObj.GetComponent<UltimatePlayerController>();
                }
                
                if (playerController != null)
                {
                    inventory = playerController.GetComponent<PlayerInventory>();
                    if (inventory == null)
                    {
                        inventory = playerController.gameObject.AddComponent<PlayerInventory>();
                        if (_showDebugMessages)
                            Debug.Log($"Key: Added PlayerInventory to {playerController.gameObject.name}");
                    }
                    
                    if (_showDebugMessages)
                        Debug.Log($"Key: Found REAL player '{playerController.gameObject.name}' for clone key transfer");
                }
                else
                {
                    Debug.LogError("Key: Clone collected key but could not find the real player!");
                    return;
                }
            }
            else
            {
                // Player collected directly
                inventory = player.GetComponent<PlayerInventory>();
                if (inventory == null)
                    inventory = player.gameObject.AddComponent<PlayerInventory>();
            }

            if (inventory == null)
            {
                Debug.LogWarning("Key: Could not find PlayerInventory!");
                return;
            }

            // Add key to inventory
            inventory.AddKey(_keyID);

            if (_showDebugMessages)
            {
                string collector = isClone ? "Clone" : "Player";
                Debug.Log($"Key collected by {collector}: {_keyID}");
            }

            // Effects
            if (_collectParticles != null)
            {
                var particles = Instantiate(_collectParticles, transform.position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound.clip, transform.position);
            }

            // Destroy the key
            Destroy(gameObject);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = _cloneOnly ? new Color(0.2f, 1f, 0.4f, 1f) : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw key icon
            Vector3 pos = transform.position;
            Gizmos.DrawLine(pos + Vector3.left * 0.15f, pos + Vector3.right * 0.15f);
            Gizmos.DrawLine(pos + Vector3.right * 0.15f, pos + Vector3.right * 0.15f + Vector3.up * 0.1f);
            Gizmos.DrawLine(pos + Vector3.right * 0.15f, pos + Vector3.right * 0.15f + Vector3.down * 0.1f);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            if (_cloneOnly)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "CLONE ONLY");
            }
            #endif
        }
    }
}