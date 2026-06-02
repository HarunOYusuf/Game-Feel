using System;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// A single leaderboard entry: player name, completion time in seconds, and date.
    /// Serialisable so it can be saved via JsonUtility + PlayerPrefs.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string PlayerName;
        public float TimeSeconds;
        public string DateString; // ISO 8601 format for easy parsing

        public LeaderboardEntry() { }

        public LeaderboardEntry(string playerName, float timeSeconds)
        {
            PlayerName = playerName;
            TimeSeconds = timeSeconds;
            DateString = DateTime.UtcNow.ToString("o"); // ISO 8601
        }

        /// <summary>
        /// Returns the time formatted as MM:SS.mmm (e.g. "02:47.350")
        /// </summary>
        public string FormattedTime => FormatTime(TimeSeconds);

        /// <summary>
        /// Format a time in seconds as MM:SS.mmm
        /// </summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            
            int minutes = (int)(seconds / 60f);
            float remainingSeconds = seconds - (minutes * 60f);
            int wholeSeconds = (int)remainingSeconds;
            int milliseconds = (int)((remainingSeconds - wholeSeconds) * 1000f);
            
            return $"{minutes:00}:{wholeSeconds:00}.{milliseconds:000}";
        }
    }

    /// <summary>
    /// Wrapper for serialising a list of leaderboard entries to JSON
    /// (JsonUtility cannot serialise top-level lists directly).
    /// </summary>
    [Serializable]
    public class LeaderboardData
    {
        public LeaderboardEntry[] Entries;
    }
}