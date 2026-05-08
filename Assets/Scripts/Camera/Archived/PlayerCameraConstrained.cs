using UnityEngine;
using System.Collections.Generic;

namespace UltimateController
{
    /// <summary>
    /// Combined camera script: follows the player with look-ahead AND stays inside zone bounds.
    /// Replaces both PlayerCamera and CameraContainment/CameraBounds.
    /// 
    /// Setup:
    /// 1. Remove PlayerCamera, CameraBounds, CameraContainment, CameraZone from your camera
    /// 2. Add ONLY this script to your Main Camera
    /// 3. Create BoxCollider2D zones covering your level
    /// 4. Drag all zones into the Containment Zones list
    /// </summary>
    public class PlayerCameraConstrained : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Follow Settings")]
        [Tooltip("Base offset from the player")]
        [SerializeField] private Vector3 _offset = new Vector3(0, 2f, -10f);
        
        [Tooltip("How smoothly the camera follows")]
        [SerializeField] private float _followSpeed = 8f;

        [Header("Look-Ahead")]
        [Tooltip("How far ahead of the player to look")]
        [SerializeField] private float _lookAheadDistance = 3f;
        
        [Tooltip("How fast the look-ahead adjusts")]
        [SerializeField] private float _lookAheadSpeed = 5f;

        [Header("Containment Zones")]
        [Tooltip("BoxCollider2D zones the camera must stay inside")]
        [SerializeField] private List<BoxCollider2D> _containmentZones = new List<BoxCollider2D>();
        
        [Tooltip("How smoothly camera transitions between zones")]
        [SerializeField] private float _zoneTransitionSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;

        // Components
        private Camera _camera;
        private UltimatePlayerController _controller;
        
        // Camera size
        private float _halfHeight;
        private float _halfWidth;
        
        // Look-ahead
        private float _currentLookAhead;
        private float _lastFacingDirection = 1f;
        
        // Zone tracking
        private BoxCollider2D _currentZone;
        private Bounds _currentBounds;
        private Bounds _targetBounds;
        private bool _hasBounds;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            
            // Find target if not assigned
            if (_target == null)
            {
                var player = FindFirstObjectByType<UltimatePlayerController>();
                if (player != null)
                {
                    _target = player.transform;
                    _controller = player;
                }
            }
            else
            {
                _controller = _target.GetComponent<UltimatePlayerController>();
            }

            // Initialize camera position
            if (_target != null)
            {
                transform.position = _target.position + _offset;
            }

            // Initialize bounds
            if (_containmentZones.Count > 0 && _containmentZones[0] != null)
            {
                _currentBounds = _targetBounds = _containmentZones[0].bounds;
                _hasBounds = true;
            }

            CalculateCameraSize();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            CalculateCameraSize();
            
            // Step 1: Calculate desired position (follow + look-ahead)
            Vector3 desiredPosition = CalculateDesiredPosition();
            
            // Step 2: Smoothly move towards desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _followSpeed * Time.deltaTime);
            
            // Step 3: Update current zone based on player position
            if (_containmentZones.Count > 0)
            {
                UpdateCurrentZone();
                SmoothTransitionBounds();
                
                // Step 4: Clamp to zone bounds
                smoothedPosition = ClampToBounds(smoothedPosition);
            }
            
            // Apply final position
            transform.position = smoothedPosition;
        }

        private void CalculateCameraSize()
        {
            _halfHeight = _camera.orthographicSize;
            _halfWidth = _halfHeight * _camera.aspect;
        }

        private Vector3 CalculateDesiredPosition()
        {
            // Get facing direction
            float facingDirection = GetFacingDirection();
            
            // Smoothly interpolate look-ahead
            float targetLookAhead = facingDirection * _lookAheadDistance;
            _currentLookAhead = Mathf.Lerp(_currentLookAhead, targetLookAhead, _lookAheadSpeed * Time.deltaTime);
            
            // Calculate target position
            Vector3 targetPos = _target.position + _offset;
            targetPos.x += _currentLookAhead;
            
            return targetPos;
        }

        private float GetFacingDirection()
        {
            if (_controller != null)
            {
                int facing = _controller.FacingDirection;
                if (facing != 0)
                {
                    _lastFacingDirection = facing;
                }
            }
            return _lastFacingDirection;
        }

        private void UpdateCurrentZone()
        {
            if (_target == null) return;
            
            Vector2 playerPos = _target.position;
            
            // Find which zone the player is in
            foreach (var zone in _containmentZones)
            {
                if (zone == null) continue;

                if (zone.bounds.Contains(playerPos))
                {
                    if (_currentZone != zone)
                    {
                        _currentZone = zone;
                        _targetBounds = zone.bounds;
                        _hasBounds = true;
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
                _hasBounds = true;
            }
        }

        private void SmoothTransitionBounds()
        {
            if (!_hasBounds) return;
            
            Vector3 center = Vector3.Lerp(_currentBounds.center, _targetBounds.center, _zoneTransitionSpeed * Time.deltaTime);
            Vector3 size = Vector3.Lerp(_currentBounds.size, _targetBounds.size, _zoneTransitionSpeed * Time.deltaTime);
            _currentBounds = new Bounds(center, size);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!_hasBounds) return position;

            // Calculate where camera center can be
            float minX = _currentBounds.min.x + _halfWidth;
            float maxX = _currentBounds.max.x - _halfWidth;
            float minY = _currentBounds.min.y + _halfHeight;
            float maxY = _currentBounds.max.y - _halfHeight;

            // Handle case where zone is smaller than camera
            if (maxX < minX)
            {
                position.x = _currentBounds.center.x;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (maxY < minY)
            {
                position.y = _currentBounds.center.y;
            }
            else
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            return position;
        }

        /// <summary>
        /// Snap camera instantly to target (for respawns)
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            
            _currentLookAhead = 0f;
            Vector3 pos = _target.position + _offset;
            
            if (_hasBounds)
            {
                pos = ClampToBounds(pos);
            }
            
            transform.position = pos;
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;

            // Draw all zones
            foreach (var zone in _containmentZones)
            {
                if (zone == null) continue;

                bool isCurrentZone = zone == _currentZone;
                
                Gizmos.color = isCurrentZone 
                    ? new Color(0f, 1f, 0f, 0.15f) 
                    : new Color(0f, 1f, 1f, 0.1f);
                Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);
                
                Gizmos.color = isCurrentZone ? Color.green : Color.cyan;
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