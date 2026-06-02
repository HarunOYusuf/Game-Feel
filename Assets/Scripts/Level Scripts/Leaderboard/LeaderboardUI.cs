using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateController
{
    /// <summary>
    /// Displays a scrollable list of all leaderboard entries.
    /// Used by the pause menu, main menu, and level end panel.
    /// 
    /// Setup (in your leaderboard panel):
    /// 1. Create a UI Panel with a ScrollRect
    /// 2. Inside the Viewport, create a "Content" GameObject with a VerticalLayoutGroup
    ///    and ContentSizeFitter (Vertical Fit = Preferred Size)
    /// 3. Create a row prefab (UI element) with three TMP texts: placement, name, time.
    ///    Give the prefab a LayoutElement so each row has a fixed height.
    /// 4. Add this script to the panel root
    /// 5. Assign Content Parent (the Content GameObject) and Row Prefab
    /// 6. Call Refresh() when opening the panel, or tick "Refresh On Enable"
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Required References")]
        [Tooltip("The Content GameObject inside the ScrollRect's Viewport. Rows are spawned as children.")]
        [SerializeField] private Transform _contentParent;

        [Tooltip("Prefab for one row. Should contain three TMP_Text fields (placement / name / time).")]
        [SerializeField] private LeaderboardRow _rowPrefab;

        [Header("Optional")]
        [Tooltip("Text shown when there are no entries yet (optional)")]
        [SerializeField] private TMP_Text _emptyMessage;

        [Tooltip("Index of an entry to highlight (e.g. just-added entry). -1 for none.")]
        [SerializeField] private int _highlightIndex = -1;

        [Tooltip("Refresh automatically when the panel becomes active")]
        [SerializeField] private bool _refreshOnEnable = true;

        [Header("Highlight Style")]
        [SerializeField] private Color _highlightColour = new Color(1f, 0.85f, 0.3f);

        private readonly List<LeaderboardRow> _spawnedRows = new List<LeaderboardRow>();

        private void OnEnable()
        {
            if (_refreshOnEnable)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Highlight a specific entry by 1-based placement (e.g. the entry the player just added).
        /// Call this before Refresh(), or it will reset.
        /// </summary>
        public void SetHighlightedPlacement(int placement)
        {
            _highlightIndex = placement - 1;
        }

        /// <summary>
        /// Rebuild the list from the current LeaderboardManager state.
        /// </summary>
        public void Refresh()
        {
            if (_contentParent == null)
            {
                Debug.LogWarning("LeaderboardUI: No content parent assigned!");
                return;
            }

            // Clear existing rows
            foreach (var row in _spawnedRows)
            {
                if (row != null) Destroy(row.gameObject);
            }
            _spawnedRows.Clear();

            var manager = LeaderboardManager.Instance;
            var entries = manager.Entries;

            // Empty message
            if (_emptyMessage != null)
            {
                _emptyMessage.gameObject.SetActive(entries.Count == 0);
            }

            if (entries.Count == 0 || _rowPrefab == null) return;

            // Spawn rows
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = Instantiate(_rowPrefab, _contentParent);
                int placement = i + 1;

                bool highlighted = (i == _highlightIndex);
                row.SetEntry(placement, entry, highlighted ? _highlightColour : Color.white);

                _spawnedRows.Add(row);
            }
        }
    }
}