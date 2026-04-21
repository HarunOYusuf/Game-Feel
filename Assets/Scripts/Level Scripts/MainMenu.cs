using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateController
{
    /// <summary>
    /// Simple main menu controller.
    /// 
    /// Setup:
    /// 1. Create a new scene called "MainMenu"
    /// 2. Create a Canvas with buttons
    /// 3. Attach this script to an empty GameObject
    /// 4. Link buttons to Play() and Quit() methods
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Name of the first level scene to load")]
        [SerializeField] private string _firstLevelScene = "Level1";

        /// <summary>
        /// Called when Play button is pressed
        /// </summary>
        public void Play()
        {
            SceneManager.LoadScene(_firstLevelScene);
        }

        /// <summary>
        /// Called when Quit button is pressed
        /// </summary>
        public void Quit()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}