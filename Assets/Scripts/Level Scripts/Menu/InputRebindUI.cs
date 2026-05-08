using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for rebinding CONTROLLER buttons.
/// - Shows "Please select a JUMP button..." when rebinding
/// - Buttons swap if already assigned
/// - Stays open until player selects a button
/// </summary>
public class InputRebindUI : MonoBehaviour
{
    [Header("Action Buttons")]
    [SerializeField] private Button _jumpButton;
    [SerializeField] private TextMeshProUGUI _jumpButtonText;
    
    [SerializeField] private Button _dashButton;
    [SerializeField] private TextMeshProUGUI _dashButtonText;
    
    [SerializeField] private Button _cloneButton;
    [SerializeField] private TextMeshProUGUI _cloneButtonText;

    [Header("Other Buttons")]
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _backButton;

    [Header("Waiting Panel")]
    [SerializeField] private GameObject _waitingPanel;
    [SerializeField] private TextMeshProUGUI _waitingText;

    private bool _isWaitingForInput;
    private InputManager.GameAction _currentRebindAction;
    private float _rebindStartTime;
    private float _rebindEndTime;
    private const float INPUT_DELAY = 0.15f;

    /// <summary>
    /// True when waiting for player to press a button for rebinding
    /// Also returns true briefly after closing to block other input
    /// </summary>
    public bool IsRebinding => _isWaitingForInput || (Time.unscaledTime - _rebindEndTime < INPUT_DELAY);

    private void Start()
    {
        if (_jumpButton != null)
            _jumpButton.onClick.AddListener(() => StartRebind(InputManager.GameAction.Jump));
        if (_dashButton != null)
            _dashButton.onClick.AddListener(() => StartRebind(InputManager.GameAction.Dash));
        if (_cloneButton != null)
            _cloneButton.onClick.AddListener(() => StartRebind(InputManager.GameAction.Clone));

        if (_resetButton != null)
            _resetButton.onClick.AddListener(ResetToDefaults);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsChanged += RefreshDisplay;
        }

        RefreshDisplay();
        
        if (_waitingPanel != null)
            _waitingPanel.SetActive(false);
            
        _rebindEndTime = -1f;
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsChanged -= RefreshDisplay;
        }
    }

    private void Update()
    {
        if (_isWaitingForInput)
        {
            CheckForControllerInput();
        }
    }

    public void RefreshDisplay()
    {
        if (InputManager.Instance == null) return;

        if (_jumpButtonText != null)
        {
            string buttonName = InputManager.Instance.GetBindingDisplayName(InputManager.GameAction.Jump);
            _jumpButtonText.text = "JUMP:  " + buttonName;
        }

        if (_dashButtonText != null)
        {
            string buttonName = InputManager.Instance.GetBindingDisplayName(InputManager.GameAction.Dash);
            _dashButtonText.text = "DASH:  " + buttonName;
        }

        if (_cloneButtonText != null)
        {
            string buttonName = InputManager.Instance.GetBindingDisplayName(InputManager.GameAction.Clone);
            _cloneButtonText.text = "CLONE:  " + buttonName;
        }
    }

    public void StartRebind(InputManager.GameAction action)
    {
        if (_isWaitingForInput) return;

        _currentRebindAction = action;
        _isWaitingForInput = true;
        _rebindStartTime = Time.unscaledTime;

        if (_waitingPanel != null)
        {
            _waitingPanel.SetActive(true);
        }

        if (_waitingText != null)
        {
            string actionName = InputManager.GetActionDisplayName(action);
            _waitingText.text = $"Please select a {actionName} button...";
        }
    }

    private void CheckForControllerInput()
    {
        // Ignore input briefly after opening
        if (Time.unscaledTime - _rebindStartTime < INPUT_DELAY)
        {
            return;
        }

        // Check all joystick buttons
        for (int i = 0; i < 20; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.JoystickButton0 + i);
            
            if (Input.GetKeyDown(key))
            {
                TryApplyBinding(key);
                return;
            }
        }
    }

    private void TryApplyBinding(KeyCode joystickButton)
    {
        // Apply the binding (will swap if button is already used)
        bool success = InputManager.Instance.SetJoystickBinding(_currentRebindAction, joystickButton);
        
        if (success)
        {
            FinishRebind();
        }
    }

    private void FinishRebind()
    {
        _isWaitingForInput = false;
        _rebindEndTime = Time.unscaledTime; // Track when we closed

        if (_waitingPanel != null)
        {
            _waitingPanel.SetActive(false);
        }

        RefreshDisplay();
    }

    public void ResetToDefaults()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetToDefaults();
        }
        RefreshDisplay();
    }
}