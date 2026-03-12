using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A zone that disables specific player abilities when inside.
    /// Inspired by The Swapper's light mechanics.
    /// 
    /// Setup:
    /// 1. Create a "lamp" parent object with your lamp sprite
    /// 2. Create a child object for the light zone
    /// 3. Add BoxCollider2D (or CircleCollider2D), set to "Is Trigger"
    /// 4. Add this script and select ZoneType
    /// 5. Add a SpriteRenderer with a gradient/glow sprite for the light effect
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ColourZone : MonoBehaviour
    {
        [Header("Zone Type")]
        [Tooltip("What ability does this zone disable?")]
        [SerializeField] private ZoneType _zoneType = ZoneType.NoDash;

        [Header("Visuals")]
        [Tooltip("Automatically tint the sprite based on zone type")]
        [SerializeField] private bool _autoTintSprite = true;
        
        [SerializeField] private SpriteRenderer _zoneSprite;

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Zone colours
        private static readonly Color RedZoneColour = new Color(1f, 0.3f, 0.3f, 0.35f);    // No Dash
        private static readonly Color BlueZoneColour = new Color(0.3f, 0.3f, 1f, 0.35f);   // No Record
        private static readonly Color PurpleZoneColour = new Color(0.7f, 0.2f, 0.9f, 0.35f); // Both

        private void Start()
        {
            // Ensure collider is a trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"ColourZone '{gameObject.name}': Collider set to trigger automatically.", this);
            }

            // Auto-find sprite if not assigned
            if (_zoneSprite == null)
            {
                _zoneSprite = GetComponent<SpriteRenderer>();
            }

            // Apply colour
            if (_autoTintSprite && _zoneSprite != null)
            {
                ApplyZoneColour();
            }
        }

        private void ApplyZoneColour()
        {
            _zoneSprite.color = _zoneType switch
            {
                ZoneType.NoDash => RedZoneColour,
                ZoneType.NoRecord => BlueZoneColour,
                ZoneType.NoDashAndNoRecord => PurpleZoneColour,
                _ => Color.white
            };
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check for player controller
            var controller = other.GetComponent<UltimatePlayerController>();
            if (controller != null)
            {
                ApplyControllerEffect(controller, true);
            }

            // Check for clone recorder
            var recorder = other.GetComponent<TimeCloneRecorder>();
            if (recorder != null)
            {
                ApplyRecorderEffect(recorder, true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Check for player controller
            var controller = other.GetComponent<UltimatePlayerController>();
            if (controller != null)
            {
                ApplyControllerEffect(controller, false);
            }

            // Check for clone recorder
            var recorder = other.GetComponent<TimeCloneRecorder>();
            if (recorder != null)
            {
                ApplyRecorderEffect(recorder, false);
            }
        }

        private void ApplyControllerEffect(UltimatePlayerController controller, bool entering)
        {
            // Disable dash for Red and Purple zones
            if (_zoneType == ZoneType.NoDash || _zoneType == ZoneType.NoDashAndNoRecord)
            {
                controller.SetDashEnabled(!entering);
                
                if (_showDebugMessages)
                    Debug.Log($"Dash {(entering ? "DISABLED" : "ENABLED")}");
            }
        }

        private void ApplyRecorderEffect(TimeCloneRecorder recorder, bool entering)
        {
            // Disable recording for Blue and Purple zones
            if (_zoneType == ZoneType.NoRecord || _zoneType == ZoneType.NoDashAndNoRecord)
            {
                recorder.SetRecordingEnabled(!entering);
                
                if (_showDebugMessages)
                    Debug.Log($"Clone Recording {(entering ? "DISABLED" : "ENABLED")}");
            }
        }

        // Update colours in editor when zone type changes
        private void OnValidate()
        {
            if (_zoneSprite == null)
                _zoneSprite = GetComponent<SpriteRenderer>();
                
            if (_autoTintSprite && _zoneSprite != null)
            {
                ApplyZoneColour();
            }
        }

        // Draw zone in Scene view
        private void OnDrawGizmos()
        {
            Gizmos.color = _zoneType switch
            {
                ZoneType.NoDash => new Color(1f, 0.3f, 0.3f, 0.3f),
                ZoneType.NoRecord => new Color(0.3f, 0.3f, 1f, 0.3f),
                ZoneType.NoDashAndNoRecord => new Color(0.7f, 0.2f, 0.9f, 0.3f),
                _ => new Color(1f, 1f, 1f, 0.3f)
            };

            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }
    }

    public enum ZoneType
    {
        NoDash,             // Red - Player cannot dash
        NoRecord,           // Blue - Player cannot record/spawn clone
        NoDashAndNoRecord   // Purple - Both abilities disabled
    }
}