using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Attach to a platform parent (the same GameObject as the platform's Rigidbody2D).
    /// Tracks which Rigidbody2Ds (player, clone) are currently standing on this platform
    /// via collision events bubbling up from child colliders.
    /// 
    /// Used by LinkedPlatformGroup to carry riders along when the platform moves.
    /// 
    /// Setup:
    /// 1. Attach to the platform parent GameObject
    /// 2. Platform parent must have a Rigidbody2D (Kinematic)
    /// 3. Child colliders must be SOLID (Is Trigger = OFF) - they're the ground
    /// 4. Children with colliders will bubble OnCollision events up to this parent
    /// </summary>
    public class PlatformRider : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Only detect bodies hitting from above (standing on top)")]
        [SerializeField] private bool _onlyDetectFromAbove = true;
        
        [Tooltip("Dot product threshold for 'from above' detection (1 = directly above, 0 = side)")]
        [Range(0f, 1f)]
        [SerializeField] private float _aboveThreshold = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Currently active riders (Rigidbody2D references)
        private readonly HashSet<Rigidbody2D> _riders = new HashSet<Rigidbody2D>();

        /// <summary>
        /// All Rigidbody2Ds currently standing on this platform.
        /// </summary>
        public IReadOnlyCollection<Rigidbody2D> Riders => _riders;

        /// <summary>
        /// Number of bodies currently riding this platform.
        /// </summary>
        public int RiderCount => _riders.Count;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Only track bodies that are valid riders (player or clone)
            if (!IsValidRider(collision.rigidbody)) return;

            // Optional: check that the rider is on top of the platform
            if (_onlyDetectFromAbove && !IsContactFromAbove(collision))
            {
                return;
            }

            if (_riders.Add(collision.rigidbody))
            {
                if (_showDebugMessages)
                {
                    Debug.Log($"PlatformRider [{gameObject.name}]: {collision.gameObject.name} mounted (riders: {_riders.Count})");
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Re-validate riders every frame in case they slid off the top onto the side
            if (!IsValidRider(collision.rigidbody)) return;

            if (_onlyDetectFromAbove)
            {
                if (IsContactFromAbove(collision))
                {
                    _riders.Add(collision.rigidbody);
                }
                else
                {
                    _riders.Remove(collision.rigidbody);
                }
            }
            else
            {
                _riders.Add(collision.rigidbody);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.rigidbody == null) return;

            if (_riders.Remove(collision.rigidbody))
            {
                if (_showDebugMessages)
                {
                    Debug.Log($"PlatformRider [{gameObject.name}]: {collision.gameObject.name} dismounted (riders: {_riders.Count})");
                }
            }
        }

        /// <summary>
        /// Validate that the colliding body is a player or clone
        /// </summary>
        private bool IsValidRider(Rigidbody2D body)
        {
            if (body == null) return false;

            // Check for player or clone components
            if (body.GetComponent<UltimatePlayerController>() != null) return true;
            if (body.GetComponent<CloneMovement>() != null) return true;
            if (body.GetComponent<TimeClone>() != null) return true;

            return false;
        }

        /// <summary>
        /// Check if the contact is from above (rider on top of platform)
        /// by checking the average contact normal direction.
        /// </summary>
        private bool IsContactFromAbove(Collision2D collision)
        {
            if (collision.contactCount == 0) return false;

            // Average the contact normals
            Vector2 averageNormal = Vector2.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                averageNormal += collision.GetContact(i).normal;
            }
            averageNormal /= collision.contactCount;

            // Contact normal points FROM the platform surface TOWARDS the rider
            // If the rider is on top of the platform, the normal points UP (positive Y)
            return averageNormal.y >= _aboveThreshold;
        }

        /// <summary>
        /// Manually clear riders (e.g. when platform teleports or resets)
        /// </summary>
        public void ClearRiders()
        {
            _riders.Clear();
        }
    }
}