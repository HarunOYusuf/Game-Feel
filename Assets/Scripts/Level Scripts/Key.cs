using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A collectible key that allows the player to open doors.
    /// 
    /// Setup:
    /// 1. Create a sprite for the key
    /// 2. Add BoxCollider2D or CircleCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Key : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Unique ID for this key (match with Door's Required Key ID)")]
        [SerializeField] private string _keyID = "key_1";

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _collectParticles;
        [SerializeField] private AudioSource _collectSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        /// <summary>
        /// This key's unique ID
        /// </summary>
        public string KeyID => _keyID;

        private void Start()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if player collected the key
            var player = other.GetComponent<UltimatePlayerController>();
            if (player == null) return;

            // Get or add PlayerInventory
            var inventory = other.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = other.gameObject.AddComponent<PlayerInventory>();
            }

            // Add key to inventory
            inventory.AddKey(_keyID);

            if (_showDebugMessages)
                Debug.Log($"Key collected: {_keyID}");

            // Effects
            if (_collectParticles != null)
            {
                var particles = Instantiate(_collectParticles, transform.position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            if (_collectSound != null)
            {
                // Play sound at position (survives object destruction)
                AudioSource.PlayClipAtPoint(_collectSound.clip, transform.position);
            }

            // Destroy the key
            Destroy(gameObject);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw key icon
            Vector3 pos = transform.position;
            Gizmos.DrawLine(pos + Vector3.left * 0.15f, pos + Vector3.right * 0.15f);
            Gizmos.DrawLine(pos + Vector3.right * 0.15f, pos + Vector3.right * 0.15f + Vector3.up * 0.1f);
            Gizmos.DrawLine(pos + Vector3.right * 0.15f, pos + Vector3.right * 0.15f + Vector3.down * 0.1f);
        }
    }
}