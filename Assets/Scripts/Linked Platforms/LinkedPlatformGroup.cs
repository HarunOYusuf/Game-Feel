using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Controls a group of linked platforms that move together.
    /// When activated, each platform moves to its activated position.
    /// When deactivated, each platform returns to its default position.
    /// 
    /// Supports complex platforms with children (colliders, sprites as children).
    /// 
    /// Setup:
    /// 1. Create an empty GameObject called "LinkedPlatformGroup"
    /// 2. Add this script
    /// 3. Add your platform PARENTS to the Platforms list
    /// 4. Set the movement offset for each platform (positive = up, negative = down)
    /// </summary>
    public class LinkedPlatformGroup : MonoBehaviour
    {
        [System.Serializable]
        public class PlatformEntry
        {
            [Tooltip("The platform parent GameObject to move (children will move with it)")]
            public Transform Platform;
            
            [Tooltip("How far to move when activated (positive = up, negative = down)")]
            public float VerticalOffset = 2f;
            
            // Internal: stored default position
            [HideInInspector] public Vector3 DefaultPosition;
            [HideInInspector] public Vector3 ActivatedPosition;
        }

        [Header("Platforms")]
        [Tooltip("List of platform PARENTS and their movement offsets")]
        [SerializeField] private List<PlatformEntry> _platforms = new List<PlatformEntry>();

        [Header("Movement Settings")]
        [Tooltip("How fast platforms move between positions")]
        [SerializeField] private float _moveSpeed = 5f;
        
        [Tooltip("How close to target before considered arrived")]
        [SerializeField] private float _arrivalThreshold = 0.01f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        // State
        private bool _isActivated = false;
        private int _activatorCount = 0;

        /// <summary>
        /// Is the platform group currently activated?
        /// </summary>
        public bool IsActivated => _isActivated;

        private void Start()
        {
            // Store default positions and calculate activated positions
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null) continue;
                
                entry.DefaultPosition = entry.Platform.position;
                entry.ActivatedPosition = entry.DefaultPosition + new Vector3(0f, entry.VerticalOffset, 0f);
                
                if (_showDebugMessages)
                {
                    // Count children for debug info
                    int childColliders = entry.Platform.GetComponentsInChildren<Collider2D>().Length;
                    int childRenderers = entry.Platform.GetComponentsInChildren<SpriteRenderer>().Length;
                    Debug.Log($"LinkedPlatformGroup: Platform '{entry.Platform.name}' has {childColliders} colliders, {childRenderers} sprites in children");
                }
            }

            if (_showDebugMessages)
            {
                Debug.Log($"LinkedPlatformGroup: Initialised with {_platforms.Count} platforms");
            }
        }

        private void Update()
        {
            // Move all platform PARENTS towards their target positions
            // Children automatically move with the parent!
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null) continue;

                Vector3 targetPosition = _isActivated ? entry.ActivatedPosition : entry.DefaultPosition;
                
                // Only move if not already at target
                if (Vector3.Distance(entry.Platform.position, targetPosition) > _arrivalThreshold)
                {
                    entry.Platform.position = Vector3.MoveTowards(
                        entry.Platform.position,
                        targetPosition,
                        _moveSpeed * Time.deltaTime
                    );
                }
            }
        }

        /// <summary>
        /// Called by LinkedPlatformTrigger when something enters the trigger
        /// </summary>
        public void OnTriggerActivate()
        {
            _activatorCount++;
            
            if (_activatorCount > 0 && !_isActivated)
            {
                _isActivated = true;
                
                if (_showDebugMessages)
                {
                    Debug.Log("LinkedPlatformGroup: ACTIVATED");
                }
            }
        }

        /// <summary>
        /// Called by LinkedPlatformTrigger when something exits the trigger
        /// </summary>
        public void OnTriggerDeactivate()
        {
            _activatorCount = Mathf.Max(0, _activatorCount - 1);
            
            if (_activatorCount == 0 && _isActivated)
            {
                _isActivated = false;
                
                if (_showDebugMessages)
                {
                    Debug.Log("LinkedPlatformGroup: DEACTIVATED");
                }
            }
        }

        /// <summary>
        /// Manually set the activated state (for testing or other triggers)
        /// </summary>
        public void SetActivated(bool activated)
        {
            _isActivated = activated;
            _activatorCount = activated ? 1 : 0;
        }

        // Editor visualisation
        private void OnDrawGizmos()
        {
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null) continue;

                Vector3 defaultPos = Application.isPlaying ? entry.DefaultPosition : entry.Platform.position;
                Vector3 activatedPos = defaultPos + new Vector3(0f, entry.VerticalOffset, 0f);

                // Try to get bounds from children for better visualisation
                Bounds bounds = new Bounds(defaultPos, new Vector3(1f, 0.2f, 0f));
                var renderers = entry.Platform.GetComponentsInChildren<SpriteRenderer>();
                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    foreach (var r in renderers)
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }

                Vector3 size = bounds.size;
                size.z = 0.1f;

                // Draw default position (green)
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(defaultPos, size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(defaultPos, size);

                // Draw activated position (yellow)
                Vector3 activatedCenter = defaultPos + new Vector3(0f, entry.VerticalOffset, 0f);
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(activatedCenter, size);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(activatedCenter, size);

                // Draw line between positions
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
                Gizmos.DrawLine(defaultPos, activatedCenter);

                // Draw arrow showing direction
                Vector3 direction = entry.VerticalOffset > 0 ? Vector3.up : Vector3.down;
                Vector3 midPoint = (defaultPos + activatedCenter) / 2f;
                Gizmos.DrawLine(midPoint, midPoint + direction * 0.3f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Show platform labels when selected
            #if UNITY_EDITOR
            int index = 0;
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null) continue;
                
                Vector3 pos = entry.Platform.position;
                string direction = entry.VerticalOffset > 0 ? "UP" : "DOWN";
                string label = $"Platform {index + 1}\n{direction} {Mathf.Abs(entry.VerticalOffset)} units";
                
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 11;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                
                UnityEditor.Handles.Label(pos + Vector3.up * 1f, label, style);
                index++;
            }
            #endif
        }
    }
}