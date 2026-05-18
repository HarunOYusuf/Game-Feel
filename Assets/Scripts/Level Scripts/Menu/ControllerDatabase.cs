using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects connected controllers and provides correct button mappings and display names.
/// 
/// Supported Controllers:
/// - PlayStation 4/5 (DualShock 4, DualSense)
/// - Xbox (Xbox One, Xbox Series, 360)
/// - Nintendo Switch Pro Controller
/// - Generic/Unknown controllers
/// 
/// Usage:
///   ControllerDatabase.Instance.GetButtonName(KeyCode.JoystickButton0) → "X" (PS) or "A" (Xbox)
///   ControllerDatabase.Instance.CurrentControllerType → ControllerType.PlayStation
/// </summary>
public class ControllerDatabase : MonoBehaviour
{
    public static ControllerDatabase Instance { get; private set; }

    /// <summary>
    /// Types of controllers we can detect
    /// </summary>
    public enum ControllerType
    {
        None,           // No controller / Keyboard only
        PlayStation,    // PS4, PS5, DualShock, DualSense
        Xbox,           // Xbox One, Xbox Series, Xbox 360
        Switch,         // Nintendo Switch Pro Controller
        Generic         // Unknown / Generic USB controller
    }

    /// <summary>
    /// Button mapping profile for a controller type
    /// </summary>
    [System.Serializable]
    public class ControllerProfile
    {
        public ControllerType Type;
        public string[] IdentifierStrings; // Strings to look for in joystick name
        
        // Button indices (which JoystickButton number maps to which action)
        public int FaceButtonBottom;  // A / X / B
        public int FaceButtonRight;   // B / Circle / A
        public int FaceButtonLeft;    // X / Square / Y
        public int FaceButtonTop;     // Y / Triangle / X
        public int LeftBumper;        // LB / L1 / L
        public int RightBumper;       // RB / R1 / R
        public int LeftTrigger;       // LT / L2 / ZL (if button, not axis)
        public int RightTrigger;      // RT / R2 / ZR (if button, not axis)
        public int Select;            // View / Share / -
        public int Start;             // Menu / Options / +
        public int LeftStickClick;    // L3
        public int RightStickClick;   // R3

        // Display names for each button index
        public string[] ButtonNames;
    }

    [Header("Detection Settings")]
    [Tooltip("How often to check for controller changes (seconds)")]
    [SerializeField] private float _detectionInterval = 1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugMessages = true;

    // Current state
    private ControllerType _currentType = ControllerType.None;
    private ControllerProfile _currentProfile;
    private string _currentControllerName = "";
    private float _lastDetectionTime;

    // All known controller profiles
    private List<ControllerProfile> _profiles;

    // Events
    public event Action<ControllerType> OnControllerChanged;

    // Public accessors
    public ControllerType CurrentControllerType => _currentType;
    public string CurrentControllerName => _currentControllerName;
    public ControllerProfile CurrentProfile => _currentProfile;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitialiseProfiles();
        DetectController();
    }

    private void Update()
    {
        // Periodically check for controller changes
        if (Time.unscaledTime - _lastDetectionTime > _detectionInterval)
        {
            _lastDetectionTime = Time.unscaledTime;
            DetectController();
        }
    }

    /// <summary>
    /// Set up all known controller profiles
    /// </summary>
    private void InitialiseProfiles()
    {
        _profiles = new List<ControllerProfile>();

        // PlayStation (PS4 DualShock 4, PS5 DualSense)
        _profiles.Add(new ControllerProfile
        {
            Type = ControllerType.PlayStation,
            IdentifierStrings = new[] { "playstation", "dualsense", "dualshock", "ps4", "ps5", "sony", "wireless controller" },
            FaceButtonBottom = 1,   // X
            FaceButtonRight = 2,    // Circle
            FaceButtonLeft = 0,     // Square
            FaceButtonTop = 3,      // Triangle
            LeftBumper = 4,         // L1
            RightBumper = 5,        // R1
            LeftTrigger = 6,        // L2
            RightTrigger = 7,       // R2
            Select = 8,             // Share
            Start = 9,              // Options
            LeftStickClick = 10,    // L3
            RightStickClick = 11,   // R3
            ButtonNames = new[] 
            { 
                "SQUARE",    // 0
                "X",         // 1
                "CIRCLE",    // 2
                "TRIANGLE",  // 3
                "L1",        // 4
                "R1",        // 5
                "L2",        // 6
                "R2",        // 7
                "SHARE",     // 8
                "OPTIONS",   // 9
                "L3",        // 10
                "R3",        // 11
                "PS",        // 12
                "TOUCHPAD",  // 13
                "D-PAD UP",  // 14 (some drivers)
                "D-PAD DOWN",// 15
                "D-PAD LEFT",// 16
                "D-PAD RIGHT"// 17
            }
        });

        // Xbox (Xbox One, Xbox Series, Xbox 360)
        _profiles.Add(new ControllerProfile
        {
            Type = ControllerType.Xbox,
            IdentifierStrings = new[] { "xbox", "xinput", "microsoft", "x-box" },
            FaceButtonBottom = 0,   // A
            FaceButtonRight = 1,    // B
            FaceButtonLeft = 2,     // X
            FaceButtonTop = 3,      // Y
            LeftBumper = 4,         // LB
            RightBumper = 5,        // RB
            LeftTrigger = -1,       // LT is usually axis, not button
            RightTrigger = -1,      // RT is usually axis, not button
            Select = 6,             // View (Back)
            Start = 7,              // Menu (Start)
            LeftStickClick = 8,     // LS
            RightStickClick = 9,    // RS
            ButtonNames = new[] 
            { 
                "A",         // 0
                "B",         // 1
                "X",         // 2
                "Y",         // 3
                "LB",        // 4
                "RB",        // 5
                "VIEW",      // 6
                "MENU",      // 7
                "LS",        // 8
                "RS",        // 9
                "D-PAD UP",  // 10
                "D-PAD DOWN",// 11
                "D-PAD LEFT",// 12
                "D-PAD RIGHT"// 13
            }
        });

        // Nintendo Switch Pro Controller
        _profiles.Add(new ControllerProfile
        {
            Type = ControllerType.Switch,
            IdentifierStrings = new[] { "switch", "nintendo", "pro controller" },
            FaceButtonBottom = 0,   // B
            FaceButtonRight = 1,    // A
            FaceButtonLeft = 2,     // Y
            FaceButtonTop = 3,      // X
            LeftBumper = 4,         // L
            RightBumper = 5,        // R
            LeftTrigger = 6,        // ZL
            RightTrigger = 7,       // ZR
            Select = 8,             // -
            Start = 9,              // +
            LeftStickClick = 10,    // L Stick
            RightStickClick = 11,   // R Stick
            ButtonNames = new[] 
            { 
                "B",         // 0
                "A",         // 1
                "Y",         // 2
                "X",         // 3
                "L",         // 4
                "R",         // 5
                "ZL",        // 6
                "ZR",        // 7
                "-",         // 8
                "+",         // 9
                "L STICK",   // 10
                "R STICK",   // 11
                "HOME",      // 12
                "CAPTURE"    // 13
            }
        });

        // Generic / MSI / Unknown controllers
        _profiles.Add(new ControllerProfile
        {
            Type = ControllerType.Generic,
            IdentifierStrings = new string[0], // Fallback - matches anything
            FaceButtonBottom = 0,
            FaceButtonRight = 1,
            FaceButtonLeft = 2,
            FaceButtonTop = 3,
            LeftBumper = 4,
            RightBumper = 5,
            LeftTrigger = 6,
            RightTrigger = 7,
            Select = 8,
            Start = 9,
            LeftStickClick = 10,
            RightStickClick = 11,
            ButtonNames = new string[]
            { 
                "BUTTON 1",  // 0
                "BUTTON 2",  // 1
                "BUTTON 3",  // 2
                "BUTTON 4",  // 3
                "L1",        // 4
                "R1",        // 5
                "L2",        // 6
                "R2",        // 7
                "SELECT",    // 8
                "START",     // 9
                "L3",        // 10
                "R3",        // 11
                "BUTTON 13", // 12
                "BUTTON 14", // 13
                "BUTTON 15", // 14
                "BUTTON 16", // 15
                "BUTTON 17", // 16
                "BUTTON 18", // 17
                "BUTTON 19", // 18
                "BUTTON 20"  // 19
            }
        });
    }

    /// <summary>
    /// Detect what controller is currently connected
    /// </summary>
    public void DetectController()
    {
        string[] joysticks = Input.GetJoystickNames();
        
        string detectedName = "";
        ControllerType detectedType = ControllerType.None;
        ControllerProfile detectedProfile = null;

        // Find first connected joystick
        foreach (string name in joysticks)
        {
            if (string.IsNullOrEmpty(name)) continue;

            detectedName = name;
            string lowerName = name.ToLower();

            // Try to match against known profiles
            foreach (var profile in _profiles)
            {
                foreach (string identifier in profile.IdentifierStrings)
                {
                    if (lowerName.Contains(identifier.ToLower()))
                    {
                        detectedType = profile.Type;
                        detectedProfile = profile;
                        break;
                    }
                }
                if (detectedProfile != null) break;
            }

            // If no match found, use generic profile
            if (detectedProfile == null)
            {
                detectedType = ControllerType.Generic;
                detectedProfile = _profiles.Find(p => p.Type == ControllerType.Generic);
            }

            break; // Use first connected controller
        }

        // Check if controller changed
        if (detectedType != _currentType || detectedName != _currentControllerName)
        {
            ControllerType previousType = _currentType;
            
            _currentType = detectedType;
            _currentProfile = detectedProfile;
            _currentControllerName = detectedName;

            if (_showDebugMessages)
            {
                if (_currentType == ControllerType.None)
                {
                    Debug.Log("ControllerDatabase: No controller detected (keyboard mode)");
                }
                else
                {
                    Debug.Log($"ControllerDatabase: Detected {_currentType} controller - '{_currentControllerName}'");
                }
            }

            // Notify listeners
            OnControllerChanged?.Invoke(_currentType);
        }
    }

    /// <summary>
    /// Get the display name for a joystick button based on current controller
    /// </summary>
    public string GetButtonName(KeyCode joystickButton)
    {
        if (_currentProfile == null || joystickButton == KeyCode.None)
        {
            return "???";
        }

        int buttonIndex = (int)joystickButton - (int)KeyCode.JoystickButton0;
        
        if (buttonIndex >= 0 && buttonIndex < _currentProfile.ButtonNames.Length)
        {
            return _currentProfile.ButtonNames[buttonIndex];
        }

        return $"BUTTON {buttonIndex + 1}";
    }

    /// <summary>
    /// Get the display name for a joystick button index (0-19)
    /// </summary>
    public string GetButtonName(int buttonIndex)
    {
        if (_currentProfile == null)
        {
            return $"BUTTON {buttonIndex + 1}";
        }

        if (buttonIndex >= 0 && buttonIndex < _currentProfile.ButtonNames.Length)
        {
            return _currentProfile.ButtonNames[buttonIndex];
        }

        return $"BUTTON {buttonIndex + 1}";
    }

    /// <summary>
    /// Get the KeyCode for the bottom face button (A on Xbox, X on PlayStation)
    /// </summary>
    public KeyCode GetConfirmButton()
    {
        if (_currentProfile == null) return KeyCode.JoystickButton0;
        return (KeyCode)((int)KeyCode.JoystickButton0 + _currentProfile.FaceButtonBottom);
    }

    /// <summary>
    /// Get the KeyCode for the right face button (B on Xbox, Circle on PlayStation)
    /// </summary>
    public KeyCode GetBackButton()
    {
        if (_currentProfile == null) return KeyCode.JoystickButton1;
        return (KeyCode)((int)KeyCode.JoystickButton0 + _currentProfile.FaceButtonRight);
    }

    /// <summary>
    /// Get the KeyCode for the start/options/menu button
    /// </summary>
    public KeyCode GetStartButton()
    {
        if (_currentProfile == null) return KeyCode.JoystickButton9;
        return (KeyCode)((int)KeyCode.JoystickButton0 + _currentProfile.Start);
    }

    /// <summary>
    /// Check if any controller is connected
    /// </summary>
    public bool IsControllerConnected()
    {
        return _currentType != ControllerType.None;
    }

    /// <summary>
    /// Force re-detection of controller
    /// </summary>
    public void RefreshController()
    {
        _currentType = ControllerType.None;
        _currentControllerName = "";
        DetectController();
    }
}