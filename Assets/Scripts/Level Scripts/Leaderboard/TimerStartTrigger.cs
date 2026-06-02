using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Place this on a trigger Collider2D at the end of the tutorial.
    /// The first time the player passes through it, the level timer starts.
    /// 
    /// Setup:
    /// 1. Create an empty GameObject at the tutorial-end location
    /// 2. Add a BoxCollider2D (or any 2D collider), tick "Is Trigger"
    /// 3. Add this script
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TimerStartTrigger : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        private bool _hasTriggered;

        private void Awake()
        {
            // Ensure the collider is set as a trigger
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;

            // Only the real player should trigger this — not clones, not other objects
            if (!other.TryGetComponent<UltimatePlayerController>(out _)) return;
            if (other.GetComponent<TimeClone>() != null) return;
            if (other.GetComponent<CloneMovement>() != null) return;

            _hasTriggered = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartTimer();
                
                if (_showDebugMessages)
                {
                    Debug.Log($"TimerStartTrigger: Timer started by player entering '{gameObject.name}'");
                }
            }
            else
            {
                Debug.LogWarning("TimerStartTrigger: No GameManager found in scene!");
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;

            Gizmos.color = _hasTriggered 
                ? new Color(0.3f, 0.3f, 0.3f, 0.4f)   // Grey when already triggered
                : new Color(0.4f, 1f, 0.6f, 0.4f);    // Green when active

            Gizmos.DrawCube(col.bounds.center, col.bounds.size);

            Gizmos.color = _hasTriggered ? Color.grey : Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.3f),
                "TIMER START"
            );
            #endif
        }
    }
}