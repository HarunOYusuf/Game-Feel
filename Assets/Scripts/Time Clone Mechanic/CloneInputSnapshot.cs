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
        
        // Starting state (only used for first frame)
        public Vector2 StartPosition;
        public int StartFacingDirection;

        public CloneInputSnapshot(
            float timestamp,
            float horizontalInput,
            bool jumpPressed,
            bool jumpHeld,
            bool dashPressed,
            Vector2 position,
            int facingDirection)
        {
            Timestamp = timestamp;
            HorizontalInput = horizontalInput;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            DashPressed = dashPressed;
            StartPosition = position;
            StartFacingDirection = facingDirection;
        }
    }
}