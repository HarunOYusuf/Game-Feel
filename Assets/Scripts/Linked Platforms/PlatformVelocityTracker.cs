using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Tracks a Kinematic platform's velocity by computing position deltas between
    /// FixedUpdate calls. This is needed because Unity's Kinematic Rigidbody2D
    /// does NOT reliably populate linearVelocity when moved via MovePosition.
    /// 
    /// Other scripts (UltimatePlayerController, CloneMovement) read CurrentVelocity
    /// to inherit motion when standing on the platform.
    /// 
    /// Auto-added by LinkedPlatformGroup. Can also be added manually to any
    /// Kinematic platform that needs to carry riders.
    /// </summary>
    public class PlatformVelocityTracker : MonoBehaviour
    {
        private Vector2 _previousPosition;
        private Vector2 _currentVelocity;
        private bool _initialised;

        /// <summary>
        /// The platform's velocity for this physics step, computed from position delta.
        /// Read this from external scripts for ground velocity inheritance.
        /// </summary>
        public Vector2 CurrentVelocity => _currentVelocity;

        private void Start()
        {
            _previousPosition = transform.position;
            _initialised = true;
        }

        private void FixedUpdate()
        {
            if (!_initialised)
            {
                _previousPosition = transform.position;
                _initialised = true;
                return;
            }

            Vector2 currentPosition = transform.position;
            
            // velocity = delta / time
            _currentVelocity = (currentPosition - _previousPosition) / Time.fixedDeltaTime;
            _previousPosition = currentPosition;
        }

        // Public method for external systems to force-reset tracking (e.g. after teleport)
        public void ResetTracking()
        {
            _previousPosition = transform.position;
            _currentVelocity = Vector2.zero;
        }
    }
}