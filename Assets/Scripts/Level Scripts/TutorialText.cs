using UnityEngine;
using TMPro;

namespace UltimateController
{
    /// <summary>
    /// In-world tutorial text that appears when the player enters a zone.
    /// Text stays visible while player is in the zone.
    /// 
    /// Setup:
    /// 1. Create empty GameObject for each tutorial zone
    /// 2. Add this script
    /// 3. Set your tutorial messages (supports multiple lines)
    /// 4. Resize the BoxCollider2D to cover the zone area
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TutorialText : MonoBehaviour
    {
        [Header("Text Content")]
        [TextArea(3, 8)]
        [Tooltip("The tutorial message to display (use \\n for new lines)")]
        [SerializeField] private string _message = "Use WASD to move\nPress SPACE to jump";

        [Header("Text Appearance")]
        [SerializeField] private float _fontSize = 5f;
        [SerializeField] private Color _textColour = Color.white;
        [SerializeField] private Color _backgroundColour = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Vector2 _padding = new Vector2(0.8f, 0.5f);

        [Header("Position")]
        [Tooltip("Where the text appears in world space")]
        [SerializeField] private Transform _textPosition;
        
        [Tooltip("If no position set, offset from trigger centre")]
        [SerializeField] private Vector3 _textOffset = new Vector3(0f, 3f, 0f);

        [Header("Behaviour")]
        [Tooltip("Fade in/out animation")]
        [SerializeField] private bool _useFade = true;
        [SerializeField] private float _fadeSpeed = 5f;

        // Components
        private TextMeshPro _textMesh;
        private SpriteRenderer _background;
        private BoxCollider2D _triggerZone;

        // State
        private bool _playerInZone;
        private float _currentAlpha;
        private float _targetAlpha;

        private void Awake()
        {
            SetupComponents();
        }

        private void SetupComponents()
        {
            // Setup trigger zone
            _triggerZone = GetComponent<BoxCollider2D>();
            _triggerZone.isTrigger = true;

            // Determine text position
            Vector3 textWorldPos;
            if (_textPosition != null)
            {
                textWorldPos = _textPosition.position;
            }
            else
            {
                textWorldPos = transform.position + _textOffset;
            }

            // Create text container at world position
            GameObject textContainer = new GameObject("TutorialTextDisplay");
            textContainer.transform.position = textWorldPos;

            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(textContainer.transform);
            bgObj.transform.localPosition = Vector3.zero;
            
            _background = bgObj.AddComponent<SpriteRenderer>();
            _background.sprite = CreateSquareSprite();
            _background.color = _backgroundColour;
            _background.sortingOrder = 99;

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textContainer.transform);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            _textMesh = textObj.AddComponent<TextMeshPro>();
            _textMesh.text = _message;
            _textMesh.fontSize = _fontSize;
            _textMesh.color = _textColour;
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.sortingOrder = 100;
            
            // Set rect transform size for proper text wrapping
            RectTransform rt = _textMesh.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(20f, 10f);

            // Size background to fit text
            Invoke(nameof(UpdateBackgroundSize), 0.1f);

            // Start hidden
            SetAlpha(0f);
            _currentAlpha = 0f;
            _targetAlpha = 0f;
        }

        private void Update()
        {
            // Smoothly fade towards target alpha
            if (!Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                if (_useFade)
                {
                    _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
                }
                else
                {
                    _currentAlpha = _targetAlpha;
                }
                SetAlpha(_currentAlpha);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Get the root player object (might be this collider's object or parent)
            UltimatePlayerController controller = other.GetComponent<UltimatePlayerController>();
            
            // If no controller on this object, check if it's a child of player
            if (controller == null)
            {
                controller = other.GetComponentInParent<UltimatePlayerController>();
            }
            
            // No player found at all
            if (controller == null) return;
            
            // IMPORTANT: Only trigger if collider is on the SAME object as the controller
            // This ignores DashSprite and other child colliders
            if (other.gameObject != controller.gameObject) return;

            _playerInZone = true;
            _targetAlpha = 1f;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Same check as enter
            UltimatePlayerController controller = other.GetComponent<UltimatePlayerController>();
            
            if (controller == null)
            {
                controller = other.GetComponentInParent<UltimatePlayerController>();
            }
            
            if (controller == null) return;
            
            // Only trigger if collider is on the SAME object as the controller
            if (other.gameObject != controller.gameObject) return;

            _playerInZone = false;
            _targetAlpha = 0f;
        }

        private void SetAlpha(float alpha)
        {
            if (_textMesh != null)
            {
                Color c = _textMesh.color;
                c.a = alpha;
                _textMesh.color = c;
            }

            if (_background != null)
            {
                Color c = _background.color;
                c.a = _backgroundColour.a * alpha;
                _background.color = c;
            }
        }

        private void UpdateBackgroundSize()
        {
            if (_textMesh == null || _background == null) return;

            // Force mesh update to get correct bounds
            _textMesh.ForceMeshUpdate();
            
            Vector2 textSize = _textMesh.GetRenderedValues(false);
            Vector3 bgScale = new Vector3(
                textSize.x + _padding.x * 2f,
                textSize.y + _padding.y * 2f,
                1f
            );
            
            _background.transform.localScale = bgScale;
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        // Visualise in editor
        private void OnDrawGizmos()
        {
            // Draw trigger zone
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.offset, col.size);
                
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawWireCube(col.offset, col.size);
            }

            // Draw text position
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 textPos = _textPosition != null ? _textPosition.position : transform.position + _textOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(textPos, 0.3f);
            
            // Draw line from zone to text
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawLine(transform.position, textPos);
        }

        private void OnDrawGizmosSelected()
        {
            // Show message preview in scene
            #if UNITY_EDITOR
            Vector3 textPos = _textPosition != null ? _textPosition.position : transform.position + _textOffset;
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            
            UnityEditor.Handles.Label(textPos, _message, style);
            #endif
        }
    }
}