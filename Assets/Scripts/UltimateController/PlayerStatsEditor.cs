#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace UltimateController.Editor
{
    /// <summary>
    /// Custom editor for PlayerStats with preset buttons and helpful tooltips
    /// </summary>
    [CustomEditor(typeof(PlayerStats))]
    public class PlayerStatsEditor : UnityEditor.Editor
    {
        private PlayerStats _stats;
        private bool _showPresets = true;
        private bool _showTips = false;

        private void OnEnable()
        {
            _stats = (PlayerStats)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ultimate Player Controller Stats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Preset Buttons
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Quick Presets", true);
            if (_showPresets)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Platformer", GUILayout.Height(30)))
                {
                    Undo.RecordObject(_stats, "Apply Platformer Preset");
                    _stats.ApplyPreset(MovementPreset.Platformer);
                    EditorUtility.SetDirty(_stats);
                }
                
                if (GUILayout.Button("Floaty", GUILayout.Height(30)))
                {
                    Undo.RecordObject(_stats, "Apply Floaty Preset");
                    _stats.ApplyPreset(MovementPreset.Floaty);
                    EditorUtility.SetDirty(_stats);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Tight", GUILayout.Height(30)))
                {
                    Undo.RecordObject(_stats, "Apply Tight Preset");
                    _stats.ApplyPreset(MovementPreset.Tight);
                    EditorUtility.SetDirty(_stats);
                }
                
                if (GUILayout.Button("Celeste-like", GUILayout.Height(30)))
                {
                    Undo.RecordObject(_stats, "Apply Celeste Preset");
                    _stats.ApplyPreset(MovementPreset.Celeste);
                    EditorUtility.SetDirty(_stats);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
            }

            // Tips Section
            _showTips = EditorGUILayout.Foldout(_showTips, "Tuning Tips", true);
            if (_showTips)
            {
                EditorGUILayout.HelpBox(
                    "• Jump Buffer: Higher values make it easier to chain jumps\n" +
                    "• Coyote Time: Higher values are more forgiving for late jumps\n" +
                    "• Jump Cut Multiplier: Higher = shorter minimum jump\n" +
                    "• Apex Gravity Multiplier: Lower = more hang time at jump peak\n" +
                    "• Max Fall Speed: Caps terminal velocity for better control",
                    MessageType.Info
                );
                EditorGUILayout.Space(5);
            }

            // Draw default inspector
            EditorGUILayout.Space(5);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif