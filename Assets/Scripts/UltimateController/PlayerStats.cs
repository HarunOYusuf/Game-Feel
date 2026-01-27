using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Player movement stats - tweak these values in the inspector for perfect game feel.
    /// Create via: Right-click in Project > Create > Ultimate Controller > Player Stats
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Ultimate Controller/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("=== LAYERS ===")]
        [Tooltip("Layer mask for ground detection")]
        public LayerMask GroundLayer = 1;
        
        [Header("=== INPUT ===")]
        [Tooltip("Snap input to -1, 0, or 1 for digital feel")]
        public bool SnapInput = true;
        
        [Tooltip("Horizontal input deadzone")]
        [Range(0.01f, 0.99f)] public float HorizontalDeadZone = 0.1f;
        
        [Tooltip("Vertical input deadzone")]
        [Range(0.01f, 0.99f)] public float VerticalDeadZone = 0.1f;

        [Header("=== HORIZONTAL MOVEMENT ===")]
        [Tooltip("Maximum horizontal speed")]
        [Range(1f, 50f)] public float MaxSpeed = 14f;
        
        [Tooltip("Ground acceleration")]
        [Range(1f, 200f)] public float Acceleration = 120f;
        
        [Tooltip("Ground deceleration (friction)")]
        [Range(1f, 200f)] public float GroundDeceleration = 60f;
        
        [Tooltip("Air acceleration (typically less than ground)")]
        [Range(1f, 200f)] public float AirAcceleration = 90f;
        
        [Tooltip("Air deceleration")]
        [Range(1f, 200f)] public float AirDeceleration = 30f;

        [Header("=== JUMPING ===")]
        [Tooltip("Initial jump velocity")]
        [Range(1f, 50f)] public float JumpPower = 24f;
        
        [Tooltip("Time after pressing jump that it will still trigger when landing (buffer)")]
        [Range(0f, 0.5f)] public float JumpBuffer = 0.15f;
        
        [Tooltip("Time after leaving ground that you can still jump (coyote time)")]
        [Range(0f, 0.5f)] public float CoyoteTime = 0.15f;
        
        [Tooltip("Gravity multiplier when jump is released early (variable jump height)")]
        [Range(1f, 10f)] public float JumpCutMultiplier = 3f;

        [Header("=== APEX MODIFIER ===")]
        [Tooltip("Enable apex modifier for floatier jump peak")]
        public bool UseApexModifier = true;
        
        [Tooltip("Vertical velocity threshold to consider 'at apex'")]
        [Range(1f, 20f)] public float ApexThreshold = 8f;
        
        [Tooltip("Gravity multiplier at apex (lower = floatier)")]
        [Range(0.1f, 1f)] public float ApexGravityMultiplier = 0.5f;
        
        [Tooltip("Speed bonus at apex (0 = none, 1 = double speed)")]
        [Range(0f, 1f)] public float ApexSpeedBonus = 0.1f;
        
        [Tooltip("Acceleration bonus at apex")]
        [Range(0f, 2f)] public float ApexAccelerationBonus = 0.5f;

        [Header("=== GRAVITY & FALLING ===")]
        [Tooltip("Downward acceleration when falling")]
        [Range(1f, 200f)] public float FallAcceleration = 95f;
        
        [Tooltip("Maximum fall speed (terminal velocity)")]
        [Range(1f, 100f)] public float MaxFallSpeed = 40f;
        
        [Tooltip("Small downward force applied when grounded")]
        [Range(-20f, 0f)] public float GroundingForce = -1.5f;

        [Header("=== COLLISION ===")]
        [Tooltip("Distance to check for ground below player")]
        [Range(0.01f, 0.5f)] public float GrounderDistance = 0.05f;

        [Header("=== EDGE DETECTION ===")]
        [Tooltip("Enable edge detection")]
        public bool UseEdgeDetection = true;
        
        [Tooltip("Offset from collider edge for edge rays")]
        [Range(0f, 0.5f)] public float EdgeDetectionOffset = 0.1f;
        
        [Tooltip("Auto-correct position when standing on edge")]
        public bool EdgeCorrection = false;
        
        [Tooltip("Strength of edge correction nudge")]
        [Range(0f, 50f)] public float EdgeCorrectionStrength = 10f;

        [Header("=== DASH (Optional) ===")]
        [Tooltip("Enable dash ability")]
        public bool AllowDash = true;
        
        [Tooltip("Dash speed")]
        [Range(10f, 100f)] public float DashSpeed = 30f;
        
        [Tooltip("Duration of dash in seconds")]
        [Range(0.05f, 0.5f)] public float DashDuration = 0.15f;
        
        [Tooltip("Speed retained after dash ends")]
        [Range(0f, 30f)] public float DashEndSpeed = 10f;

        [Header("=== WALL SLIDE ===")]
        [Tooltip("Enable wall slide and wall jump")]
        public bool AllowWallSlide = true;
        
        [Tooltip("Layer mask for walls (can be same as ground)")]
        public LayerMask WallLayer = 1;
        
        [Tooltip("Distance to check for walls")]
        [Range(0.01f, 0.5f)] public float WallCheckDistance = 0.1f;
        
        [Tooltip("Speed of sliding down the wall")]
        [Range(0f, 20f)] public float WallSlideSpeed = 5f;
        
        [Tooltip("How fast you accelerate to wall slide speed")]
        [Range(10f, 200f)] public float WallSlideAcceleration = 50f;
        
        [Header("=== WALL JUMP ===")]
        [Tooltip("Horizontal force when jumping off wall")]
        [Range(5f, 40f)] public float WallJumpHorizontalPower = 16f;
        
        [Tooltip("Vertical force when jumping off wall")]
        [Range(5f, 40f)] public float WallJumpVerticalPower = 22f;
        
        [Tooltip("Coyote time for wall jump (time after leaving wall you can still jump)")]
        [Range(0f, 0.3f)] public float WallJumpCoyoteTime = 0.1f;

        [Header("=== PRESETS ===")]
        [Tooltip("Quick preset buttons appear in inspector")]
        public bool ShowPresetButtons = true;

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(MovementPreset preset)
        {
            switch (preset)
            {
                case MovementPreset.Platformer:
                    ApplyPlatformerPreset();
                    break;
                case MovementPreset.Floaty:
                    ApplyFloatyPreset();
                    break;
                case MovementPreset.Tight:
                    ApplyTightPreset();
                    break;
                case MovementPreset.Celeste:
                    ApplyCelestePreset();
                    break;
            }
        }

        private void ApplyPlatformerPreset()
        {
            MaxSpeed = 14f;
            Acceleration = 120f;
            GroundDeceleration = 60f;
            AirAcceleration = 90f;
            AirDeceleration = 30f;
            JumpPower = 24f;
            JumpBuffer = 0.15f;
            CoyoteTime = 0.15f;
            JumpCutMultiplier = 3f;
            UseApexModifier = true;
            ApexThreshold = 8f;
            ApexGravityMultiplier = 0.5f;
            FallAcceleration = 95f;
            MaxFallSpeed = 40f;
        }

        private void ApplyFloatyPreset()
        {
            MaxSpeed = 10f;
            Acceleration = 80f;
            GroundDeceleration = 40f;
            AirAcceleration = 60f;
            AirDeceleration = 20f;
            JumpPower = 20f;
            JumpBuffer = 0.2f;
            CoyoteTime = 0.2f;
            JumpCutMultiplier = 2f;
            UseApexModifier = true;
            ApexThreshold = 12f;
            ApexGravityMultiplier = 0.3f;
            FallAcceleration = 50f;
            MaxFallSpeed = 25f;
        }

        private void ApplyTightPreset()
        {
            MaxSpeed = 16f;
            Acceleration = 200f;
            GroundDeceleration = 200f;
            AirAcceleration = 150f;
            AirDeceleration = 100f;
            JumpPower = 26f;
            JumpBuffer = 0.1f;
            CoyoteTime = 0.1f;
            JumpCutMultiplier = 4f;
            UseApexModifier = false;
            FallAcceleration = 120f;
            MaxFallSpeed = 50f;
        }

        private void ApplyCelestePreset()
        {
            MaxSpeed = 13f;
            Acceleration = 150f;
            GroundDeceleration = 80f;
            AirAcceleration = 130f;
            AirDeceleration = 60f;
            JumpPower = 22f;
            JumpBuffer = 0.15f;
            CoyoteTime = 0.12f;
            JumpCutMultiplier = 3.5f;
            UseApexModifier = true;
            ApexThreshold = 6f;
            ApexGravityMultiplier = 0.4f;
            ApexSpeedBonus = 0.15f;
            ApexAccelerationBonus = 0.8f;
            FallAcceleration = 100f;
            MaxFallSpeed = 35f;
            AllowDash = true;
            DashSpeed = 35f;
            DashDuration = 0.12f;
        }
    }

    public enum MovementPreset
    {
        Platformer,  // Balanced, Mario-like
        Floaty,      // Slow, floaty jumps
        Tight,       // Responsive, snappy
        Celeste      // Celeste-inspired with dash
    }
}