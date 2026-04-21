using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Triggers level completion when player enters.
    /// Place at the end of your level.
    /// 
    /// Setup:
    /// 1. Create empty GameObject at level end
    /// 2. Add BoxCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelEnd : MonoBehaviour
    {
        [Header("Effects (Optional)")]
        [SerializeField] private ParticleSystem _completionParticles;
        [SerializeField] private AudioSource _completionSound;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        private bool _triggered;

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
            if (_triggered) return;

            // Only trigger for player
            if (other.GetComponent<UltimatePlayerController>() == null)
                return;

            _triggered = true;

            if (_showDebugMessages)
                Debug.Log("LevelEnd: Player reached the end!");

            // Effects
            if (_completionParticles != null)
            {
                _completionParticles.Play();
            }

            if (_completionSound != null)
            {
                _completionSound.Play();
            }

            // Tell GameManager to restart (for now)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                
                // Draw finish flag icon
                Gizmos.matrix = Matrix4x4.identity;
                Vector3 centre = transform.position + (Vector3)box.offset;
                Gizmos.DrawLine(centre + Vector3.down * 0.5f, centre + Vector3.up * 1f);
                Gizmos.DrawWireCube(centre + Vector3.up * 0.8f + Vector3.right * 0.2f, new Vector3(0.4f, 0.3f, 0f));
            }
        }
    }
}