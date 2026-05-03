using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Limits the camera movement to stay within defined bounds.
    /// Works with CameraZone triggers to change bounds per section.
    /// 
    /// Setup:
    /// 1. Add this to your Main Camera
    /// 2. Create CameraZone triggers for each section of your level
    /// </summary>
    public class CameraBounds : MonoBehaviour
    {
        [Header("Default Bounds")]
        [Tooltip("Used if no CameraZone is active")]
        [SerializeField] private Bounds _defaultBounds = new Bounds(Vector3.zero, new Vector3(100, 50, 0));

        [Header("Transition")]
        [Tooltip("How fast the camera bounds transition between zones")]
        [SerializeField] private float _transitionSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Current bounds (smoothly interpolated)
        private Vector2 _currentMin;
        private Vector2 _currentMax;
        private Vector2 _targetMin;
        private Vector2 _targetMax;

        private Camera _camera;
        private float _halfHeight;
        private float _halfWidth;

        // Singleton for easy access from CameraZone
        public static CameraBounds Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _camera = GetComponent<Camera>();
            CalculateCameraSize();

            // Initialize to default bounds
            _currentMin = _targetMin = (Vector2)_defaultBounds.min;
            _currentMax = _targetMax = (Vector2)_defaultBounds.max;
        }

        private void LateUpdate()
        {
            CalculateCameraSize();

            // Smoothly transition bounds
            _currentMin = Vector2.Lerp(_currentMin, _targetMin, _transitionSpeed * Time.deltaTime);
            _currentMax = Vector2.Lerp(_currentMax, _targetMax, _transitionSpeed * Time.deltaTime);

            ClampCamera();
        }

        private void CalculateCameraSize()
        {
            _halfHeight = _camera.orthographicSize;
            _halfWidth = _halfHeight * _camera.aspect;
        }

        private void ClampCamera()
        {
            Vector3 pos = transform.position;

            // Calculate clamped position
            float clampedX = Mathf.Clamp(pos.x, _currentMin.x + _halfWidth, _currentMax.x - _halfWidth);
            float clampedY = Mathf.Clamp(pos.y, _currentMin.y + _halfHeight, _currentMax.y - _halfHeight);

            // Handle case where bounds are smaller than camera view
            if (_currentMax.x - _currentMin.x < _halfWidth * 2)
            {
                clampedX = (_currentMin.x + _currentMax.x) / 2f;
            }
            if (_currentMax.y - _currentMin.y < _halfHeight * 2)
            {
                clampedY = (_currentMin.y + _currentMax.y) / 2f;
            }

            transform.position = new Vector3(clampedX, clampedY, pos.z);
        }

        /// <summary>
        /// Set new camera bounds (called by CameraZone)
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _targetMin = min;
            _targetMax = max;

            if (_showDebugMessages)
                Debug.Log($"CameraBounds: New bounds Min({min}) Max({max})");
        }

        /// <summary>
        /// Instantly snap to new bounds (no transition)
        /// </summary>
        public void SetBoundsImmediate(Vector2 min, Vector2 max)
        {
            _targetMin = _currentMin = min;
            _targetMax = _currentMax = max;
        }

        /// <summary>
        /// Reset to default bounds
        /// </summary>
        public void ResetToDefault()
        {
            _targetMin = (Vector2)_defaultBounds.min;
            _targetMax = (Vector2)_defaultBounds.max;
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            // Draw current bounds
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (_currentMin.x + _currentMax.x) / 2f,
                (_currentMin.y + _currentMax.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                _currentMax.x - _currentMin.x,
                _currentMax.y - _currentMin.y,
                0f
            );
            Gizmos.DrawWireCube(center, size);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw camera preview
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                Gizmos.color = Color.cyan;
                float h = _camera.orthographicSize;
                float w = h * _camera.aspect;
                Gizmos.DrawWireCube(transform.position, new Vector3(w * 2f, h * 2f, 0f));
            }
        }
    }
}