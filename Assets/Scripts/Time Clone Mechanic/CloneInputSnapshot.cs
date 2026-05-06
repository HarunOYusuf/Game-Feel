using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Records player INPUT at a moment in time, not position.
    /// This allows the clone to physically respond to the environment.
    /// </summary>
    [System.Serializable]
    public struct CloneInputSnapshot
    {
        public float Timestamp;
        
        // Inputs
        public float HorizontalInput;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool DashPressed;
        public Vector2 DashDirection;
        
        // State (needed for wall sliding/jumping)
        public bool IsGrounded;
        public bool IsWallSliding;
        public int WallDirection;
        
        // Starting state (only used for first frame)
        public Vector2 StartPosition;
        public int StartFacingDirection;

        public CloneInputSnapshot(
            float timestamp,
            float horizontalInput,
            bool jumpPressed,
            bool jumpHeld,
            bool dashPressed,
            Vector2 dashDirection,
            bool isGrounded,
            bool isWallSliding,
            int wallDirection,
            Vector2 position,
            int facingDirection)
        {
            Timestamp = timestamp;
            HorizontalInput = horizontalInput;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            DashPressed = dashPressed;
            DashDirection = dashDirection;
            IsGrounded = isGrounded;
            IsWallSliding = isWallSliding;
            WallDirection = wallDirection;
            StartPosition = position;
            StartFacingDirection = facingDirection;
        }
    }
}