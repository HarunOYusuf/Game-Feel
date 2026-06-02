using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Controls a group of linked platforms that move together.
    /// When activated, each platform moves to its activated position.
    /// When deactivated, each platform returns to its default position.
    /// 
    /// Movement runs in FixedUpdate using Rigidbody2D.MovePosition for proper physics sync.
    /// 
    /// The player (and clones) automatically ride this platform via "ground velocity
    /// inheritance" implemented in UltimatePlayerController and CloneMovement —
    /// they detect the moving Kinematic Rigidbody2D they're standing on and inherit
    /// its velocity. No PlatformRider component needed.
    /// 
    /// Setup:
    /// 1. Create an empty GameObject called "LinkedPlatformGroup"
    /// 2. Add this script
    /// 3. Add your platform PARENTS to the Platforms list
    /// 4. Each platform parent must have a Rigidbody2D (auto-added as Kinematic)
    /// 5. Set the movement offset for each platform (positive = up, negative = down)
    /// </summary>
    [DefaultExecutionOrder(-50)] // Run before player controllers so they see updated platform velocity
    public class LinkedPlatformGroup : MonoBehaviour
    {
        [System.Serializable]
        public class PlatformEntry
        {
            [Tooltip("The platform parent GameObject to move (children will move with it)")]
            public Transform Platform;
            
            [Tooltip("How far to move when activated (positive = up, negative = down)")]
            public float VerticalOffset = 2f;
            
            // Internal cached references and positions
            [HideInInspector] public Vector3 DefaultPosition;
            [HideInInspector] public Vector3 ActivatedPosition;
            [HideInInspector] public Rigidbody2D Rigidbody;
        }

        [Header("Platforms")]
        [Tooltip("List of platform PARENTS and their movement offsets")]
        [SerializeField] private List<PlatformEntry> _platforms = new List<PlatformEntry>();

        [Header("Movement Settings")]
        [Tooltip("How fast platforms move between positions (units per second)")]
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

        private void Awake()
        {
            // Ensure each platform has a Kinematic Rigidbody2D and a velocity tracker
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null) continue;

                entry.Rigidbody = entry.Platform.GetComponent<Rigidbody2D>();
                if (entry.Rigidbody == null)
                {
                    entry.Rigidbody = entry.Platform.gameObject.AddComponent<Rigidbody2D>();
                    if (_showDebugMessages)
                    {
                        Debug.Log($"LinkedPlatformGroup: Auto-added Rigidbody2D to '{entry.Platform.name}'");
                    }
                }
                entry.Rigidbody.bodyType = RigidbodyType2D.Kinematic;
                entry.Rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

                // Ensure PlatformVelocityTracker exists so riders can inherit motion
                // (Kinematic MovePosition does not populate linearVelocity reliably)
                if (entry.Platform.GetComponent<PlatformVelocityTracker>() == null)
                {
                    entry.Platform.gameObject.AddComponent<PlatformVelocityTracker>();
                    if (_showDebugMessages)
                    {
                        Debug.Log($"LinkedPlatformGroup: Auto-added PlatformVelocityTracker to '{entry.Platform.name}'");
                    }
                }
            }
        }

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

        private void FixedUpdate()
        {
            // Move all platforms in lockstep with the physics step
            // Use MovePosition on Kinematic Rigidbody2D so:
            //  1. Physics integrates the movement properly
            //  2. linearVelocity is readable by other scripts (player inheritance)
            //  3. Child colliders move correctly with the parent
            foreach (var entry in _platforms)
            {
                if (entry.Platform == null || entry.Rigidbody == null) continue;

                Vector3 currentPosition = entry.Rigidbody.position;
                Vector3 targetPosition = _isActivated ? entry.ActivatedPosition : entry.DefaultPosition;
                
                if (Vector3.Distance(currentPosition, targetPosition) > _arrivalThreshold)
                {
                    Vector3 newPosition = Vector3.MoveTowards(
                        currentPosition,
                        targetPosition,
                        _moveSpeed * Time.fixedDeltaTime
                    );

                    entry.Rigidbody.MovePosition(newPosition);
                }
                else
                {
                    // Snap to target and clear velocity so riders know to stop moving
                    if (entry.Rigidbody.position != (Vector2)targetPosition)
                    {
                        entry.Rigidbody.MovePosition(targetPosition);
                    }
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

                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(defaultPos, size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(defaultPos, size);

                Vector3 activatedCenter = defaultPos + new Vector3(0f, entry.VerticalOffset, 0f);
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawCube(activatedCenter, size);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(activatedCenter, size);

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
                Gizmos.DrawLine(defaultPos, activatedCenter);

                Vector3 direction = entry.VerticalOffset > 0 ? Vector3.up : Vector3.down;
                Vector3 midPoint = (defaultPos + activatedCenter) / 2f;
                Gizmos.DrawLine(midPoint, midPoint + direction * 0.3f);
            }
        }

        private void OnDrawGizmosSelected()
        {
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