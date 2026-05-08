using UnityEngine;

/// <summary>
/// Temporary script to test which buttons map to which KeyCodes.
/// Add to any GameObject and check Console when pressing controller buttons.
/// DELETE THIS AFTER TESTING.
/// </summary>
public class ControllerButtonTester : MonoBehaviour
{
    void Update()
    {
        for (int i = 0; i < 20; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.JoystickButton0 + i);
            if (Input.GetKeyDown(key))
            {
                Debug.Log($"PRESSED: JoystickButton{i} (KeyCode.JoystickButton{i})");
            }
        }
    }
}