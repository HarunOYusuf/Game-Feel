using UnityEngine;
using System.Collections.Generic;

namespace UltimateController
{
    /// <summary>
    /// Pressure plate that activates linked platforms when stepped on.
    /// Platforms become solid and visible when plate is pressed.
    /// 
    /// Setup:
    /// 1. Create pressure plate GameObject with BoxCollider2D (trigger)
    /// 2. Add this script
    /// 3. Create platform GameObjects with Collider2D and SpriteRenderer
    /// 4. Drag platforms into the Linked Platforms list
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PressurePlatePlatform : MonoBehaviour
    {
        [Header("Linked Platforms")]
        [Tooltip("Platforms that appear when this plate is pressed")]
        [SerializeField] private List<GameObject> _linkedPlatforms = new List<GameObject>();

        [Header("Appearance")]
        [Tooltip("Alpha when platforms are inactive (0 = invisible, 0.3 = ghostly)")]
        [SerializeField] private float _inactiveAlpha = 0.3f;
        
        [Tooltip("Alpha when platforms are active")]
        [SerializeField] private float _activeAlpha = 1f;
        
        [Tooltip("How fast platforms fade in/out")]
        [SerializeField] private float _fadeSpeed = 8f;

        [Header("Plate Visuals")]
        [SerializeField] private Color _unpressedColour = Color.grey;
        [SerializeField] private Color _pressedColour = Color.green;

        [Header("Settings")]
        [Tooltip("Does the clone also activate the plate?")]
        [SerializeField] private bool _cloneCanActivate = true;
        
        [Tooltip("Stay active for this long after player leaves (0 = instant off)")]
        [SerializeField] private float _deactivateDelay = 0f;

        // Components
        private BoxCollider2D _trigger;
        private SpriteRenderer _plateRenderer;

        // State
        private int _activatorCount = 0;
        private bool _isActive;
        private float _deactivateTimer;
        private Dictionary<GameObject, SpriteRenderer[]> _platformRenderers = new Dictionary<GameObject, SpriteRenderer[]>();
        private Dictionary<GameObject, Collider2D[]> _platformColliders = new Dictionary<GameObject, Collider2D[]>();
        private Dictionary<GameObject, float> _platformAlphas = new Dictionary<GameObject, float>();

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider2D>();
            _trigger.isTrigger = true;

            _plateRenderer = GetComponent<SpriteRenderer>();

            // Cache platform components (including children)
            foreach (var platform in _linkedPlatforms)
            {
                if (platform == null) continue;

                // Get ALL SpriteRenderers (including children)
                var renderers = platform.GetComponentsInChildren<SpriteRenderer>(true);
                if (renderers.Length > 0)
                {
                    _platformRenderers[platform] = renderers;
                    _platformAlphas[platform] = _inactiveAlpha;
                }

                // Get ALL Colliders (including children)
                var colliders = platform.GetComponentsInChildren<Collider2D>(true);
                if (colliders.Length > 0)
                {
                    _platformColliders[platform] = colliders;
                }
            }
            
            Debug.Log($"PressurePlatePlatform: Found {_linkedPlatforms.Count} platforms");
        }

        private void Start()
        {
            // Start with platforms inactive
            SetPlatformsActive(false, true);
            UpdatePlateVisual();
        }

        private void Update()
        {
            // Handle deactivate delay
            if (!_isActive && _deactivateTimer > 0)
            {
                _deactivateTimer -= Time.deltaTime;
                if (_deactivateTimer <= 0)
                {
                    SetPlatformsActive(false, false);
                }
            }

            // Smooth alpha transitions
            foreach (var platform in _linkedPlatforms)
            {
                if (platform == null) continue;
                if (!_platformRenderers.TryGetValue(platform, out var renderers)) continue;
                if (!_platformAlphas.TryGetValue(platform, out var currentAlpha)) continue;

                float targetAlpha = _isActive ? _activeAlpha : _inactiveAlpha;

                if (!Mathf.Approximately(currentAlpha, targetAlpha))
                {
                    currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, _fadeSpeed * Time.deltaTime);
                    _platformAlphas[platform] = currentAlpha;

                    foreach (var sr in renderers)
                    {
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = currentAlpha;
                            sr.color = c;
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"PressurePlatePlatform: Something entered - {other.name}");
            
            if (IsValidActivator(other))
            {
                Debug.Log($"PressurePlatePlatform: Valid activator! Activating {_linkedPlatforms.Count} platforms");
                _activatorCount++;
                
                if (_activatorCount > 0 && !_isActive)
                {
                    _isActive = true;
                    _deactivateTimer = 0;
                    SetPlatformsActive(true, false);
                    UpdatePlateVisual();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Debug.Log($"PressurePlatePlatform: Something exited - {other.name}");
            
            if (IsValidActivator(other))
            {
                _activatorCount = Mathf.Max(0, _activatorCount - 1);
                
                if (_activatorCount == 0 && _isActive)
                {
                    _isActive = false;
                    
                    if (_deactivateDelay > 0)
                    {
                        _deactivateTimer = _deactivateDelay;
                    }
                    else
                    {
                        SetPlatformsActive(false, false);
                    }
                    
                    UpdatePlateVisual();
                }
            }
        }

        private bool IsValidActivator(Collider2D other)
        {
            // Check for player
            var player = other.GetComponent<UltimatePlayerController>();
            if (player != null) return true;

            // Check for clone
            if (_cloneCanActivate)
            {
                var clone = other.GetComponent<CloneMovement>();
                if (clone != null) return true;

                var oldClone = other.GetComponent<TimeClone>();
                if (oldClone != null) return true;
            }

            return false;
        }

        private void SetPlatformsActive(bool active, bool instant)
        {
            foreach (var platform in _linkedPlatforms)
            {
                if (platform == null) continue;

                // Set all colliders
                if (_platformColliders.TryGetValue(platform, out var colliders))
                {
                    foreach (var col in colliders)
                    {
                        if (col != null)
                            col.enabled = active;
                    }
                }

                // Set alpha instantly if needed
                if (instant && _platformRenderers.TryGetValue(platform, out var renderers))
                {
                    float alpha = active ? _activeAlpha : _inactiveAlpha;
                    _platformAlphas[platform] = alpha;
                    
                    foreach (var sr in renderers)
                    {
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = alpha;
                            sr.color = c;
                        }
                    }
                }
            }
            
            Debug.Log($"PressurePlatePlatform: Platforms set to {(active ? "ACTIVE" : "INACTIVE")}");
        }

        private void UpdatePlateVisual()
        {
            if (_plateRenderer != null)
            {
                _plateRenderer.color = _isActive ? _pressedColour : _unpressedColour;
            }
        }

        // Editor visualisation
        private void OnDrawGizmos()
        {
            // Draw pressure plate
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.offset, col.size);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.offset, col.size);
            }

            // Draw lines to linked platforms
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            
            foreach (var platform in _linkedPlatforms)
            {
                if (platform != null)
                {
                    Gizmos.DrawLine(transform.position, platform.transform.position);
                    Gizmos.DrawWireCube(platform.transform.position, Vector3.one * 0.3f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight linked platforms when selected
            Gizmos.color = Color.cyan;
            foreach (var platform in _linkedPlatforms)
            {
                if (platform != null)
                {
                    var sr = platform.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Gizmos.DrawWireCube(platform.transform.position, sr.bounds.size);
                    }
                }
            }
        }
    }
}