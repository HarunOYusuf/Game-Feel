using TMPro;
using UnityEngine;

namespace UltimateController
{
    /// <summary>
    /// Attach to your leaderboard row prefab.
    /// Holds references to the three text fields and lets LeaderboardUI populate them.
    /// 
    /// Setup:
    /// 1. Create a row UI element with a horizontal layout (e.g. Horizontal Layout Group)
    /// 2. Add three TMP_Text children: placement, name, time
    /// 3. Add this script to the row root
    /// 4. Assign the three text fields
    /// 5. Save as a prefab and reference it from LeaderboardUI
    /// </summary>
    public class LeaderboardRow : MonoBehaviour
    {
        [Tooltip("Text showing the placement, e.g. '1ST', '2ND'")]
        [SerializeField] private TMP_Text _placementText;

        [Tooltip("Text showing the player's name")]
        [SerializeField] private TMP_Text _nameText;

        [Tooltip("Text showing the time in MM:SS.mmm")]
        [SerializeField] private TMP_Text _timeText;

        /// <summary>
        /// Populate this row from a leaderboard entry.
        /// </summary>
        public void SetEntry(int placement, LeaderboardEntry entry, Color textColour)
        {
            if (_placementText != null)
            {
                _placementText.text = LeaderboardManager.FormatPlacement(placement);
                _placementText.color = textColour;
            }

            if (_nameText != null)
            {
                _nameText.text = entry.PlayerName;
                _nameText.color = textColour;
            }

            if (_timeText != null)
            {
                _timeText.text = entry.FormattedTime;
                _timeText.color = textColour;
            }
        }
    }
}