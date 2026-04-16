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
    /// 3. Set the spawn point (or it uses player's starting position)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [Tooltip("Where to respawn the player. If empty, uses player's starting position.")]
        [SerializeField] private Transform _respawnPoint;
        
        [Tooltip("Delay before respawning (for death animation/effect)")]
        [SerializeField] private float _respawnDelay = 0.5f;

        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private AudioSource _deathSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Cached player start position
        private static Vector2 _currentCheckpoint;
        private static bool _checkpointSet = false;

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
                _deathParticles.transform.position = player.transform.position;
                _deathParticles.Play();
            }

            if (_deathSound != null)
            {
                _deathSound.Play();
            }

            // Get respawn position
            Vector2 respawnPos = GetRespawnPosition(player);

            // Respawn player (with optional delay)
            if (_respawnDelay > 0)
            {
                // Hide player briefly
                player.gameObject.SetActive(false);
                StartCoroutine(RespawnAfterDelay(player, respawnPos));
            }
            else
            {
                RespawnPlayer(player, respawnPos);
            }
        }

        private Vector2 GetRespawnPosition(UltimatePlayerController player)
        {
            // Priority: Respawn point > Checkpoint > Player start position
            if (_respawnPoint != null)
            {
                return _respawnPoint.position;
            }
            
            if (_checkpointSet)
            {
                return _currentCheckpoint;
            }

            // First time - store player's starting position as default checkpoint
            if (!_checkpointSet)
            {
                _currentCheckpoint = player.transform.position;
                _checkpointSet = true;
            }

            return _currentCheckpoint;
        }

        private System.Collections.IEnumerator RespawnAfterDelay(UltimatePlayerController player, Vector2 position)
        {
            yield return new WaitForSeconds(_respawnDelay);
            
            player.gameObject.SetActive(true);
            RespawnPlayer(player, position);
        }

        private void RespawnPlayer(UltimatePlayerController player, Vector2 position)
        {
            player.Teleport(position);
            
            if (_showDebugMessages)
                Debug.Log($"Player respawned at {position}");
        }

        /// <summary>
        /// Call this to set a new checkpoint (from a Checkpoint script)
        /// </summary>
        public static void SetCheckpoint(Vector2 position)
        {
            _currentCheckpoint = position;
            _checkpointSet = true;
        }

        /// <summary>
        /// Reset checkpoint (useful for restarting level)
        /// </summary>
        public static void ResetCheckpoint()
        {
            _checkpointSet = false;
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