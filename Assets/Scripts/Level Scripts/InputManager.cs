using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central input manager with rebindable controls.
/// Stores bindings in PlayerPrefs so they persist between sessions.
/// 
/// PS5 Controller Mappings:
/// - Square = JoystickButton0
/// - X = JoystickButton1
/// - Circle = JoystickButton2
/// - Triangle = JoystickButton3
/// - Options = JoystickButton9
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public enum GameAction
    {
        Jump,
        Dash,
        Clone,
        Pause,
        MenuConfirm,
        MenuBack
    }

    [System.Serializable]
    public class InputBinding
    {
        public KeyCode KeyboardKey = KeyCode.None;
        public KeyCode JoystickButton = KeyCode.None;

        public InputBinding() { }

        public InputBinding(KeyCode keyboard, KeyCode joystick)
        {
            KeyboardKey = keyboard;
            JoystickButton = joystick;
        }
    }

    private Dictionary<GameAction, InputBinding> _bindings = new Dictionary<GameAction, InputBinding>();

    public event Action OnBindingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitialiseDefaultBindings();
        LoadBindings();
    }

    /// <summary>
    /// Default bindings for PS5 controller
    /// </summary>
    private void InitialiseDefaultBindings()
    {
        _bindings.Clear();

        // Gameplay actions (rebindable)
        _bindings[GameAction.Jump] = new InputBinding(KeyCode.Space, KeyCode.JoystickButton1);      // X
        _bindings[GameAction.Dash] = new InputBinding(KeyCode.LeftShift, KeyCode.JoystickButton2);  // Circle
        _bindings[GameAction.Clone] = new InputBinding(KeyCode.Q, KeyCode.JoystickButton0);         // Square

        // System actions (not rebindable)
        _bindings[GameAction.Pause] = new InputBinding(KeyCode.Escape, KeyCode.JoystickButton9);    // Options
        _bindings[GameAction.MenuConfirm] = new InputBinding(KeyCode.Return, KeyCode.JoystickButton1); // X
        _bindings[GameAction.MenuBack] = new InputBinding(KeyCode.Escape, KeyCode.JoystickButton2);    // Circle
    }

    #region Input Checking

    public bool GetButtonDown(GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var binding)) return false;

        if (binding.KeyboardKey != KeyCode.None && Input.GetKeyDown(binding.KeyboardKey))
            return true;

        if (binding.JoystickButton != KeyCode.None && Input.GetKeyDown(binding.JoystickButton))
            return true;

        return false;
    }

    public bool GetButton(GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var binding)) return false;

        if (binding.KeyboardKey != KeyCode.None && Input.GetKey(binding.KeyboardKey))
            return true;

        if (binding.JoystickButton != KeyCode.None && Input.GetKey(binding.JoystickButton))
            return true;

        return false;
    }

    public bool GetButtonUp(GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var binding)) return false;

        if (binding.KeyboardKey != KeyCode.None && Input.GetKeyUp(binding.KeyboardKey))
            return true;

        if (binding.JoystickButton != KeyCode.None && Input.GetKeyUp(binding.JoystickButton))
            return true;

        return false;
    }

    public Vector2 GetMovement()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    #endregion

    #region Binding Management

    public InputBinding GetBinding(GameAction action)
    {
        return _bindings.TryGetValue(action, out var binding) ? binding : null;
    }

    /// <summary>
    /// Check if a joystick button is already used by another action
    /// </summary>
    public bool IsJoystickButtonInUse(KeyCode joystickButton, GameAction excludeAction)
    {
        GameAction[] gameplayActions = { GameAction.Jump, GameAction.Dash, GameAction.Clone };
        
        foreach (var action in gameplayActions)
        {
            if (action == excludeAction) continue;
            
            if (_bindings.TryGetValue(action, out var binding))
            {
                if (binding.JoystickButton == joystickButton)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Get which action is using a joystick button
    /// </summary>
    public GameAction? GetActionUsingButton(KeyCode joystickButton)
    {
        GameAction[] gameplayActions = { GameAction.Jump, GameAction.Dash, GameAction.Clone };
        
        foreach (var action in gameplayActions)
        {
            if (_bindings.TryGetValue(action, out var binding))
            {
                if (binding.JoystickButton == joystickButton)
                {
                    return action;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Set a new joystick button binding for an action.
    /// If the button is already used by another action, they will SWAP.
    /// </summary>
    public bool SetJoystickBinding(GameAction action, KeyCode joystickButton)
    {
        if (!_bindings.ContainsKey(action)) return false;

        // Get the button this action currently uses
        KeyCode oldButton = _bindings[action].JoystickButton;

        // Check if the new button is used by another action
        GameAction? otherAction = GetActionUsingButton(joystickButton);
        
        if (otherAction.HasValue && otherAction.Value != action)
        {
            // SWAP: Give the other action our old button
            _bindings[otherAction.Value].JoystickButton = oldButton;
        }

        // Set our new button
        _bindings[action].JoystickButton = joystickButton;

        SaveBindings();
        OnBindingsChanged?.Invoke();
        return true;
    }

    public void ResetToDefaults()
    {
        InitialiseDefaultBindings();
        SaveBindings();
        OnBindingsChanged?.Invoke();
    }

    public string GetBindingDisplayName(GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var binding)) return "???";

        if (binding.JoystickButton != KeyCode.None)
        {
            return GetJoystickButtonName(binding.JoystickButton);
        }

        return "NOT SET";
    }

    public static string GetJoystickButtonName(KeyCode key)
    {
        if (key == KeyCode.None) return "NOT SET";
        
        // Use ControllerDatabase if available for controller-specific names
        if (ControllerDatabase.Instance != null)
        {
            return ControllerDatabase.Instance.GetButtonName(key);
        }
        
        // Fallback to generic names if no ControllerDatabase
        int buttonNum = (int)key - (int)KeyCode.JoystickButton0;
        
        switch (buttonNum)
        {
            case 0: return "BUTTON 1";
            case 1: return "BUTTON 2";
            case 2: return "BUTTON 3";
            case 3: return "BUTTON 4";
            case 4: return "L1";
            case 5: return "R1";
            case 6: return "L2";
            case 7: return "R2";
            case 8: return "SELECT";
            case 9: return "START";
            case 10: return "L3";
            case 11: return "R3";
            default: return $"BUTTON {buttonNum + 1}";
        }
    }

    public static string GetActionDisplayName(GameAction action)
    {
        switch (action)
        {
            case GameAction.Jump: return "JUMP";
            case GameAction.Dash: return "DASH";
            case GameAction.Clone: return "CLONE";
            default: return action.ToString();
        }
    }

    #endregion

    #region Save/Load

    private void SaveBindings()
    {
        foreach (var kvp in _bindings)
        {
            string prefix = $"Input_{kvp.Key}_";
            PlayerPrefs.SetInt(prefix + "Keyboard", (int)kvp.Value.KeyboardKey);
            PlayerPrefs.SetInt(prefix + "Joystick", (int)kvp.Value.JoystickButton);
        }
        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
        {
            string prefix = $"Input_{action}_";
            
            if (PlayerPrefs.HasKey(prefix + "Joystick"))
            {
                if (!_bindings.ContainsKey(action))
                    _bindings[action] = new InputBinding();

                _bindings[action].KeyboardKey = (KeyCode)PlayerPrefs.GetInt(prefix + "Keyboard");
                _bindings[action].JoystickButton = (KeyCode)PlayerPrefs.GetInt(prefix + "Joystick");
            }
        }
    }

    #endregion
}