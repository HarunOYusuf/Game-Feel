using UnityEngine;
using System.Collections.Generic;

namespace UltimateController
{
    /// <summary>
    /// A pressure plate that activates when the player or clone stands on it.
    /// Can trigger doors, moving platforms, or any other connected objects.
    /// 
    /// Setup:
    /// 1. Create a sprite for the plate
    /// 2. Add BoxCollider2D, set to "Is Trigger"
    /// 3. Add this script
    /// 4. Link objects to activate in the "Connected Objects" list
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        public enum ActivationMode
        {
            HoldToActivate,     // Must stay on plate to keep it active (clone puzzle)
            ToggleOnStep,       // Steps on = toggle state (on/off)
            OneTimeActivation   // Once activated, stays active forever
        }

        [Header("Activation Mode")]
        [Tooltip("How the pressure plate behaves")]
        [SerializeField] private ActivationMode _activationMode = ActivationMode.HoldToActivate;

        [Header("Connected Objects")]
        [Tooltip("Objects that respond to this pressure plate")]
        [SerializeField] private List<PressurePlateReceiver> _connectedReceivers = new List<PressurePlateReceiver>();

        [Header("Detection")]
        [Tooltip("Can the player activate this plate?")]
        [SerializeField] private bool _detectPlayer = true;
        
        [Tooltip("Can clones activate this plate?")]
        [SerializeField] private bool _detectClones = true;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _inactiveColour = Color.grey;
        [SerializeField] private Color _activeColour = Color.green;
        
        [Header("Animation")]
        [Tooltip("How much the plate moves down when pressed")]
        [SerializeField] private float _pressDepth = 0.1f;
        [SerializeField] private float _pressSpeed = 10f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource _activateSound;
        [SerializeField] private AudioSource _deactivateSound;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // State
        private HashSet<GameObject> _objectsOnPlate = new HashSet<GameObject>();
        private bool _isPressed;
        private bool _isActivated; // For toggle and one-time modes
        private bool _hasBeenActivated; // For one-time mode
        private Vector3 _originalPosition;
        private Vector3 _pressedPosition;

        /// <summary>
        /// Is the plate currently pressed down?
        /// </summary>
        public bool IsPressed => _isPressed;

        /// <summary>
        /// Is the plate currently activated (triggering receivers)?
        /// </summary>
        public bool IsActivated => _isActivated;

        // Events
        public event System.Action OnPressed;
        public event System.Action OnReleased;
        public event System.Action OnActivated;
        public event System.Action OnDeactivated;

        private void Start()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Store positions for press animation
            _originalPosition = transform.position;
            _pressedPosition = _originalPosition + Vector3.down * _pressDepth;

            // Set initial colour
            UpdateVisuals();
        }

        private void Update()
        {
            // Animate plate position
            Vector3 targetPos = _isPressed ? _pressedPosition : _originalPosition;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, _pressSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (ShouldDetect(other))
            {
                _objectsOnPlate.Add(other.gameObject);
                
                if (_showDebugMessages)
                    Debug.Log($"PressurePlate: {other.name} entered");

                UpdateState();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_objectsOnPlate.Contains(other.gameObject))
            {
                _objectsOnPlate.Remove(other.gameObject);
                
                if (_showDebugMessages)
                    Debug.Log($"PressurePlate: {other.name} exited");

                UpdateState();
            }
        }

        private bool ShouldDetect(Collider2D other)
        {
            // Check for player (must be main collider, not DashSprite)
            if (_detectPlayer)
            {
                var controller = other.GetComponent<UltimatePlayerController>();
                if (controller != null && other.gameObject == controller.gameObject)
                {
                    // Make sure it's not a clone
                    if (other.GetComponent<TimeClone>() == null)
                        return true;
                }
            }

            // Check for clone
            if (_detectClones)
            {
                var clone = other.GetComponent<TimeClone>();
                if (clone != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateState()
        {
            // Clean up any destroyed objects
            _objectsOnPlate.RemoveWhere(obj => obj == null);

            bool somethingOnPlate = _objectsOnPlate.Count > 0;
            
            // Handle press state change
            if (somethingOnPlate != _isPressed)
            {
                _isPressed = somethingOnPlate;
                
                if (_isPressed)
                {
                    OnPressed?.Invoke();
                    if (_showDebugMessages)
                        Debug.Log("PressurePlate: PRESSED");
                }
                else
                {
                    OnReleased?.Invoke();
                    if (_showDebugMessages)
                        Debug.Log("PressurePlate: RELEASED");
                }
            }

            // Handle activation based on mode
            bool shouldBeActivated = false;
            
            switch (_activationMode)
            {
                case ActivationMode.HoldToActivate:
                    // Only active while something is on the plate
                    shouldBeActivated = somethingOnPlate;
                    break;

                case ActivationMode.ToggleOnStep:
                    // Toggle when stepped on
                    if (somethingOnPlate && !_isPressed)
                    {
                        // Just stepped on - toggle
                        shouldBeActivated = !_isActivated;
                    }
                    else
                    {
                        // Keep current state
                        shouldBeActivated = _isActivated;
                    }
                    break;

                case ActivationMode.OneTimeActivation:
                    // Once activated, stays active forever
                    if (somethingOnPlate)
                    {
                        _hasBeenActivated = true;
                    }
                    shouldBeActivated = _hasBeenActivated;
                    break;
            }

            // Update activation state
            if (shouldBeActivated != _isActivated)
            {
                _isActivated = shouldBeActivated;
                UpdateVisuals();
                NotifyReceivers();

                if (_isActivated)
                {
                    if (_showDebugMessages)
                        Debug.Log("PressurePlate: ACTIVATED");

                    if (_activateSound != null)
                        _activateSound.Play();

                    OnActivated?.Invoke();
                }
                else
                {
                    if (_showDebugMessages)
                        Debug.Log("PressurePlate: DEACTIVATED");

                    if (_deactivateSound != null)
                        _deactivateSound.Play();

                    OnDeactivated?.Invoke();
                }
            }
        }

        private void UpdateVisuals()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _isActivated ? _activeColour : _inactiveColour;
            }
        }

        private void NotifyReceivers()
        {
            foreach (var receiver in _connectedReceivers)
            {
                if (receiver != null)
                {
                    receiver.OnPressurePlateChanged(_isActivated);
                }
            }
        }

        /// <summary>
        /// Manually add a receiver at runtime
        /// </summary>
        public void AddReceiver(PressurePlateReceiver receiver)
        {
            if (!_connectedReceivers.Contains(receiver))
            {
                _connectedReceivers.Add(receiver);
            }
        }

        /// <summary>
        /// Reset the plate (for one-time activation mode)
        /// </summary>
        public void Reset()
        {
            _hasBeenActivated = false;
            _isActivated = false;
            UpdateVisuals();
            NotifyReceivers();
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            
            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }

            // Draw lines to connected receivers
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            foreach (var receiver in _connectedReceivers)
            {
                if (receiver != null)
                {
                    Gizmos.DrawLine(transform.position, receiver.transform.position);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            string modeText = _activationMode switch
            {
                ActivationMode.HoldToActivate => "HOLD (Clone must stay)",
                ActivationMode.ToggleOnStep => "TOGGLE (Step to switch)",
                ActivationMode.OneTimeActivation => "ONE-TIME (Stays active)",
                _ => ""
            };
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, modeText);
            #endif
        }
    }
}