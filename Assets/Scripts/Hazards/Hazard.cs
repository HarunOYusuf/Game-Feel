using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Kills the player on contact and respawns them at the last checkpoint.
    /// Attach to any hazard (spikes, lava, pits, etc.)
    /// 
    /// Setup:
    /// 1. Add to your spike/hazard GameObject
    /// 2. Ensure it has a Collider2D set to "Is Trigger"
    /// 3. GameManager handles respawn position automatically
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [Tooltip("Delay before respawning (for death animation/effect)")]
        [SerializeField] private float _respawnDelay = 0.5f;

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private AudioSource _deathSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Fallback checkpoint (used if no GameManager)
        private static Vector2 _fallbackCheckpoint;
        private static bool _fallbackCheckpointSet = false;

        private void Start()
        {
            // Ensure collider is a trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"Hazard '{gameObject.name}': Collider set to trigger automatically.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if player entered
            var controller = other.GetComponent<UltimatePlayerController>();
            if (controller != null)
            {
                KillPlayer(controller);
            }
        }

        private void KillPlayer(UltimatePlayerController player)
        {
            if (_showDebugMessages)
                Debug.Log($"Player killed by {gameObject.name}");

            // Play effects
            if (_deathParticles != null)
            {
                // Spawn particles at player position
                var particles = Instantiate(_deathParticles, player.transform.position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            if (_deathSound != null)
            {
                _deathSound.Play();
            }

            // Respawn via GameManager or fallback
            if (_respawnDelay > 0)
            {
                player.gameObject.SetActive(false);
                StartCoroutine(RespawnAfterDelay(player));
            }
            else
            {
                RespawnPlayer(player);
            }
        }

        private System.Collections.IEnumerator RespawnAfterDelay(UltimatePlayerController player)
        {
            yield return new WaitForSeconds(_respawnDelay);
            
            player.gameObject.SetActive(true);
            RespawnPlayer(player);
        }

        private void RespawnPlayer(UltimatePlayerController player)
        {
            // Use GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDeath();
            }
            else
            {
                // Fallback: use static checkpoint
                Vector2 respawnPos = _fallbackCheckpointSet 
                    ? _fallbackCheckpoint 
                    : (Vector2)player.transform.position;
                    
                player.Teleport(respawnPos);
            }

            if (_showDebugMessages)
                Debug.Log("Player respawned");
        }

        /// <summary>
        /// Set fallback checkpoint (used when no GameManager exists)
        /// </summary>
        public static void SetCheckpoint(Vector2 position)
        {
            _fallbackCheckpoint = position;
            _fallbackCheckpointSet = true;
        }

        /// <summary>
        /// Reset fallback checkpoint
        /// </summary>
        public static void ResetCheckpoint()
        {
            _fallbackCheckpointSet = false;
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
        }
    }
}