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
        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private AudioSource _deathSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

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
            // Must have controller DIRECTLY on this collider's GameObject (not parent)
            // This prevents DashSprite's collider from triggering death
            if (!other.TryGetComponent<UltimatePlayerController>(out var controller))
                return;

            KillPlayer(controller);
        }

        private void KillPlayer(UltimatePlayerController player)
        {
            if (_showDebugMessages)
                Debug.Log($"Player killed by {gameObject.name}");

            // Play effects
            if (_deathParticles != null)
            {
                var particles = Instantiate(_deathParticles, player.transform.position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            if (_deathSound != null)
            {
                _deathSound.Play();
            }

            // Respawn via GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDeath();
            }
            else
            {
                // Fallback: just teleport to origin
                player.Teleport(Vector2.zero);
            }

            if (_showDebugMessages)
                Debug.Log("Player respawned");
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