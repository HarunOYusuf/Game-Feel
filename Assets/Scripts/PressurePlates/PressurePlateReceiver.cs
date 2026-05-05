using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Base class for objects that respond to pressure plates.
    /// Extend this class or use the built-in SlidingBlock component.
    /// </summary>
    public abstract class PressurePlateReceiver : MonoBehaviour
    {
        /// <summary>
        /// Called when the connected pressure plate changes state
        /// </summary>
        /// <param name="isPressed">True if plate is pressed, false if released</param>
        public abstract void OnPressurePlateChanged(bool isPressed);
    }
}