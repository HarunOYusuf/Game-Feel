using System.Collections.Generic;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Tracks items the player has collected (keys, etc.)
    /// Automatically added to player when they collect a key.
    /// 
    /// You can also add this manually to the player if you want
    /// to access it from other scripts.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = false;

        // Collected keys
        private HashSet<string> _keys = new HashSet<string>();

        // Events
        public event System.Action<string> OnKeyCollected;
        public event System.Action<string> OnKeyUsed;

        /// <summary>
        /// Number of keys held
        /// </summary>
        public int KeyCount => _keys.Count;

        /// <summary>
        /// Add a key to the inventory
        /// </summary>
        public void AddKey(string keyID)
        {
            if (_keys.Add(keyID))
            {
                if (_showDebugMessages)
                    Debug.Log($"PlayerInventory: Added key '{keyID}'. Total keys: {_keys.Count}");

                OnKeyCollected?.Invoke(keyID);
            }
        }

        /// <summary>
        /// Remove a key from the inventory
        /// </summary>
        public void RemoveKey(string keyID)
        {
            if (_keys.Remove(keyID))
            {
                if (_showDebugMessages)
                    Debug.Log($"PlayerInventory: Removed key '{keyID}'. Total keys: {_keys.Count}");

                OnKeyUsed?.Invoke(keyID);
            }
        }

        /// <summary>
        /// Check if player has a specific key
        /// </summary>
        public bool HasKey(string keyID)
        {
            return _keys.Contains(keyID);
        }

        /// <summary>
        /// Clear all keys (for level restart)
        /// </summary>
        public void ClearKeys()
        {
            _keys.Clear();

            if (_showDebugMessages)
                Debug.Log("PlayerInventory: All keys cleared");
        }

        /// <summary>
        /// Get all held key IDs
        /// </summary>
        public string[] GetAllKeys()
        {
            string[] result = new string[_keys.Count];
            _keys.CopyTo(result);
            return result;
        }
    }
}