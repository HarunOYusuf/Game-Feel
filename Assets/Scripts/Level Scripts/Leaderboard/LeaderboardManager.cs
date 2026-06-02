using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Persistent leaderboard manager. Stores all completion times sorted ascending
    /// (fastest first). Saves to PlayerPrefs as JSON so entries persist between sessions.
    /// 
    /// Setup:
    /// 1. Create an empty GameObject called "LeaderboardManager" in your first scene
    /// 2. Add this script
    /// 3. The script uses DontDestroyOnLoad so it persists between scene loads
    /// 
    /// Or just access via LeaderboardManager.Instance (auto-creates if missing).
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        private const string PLAYERPREFS_KEY = "Overload_Leaderboard";
        private const string LAST_NAME_KEY = "Overload_LastPlayerName";

        // Singleton with lazy auto-creation
        private static LeaderboardManager _instance;
        public static LeaderboardManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LeaderboardManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("LeaderboardManager (Auto)");
                        _instance = go.AddComponent<LeaderboardManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Debug")]
        [SerializeField] private bool _showDebugMessages = true;

        private List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

        /// <summary>
        /// All entries, sorted ascending (fastest time first).
        /// </summary>
        public IReadOnlyList<LeaderboardEntry> Entries => _entries;

        /// <summary>
        /// Total number of entries on the leaderboard.
        /// </summary>
        public int EntryCount => _entries.Count;

        /// <summary>
        /// Last name the player entered (cached for convenience).
        /// </summary>
        public string LastPlayerName
        {
            get => PlayerPrefs.GetString(LAST_NAME_KEY, "Player");
            set
            {
                PlayerPrefs.SetString(LAST_NAME_KEY, value);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadEntries();
        }

        /// <summary>
        /// Add a new entry to the leaderboard. Returns the 1-based placement
        /// (1 = fastest). The leaderboard is re-sorted and saved automatically.
        /// </summary>
        public int AddEntry(string playerName, float timeSeconds)
        {
            // Trim whitespace; default to "Anonymous" if empty
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Anonymous";
            }
            playerName = playerName.Trim();

            // Cap name length so it can't break the UI
            if (playerName.Length > 16)
            {
                playerName = playerName.Substring(0, 16);
            }

            var entry = new LeaderboardEntry(playerName, timeSeconds);
            _entries.Add(entry);

            // Sort ascending (fastest first)
            _entries = _entries.OrderBy(e => e.TimeSeconds).ToList();

            // Find the 1-based placement of the new entry
            int placement = _entries.IndexOf(entry) + 1;

            // Save to PlayerPrefs and remember the name
            SaveEntries();
            LastPlayerName = playerName;

            if (_showDebugMessages)
            {
                Debug.Log($"Leaderboard: Added '{playerName}' with {LeaderboardEntry.FormatTime(timeSeconds)} → placed #{placement}/{_entries.Count}");
            }

            return placement;
        }

        /// <summary>
        /// Calculate the placement a given time WOULD receive without actually
        /// adding the entry. Useful for showing "You will place Nth!" before
        /// the player has typed their name.
        /// </summary>
        public int CalculatePlacement(float timeSeconds)
        {
            int placement = 1;
            foreach (var entry in _entries)
            {
                if (entry.TimeSeconds < timeSeconds)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }
            return placement;
        }

        /// <summary>
        /// Clear all leaderboard entries. Saved immediately.
        /// </summary>
        public void ClearLeaderboard()
        {
            _entries.Clear();
            SaveEntries();
            
            if (_showDebugMessages)
            {
                Debug.Log("Leaderboard: Cleared all entries");
            }
        }

        private void SaveEntries()
        {
            var data = new LeaderboardData
            {
                Entries = _entries.ToArray()
            };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PLAYERPREFS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadEntries()
        {
            _entries.Clear();

            if (!PlayerPrefs.HasKey(PLAYERPREFS_KEY))
            {
                if (_showDebugMessages)
                {
                    Debug.Log("Leaderboard: No saved entries found, starting fresh");
                }
                return;
            }

            string json = PlayerPrefs.GetString(PLAYERPREFS_KEY);
            try
            {
                var data = JsonUtility.FromJson<LeaderboardData>(json);
                if (data?.Entries != null)
                {
                    _entries = data.Entries.OrderBy(e => e.TimeSeconds).ToList();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Leaderboard: Failed to load entries — {e.Message}");
                _entries.Clear();
            }

            if (_showDebugMessages)
            {
                Debug.Log($"Leaderboard: Loaded {_entries.Count} entries");
            }
        }

        /// <summary>
        /// Get the suffix for a 1-based placement: "1ST", "2ND", "3RD", "4TH", "11TH", "21ST"...
        /// </summary>
        public static string GetPlacementSuffix(int placement)
        {
            if (placement <= 0) return "TH";

            int lastTwo = placement % 100;
            // 11, 12, 13 all use TH
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return "TH";
            }

            switch (placement % 10)
            {
                case 1: return "ST";
                case 2: return "ND";
                case 3: return "RD";
                default: return "TH";
            }
        }

        /// <summary>
        /// Format a placement as "1ST", "2ND", "3RD", "11TH" etc.
        /// </summary>
        public static string FormatPlacement(int placement)
        {
            return $"{placement}{GetPlacementSuffix(placement)}";
        }
    }
}