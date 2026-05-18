using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Controller navigation for UI menus.
/// D-pad/Stick to navigate, X to select, Circle to go back.
/// 
/// PS5 Mappings:
/// - X (Button 1) = Select
/// - Circle (Button 2) = Back
/// </summary>
public class UINavigator : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Selectable _firstSelected;
    [SerializeField] private bool _autoFindSelectables = true;

    [Header("Visual Feedback")]
    [Tooltip("Scale multiplier when selected (1.0 = no change, 1.05 = 5% bigger)")]
    [SerializeField] private float _selectedScaleMultiplier = 1.05f;
    
    [Tooltip("How much brighter when selected (0 = no change, 0.1 = slightly brighter)")]
    [SerializeField] private float _selectedBrightnessBoost = 0.15f;

    [Header("Input Settings")]
    [SerializeField] private float _inputRepeatDelay = 0.25f;

    [Header("Back Button")]
    [Tooltip("Button to click when Circle/Back is pressed (optional)")]
    [SerializeField] private Button _backButton;

    private List<Selectable> _selectables = new List<Selectable>();
    private int _currentIndex = 0;
    private Selectable _currentSelected;
    private float _lastInputTime;
    private bool _inputWasZero = true;
    
    // Store original visuals
    private Dictionary<Selectable, Vector3> _originalScales = new Dictionary<Selectable, Vector3>();
    private Dictionary<Selectable, Color> _originalColours = new Dictionary<Selectable, Color>();

    private void OnEnable()
    {
        if (_autoFindSelectables)
        {
            FindSelectables();
        }

        StartCoroutine(SelectFirstDelayed());
    }

    private void OnDisable()
    {
        // Reset all visuals when disabled
        ResetAllVisuals();
    }

    private System.Collections.IEnumerator SelectFirstDelayed()
    {
        yield return null;
        
        if (_firstSelected != null && _firstSelected.gameObject.activeInHierarchy)
        {
            SelectElement(_firstSelected);
        }
        else if (_selectables.Count > 0)
        {
            SelectElement(_selectables[0]);
        }
    }

    private void FindSelectables()
    {
        _selectables.Clear();
        _originalScales.Clear();
        _originalColours.Clear();
        
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.interactable && btn.gameObject.activeInHierarchy)
            {
                _selectables.Add(btn);
                
                // Store original scale
                _originalScales[btn] = btn.transform.localScale;
                
                // Store original colour (from text or image)
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    _originalColours[btn] = text.color;
                }
            }
        }

        // Sort top to bottom
        _selectables.Sort((a, b) => 
            b.transform.position.y.CompareTo(a.transform.position.y));
    }

    private void Update()
    {
        if (_selectables.Count == 0) return;

        HandleNavigation();
        HandleConfirm();
        HandleBack();
    }

    private void HandleNavigation()
    {
        // Check if InputRebindUI is waiting for input - if so, don't navigate
        var rebindUI = GetComponentInChildren<InputRebindUI>();
        if (rebindUI == null)
        {
            rebindUI = GetComponent<InputRebindUI>();
        }
        
        if (rebindUI != null && rebindUI.IsRebinding)
        {
            return;
        }

        // Get input from left stick (Vertical axis)
        float vertical = Input.GetAxisRaw("Vertical");
        
        // PS5/PS4 D-pad is on Axis 8 (7th axis, 0-indexed)
        // We need to read it directly using GetAxis with joystick axis
        float dpadVertical = Input.GetAxisRaw("DPadY");
        if (Mathf.Abs(dpadVertical) > Mathf.Abs(vertical))
        {
            vertical = dpadVertical;
        }
        
        // Fallback: Some controllers map D-pad to button indices
        if (Input.GetKey(KeyCode.JoystickButton11)) vertical = 1f;  // Up on some
        if (Input.GetKey(KeyCode.JoystickButton12)) vertical = -1f; // Down on some
        
        bool canInput = _inputWasZero || (Time.unscaledTime - _lastInputTime > _inputRepeatDelay);
        
        if (canInput)
        {
            if (vertical > 0.5f)
            {
                Navigate(-1);
                _lastInputTime = Time.unscaledTime;
                _inputWasZero = false;
            }
            else if (vertical < -0.5f)
            {
                Navigate(1);
                _lastInputTime = Time.unscaledTime;
                _inputWasZero = false;
            }
        }

        if (Mathf.Abs(vertical) < 0.3f)
        {
            _inputWasZero = true;
        }
    }

    private void HandleConfirm()
    {
        // Check if InputRebindUI is waiting for input - if so, don't handle confirm
        var rebindUI = GetComponentInChildren<InputRebindUI>();
        if (rebindUI == null)
        {
            rebindUI = GetComponent<InputRebindUI>();
        }
        
        if (rebindUI != null && rebindUI.IsRebinding)
        {
            // Don't handle confirm - let the rebind UI capture this button press
            return;
        }

        // Get confirm button from ControllerDatabase, fallback to JoystickButton1 (X on PS)
        KeyCode confirmButton = KeyCode.JoystickButton1;
        if (ControllerDatabase.Instance != null)
        {
            confirmButton = ControllerDatabase.Instance.GetConfirmButton();
        }

        bool confirmPressed = Input.GetKeyDown(confirmButton) || 
                              Input.GetKeyDown(KeyCode.Return) || 
                              Input.GetKeyDown(KeyCode.Space);

        if (confirmPressed && _currentSelected != null)
        {
            var button = _currentSelected as Button;
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }
    }

    private void HandleBack()
    {
        // Get back button from ControllerDatabase, fallback to JoystickButton2 (Circle on PS)
        KeyCode backButton = KeyCode.JoystickButton2;
        if (ControllerDatabase.Instance != null)
        {
            backButton = ControllerDatabase.Instance.GetBackButton();
        }

        bool backPressed = Input.GetKeyDown(backButton) || 
                           Input.GetKeyDown(KeyCode.Escape);

        if (!backPressed) return;

        // Check if InputRebindUI is waiting for input - if so, don't handle back
        var rebindUI = GetComponentInChildren<InputRebindUI>();
        if (rebindUI == null)
        {
            rebindUI = GetComponent<InputRebindUI>();
        }
        
        if (rebindUI != null && rebindUI.IsRebinding)
        {
            // Don't go back - let the rebind UI handle this button press
            return;
        }

        // If back button is assigned, click it
        if (_backButton != null)
        {
            _backButton.onClick.Invoke();
            return;
        }

        // Otherwise try to find a back/resume/close button
        foreach (var sel in _selectables)
        {
            var button = sel as Button;
            if (button != null)
            {
                string name = button.name.ToLower();
                if (name.Contains("back") || name.Contains("resume") || name.Contains("close"))
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }
    }

    private void Navigate(int direction)
    {
        int newIndex = _currentIndex + direction;
        
        if (newIndex < 0) newIndex = _selectables.Count - 1;
        if (newIndex >= _selectables.Count) newIndex = 0;

        int attempts = 0;
        while (attempts < _selectables.Count)
        {
            if (_selectables[newIndex] != null && 
                _selectables[newIndex].interactable && 
                _selectables[newIndex].gameObject.activeInHierarchy)
            {
                break;
            }
            
            newIndex += direction;
            if (newIndex < 0) newIndex = _selectables.Count - 1;
            if (newIndex >= _selectables.Count) newIndex = 0;
            attempts++;
        }

        _currentIndex = newIndex;
        SelectElement(_selectables[_currentIndex]);
    }

    private void SelectElement(Selectable element)
    {
        // Reset previous selection visual
        if (_currentSelected != null)
        {
            ResetVisual(_currentSelected);
        }

        _currentSelected = element;
        _currentIndex = _selectables.IndexOf(element);
        if (_currentIndex < 0) _currentIndex = 0;

        if (_currentSelected != null)
        {
            // Apply selection visual
            ApplySelectedVisual(_currentSelected);
            
            // Update EventSystem
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_currentSelected.gameObject);
            }
        }
    }

    private void ApplySelectedVisual(Selectable element)
    {
        if (element == null) return;

        // Slight scale increase
        if (_originalScales.TryGetValue(element, out Vector3 originalScale))
        {
            element.transform.localScale = originalScale * _selectedScaleMultiplier;
        }

        // Slight brightness boost on text
        var text = element.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null && _originalColours.TryGetValue(element, out Color originalColour))
        {
            Color brighterColour = new Color(
                Mathf.Min(1f, originalColour.r + _selectedBrightnessBoost),
                Mathf.Min(1f, originalColour.g + _selectedBrightnessBoost),
                Mathf.Min(1f, originalColour.b + _selectedBrightnessBoost),
                originalColour.a
            );
            text.color = brighterColour;
        }
    }

    private void ResetVisual(Selectable element)
    {
        if (element == null) return;

        // Reset scale
        if (_originalScales.TryGetValue(element, out Vector3 originalScale))
        {
            element.transform.localScale = originalScale;
        }

        // Reset colour
        var text = element.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null && _originalColours.TryGetValue(element, out Color originalColour))
        {
            text.color = originalColour;
        }
    }

    private void ResetAllVisuals()
    {
        foreach (var sel in _selectables)
        {
            ResetVisual(sel);
        }
    }

    public void RefreshSelectables()
    {
        ResetAllVisuals();
        
        if (_autoFindSelectables)
        {
            FindSelectables();
        }

        if (_selectables.Count > 0)
        {
            SelectElement(_selectables[0]);
        }
    }
}