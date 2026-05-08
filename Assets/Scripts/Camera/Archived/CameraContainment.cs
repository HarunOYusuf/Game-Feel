using UnityEngine;
using System.Collections.Generic;

namespace UltimateController
{
    /// <summary>
    /// Camera stays inside the combined area of multiple BoxCollider2D zones.
    /// Perfect for L-shaped, staircase, or complex level layouts.
    /// 
    /// Setup:
    /// 1. Add this to your Main Camera
    /// 2. Create empty GameObjects with BoxCollider2D covering each section
    /// 3. Drag all those colliders into the Containment Zones list
    /// 4. The camera will stay inside whichever zone the player is in
    /// </summary>
    public class CameraContainment : MonoBehaviour
    {
        [Header("Containment Zones")]
        [Tooltip("All the BoxCollider2D zones the camera can be inside")]
        [SerializeField] private List<BoxCollider2D> _containmentZones = new List<BoxCollider2D>();

        [Header("Player Reference")]
        [Tooltip("Auto-finds if not set")]
        [SerializeField] private Transform _player;

        [Header("Settings")]
        [Tooltip("How smoothly camera transitions between zones")]
        [SerializeField] private float _transitionSpeed = 8f;
        
        [Tooltip("Padding from zone edges")]
        [SerializeField] private float _edgePadding = 0f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        private Camera _camera;
        private float _halfHeight;
        private float _halfWidth;
        private BoxCollider2D _currentZone;
        private Bounds _targetBounds;
        private Bounds _currentBounds;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            CalculateCameraSize();

            // Find player if not assigned
            if (_player == null)
            {
                var playerController = FindFirstObjectByType<UltimatePlayerController>();
                if (playerController != null)
                {
                    _player = playerController.transform;
                }
            }

            // Initialize bounds to first zone or default
            if (_containmentZones.Count > 0 && _containmentZones[0] != null)
            {
                _currentBounds = _targetBounds = _containmentZones[0].bounds;
            }
        }

        private void LateUpdate()
        {
            if (_player == null || _containmentZones.Count == 0) return;

            CalculateCameraSize();
            UpdateCurrentZone();
            SmoothTransitionBounds();
            ClampCameraToBounds();
        }

        private void CalculateCameraSize()
        {
            _halfHeight = _camera.orthographicSize;
            _halfWidth = _halfHeight * _camera.aspect;
        }

        private void UpdateCurrentZone()
        {
            // Find which zone the player is in
            Vector2 playerPos = _player.position;
            
            foreach (var zone in _containmentZones)
            {
                if (zone == null) continue;

                if (zone.bounds.Contains(playerPos))
                {
                    if (_currentZone != zone)
                    {
                        _currentZone = zone;
                        _targetBounds = zone.bounds;
                        
                        if (_showDebugMessages)
                            Debug.Log($"CameraContainment: Player entered {zone.name}");
                    }
                    return;
                }
            }

            // Player not in any zone - find nearest
            float nearestDist = float.MaxValue;
            BoxCollider2D nearestZone = null;

            foreach (var zone in _containmentZones)
            {
                if (zone == null) continue;

                float dist = Vector2.Distance(playerPos, zone.bounds.center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestZone = zone;
                }
            }

            if (nearestZone != null && _currentZone != nearestZone)
            {
                _currentZone = nearestZone;
                _targetBounds = nearestZone.bounds;
            }
        }

        private void SmoothTransitionBounds()
        {
            // Smoothly interpolate bounds
            Vector3 center = Vector3.Lerp(_currentBounds.center, _targetBounds.center, _transitionSpeed * Time.deltaTime);
            Vector3 size = Vector3.Lerp(_currentBounds.size, _targetBounds.size, _transitionSpeed * Time.deltaTime);
            _currentBounds = new Bounds(center, size);
        }

        private void ClampCameraToBounds()
        {
            Vector3 pos = transform.position;

            // Apply padding
            float effectiveHalfWidth = _halfWidth + _edgePadding;
            float effectiveHalfHeight = _halfHeight + _edgePadding;

            // Calculate where camera center can be
            float minX = _currentBounds.min.x + effectiveHalfWidth;
            float maxX = _currentBounds.max.x - effectiveHalfWidth;
            float minY = _currentBounds.min.y + effectiveHalfHeight;
            float maxY = _currentBounds.max.y - effectiveHalfHeight;

            // Handle case where zone is smaller than camera
            if (maxX < minX)
            {
                pos.x = _currentBounds.center.x;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }

            if (maxY < minY)
            {
                pos.y = _currentBounds.center.y;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }

            transform.position = pos;
        }

        /// <summary>
        /// Add a zone at runtime
        /// </summary>
        public void AddZone(BoxCollider2D zone)
        {
            if (!_containmentZones.Contains(zone))
            {
                _containmentZones.Add(zone);
            }
        }

        /// <summary>
        /// Remove a zone at runtime
        /// </summary>
        public void RemoveZone(BoxCollider2D zone)
        {
            _containmentZones.Remove(zone);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            // Draw all zones
            foreach (var zone in _containmentZones)
            {
                if (zone == null) continue;

                bool isCurrentZone = zone == _currentZone;
                
                // Fill
                Gizmos.color = isCurrentZone 
                    ? new Color(0f, 1f, 0f, 0.15f) 
                    : new Color(0f, 1f, 1f, 0.1f);
                Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);
                
                // Outline
                Gizmos.color = isCurrentZone 
                    ? Color.green 
                    : Color.cyan;
                Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
            }

            // Draw camera view
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                Gizmos.color = Color.yellow;
                float h = _camera.orthographicSize;
                float w = h * _camera.aspect;
                Gizmos.DrawWireCube(transform.position, new Vector3(w * 2f, h * 2f, 0f));
            }
        }
    }
}