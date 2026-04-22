#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Custom Inspector for ProceduralSoundTester with large buttons and better layout
    /// </summary>
    [CustomEditor(typeof(ProceduralSoundTester))]
    public class ProceduralSoundTesterEditor : Editor
    {
        private ProceduralSoundTester tester;
        private bool showKeyboardShortcuts = false;

        private void OnEnable()
        {
            tester = (ProceduralSoundTester)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sound Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use the buttons below to test and generate sounds.", MessageType.Info);

            // Playback controls
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("▶ Play Sound", GUILayout.Height(40)))
            {
                tester.PlaySound();
            }

            if (GUILayout.Button("■ Stop", GUILayout.Height(40)))
            {
                tester.StopSound();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Generation controls
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); // Green tint
            if (GUILayout.Button("🎲 Generate Random", GUILayout.Height(40)))
            {
                tester.GenerateRandom();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f); // Blue tint
            if (GUILayout.Button("🔀 Mutate", GUILayout.Height(40)))
            {
                tester.MutateSound();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preset Management", EditorStyles.boldLabel);

            // Show currently loaded preset
            string currentPresetDisplay = tester.GetCurrentPresetIndex() >= 0
                ? $"[{tester.GetCurrentPresetIndex()}] {tester.GetCurrentPresetName()}"
                : "None";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Preset:", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(currentPresetDisplay, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Preset navigation controls
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.8f, 0.5f); // Orange tint
            if (GUILayout.Button("◀ Previous", GUILayout.Height(35)))
            {
                tester.LoadPreviousPreset();
            }

            if (GUILayout.Button("Next ▶", GUILayout.Height(35)))
            {
                tester.LoadNextPreset();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Save preset control
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 1f, 0.5f); // Yellow tint
            if (GUILayout.Button("💾 Save Preset", GUILayout.Height(35)))
            {
                tester.SavePreset();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Show available presets list
            SerializedProperty databaseProp = serializedObject.FindProperty("presetDatabase");
            if (databaseProp != null && databaseProp.objectReferenceValue != null)
            {
                var database = databaseProp.objectReferenceValue as ProceduralSoundPresets;
                if (database != null && database.presets != null && database.presets.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Available Presets:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    for (int i = 0; i < database.presets.Count; i++)
                    {
                        var preset = database.presets[i];
                        if (preset != null)
                        {
                            EditorGUILayout.LabelField($"[{i}] {preset.name}", $"({preset.category})");
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(5);

            // Export and reset
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // Light blue
            if (GUILayout.Button("📤 Export to AudioClip", GUILayout.Height(35)))
            {
                tester.ExportAudioClip();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Light red
            if (GUILayout.Button("🔄 Reset to Defaults", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Reset Parameters",
                    "Are you sure you want to reset all parameters to defaults?",
                    "Yes", "Cancel"))
                {
                    tester.ResetToDefaults();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Keyboard shortcuts section
            EditorGUILayout.Space(10);
            showKeyboardShortcuts = EditorGUILayout.Foldout(showKeyboardShortcuts, "Keyboard Shortcuts (Optional)", true);
            if (showKeyboardShortcuts)
            {
                EditorGUILayout.HelpBox(
                    "Keyboard shortcuts are disabled by default (commented out in code).\n\n" +
                    "To enable: Uncomment the Update() method in ProceduralSoundTester.cs\n" +
                    "and set Project Settings > Player > Active Input Handling to 'Both'\n\n" +
                    "Shortcuts when enabled:\n" +
                    "Space - Play current sound\n" +
                    "R - Generate random sound\n" +
                    "M - Mutate current sound\n" +
                    "Ctrl+S - Save preset",
                    MessageType.Info);
            }

            // Tips section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Start with 'Generate Random' to create a sound in your chosen category\n" +
                "2. Use 'Mutate' to create variations\n" +
                "3. Fine-tune parameters manually in the inspector\n" +
                "4. Save your favorite sounds as presets\n" +
                "5. Export sounds as AudioClip files if you want to use them permanently",
                MessageType.Info);

            // Show current sound info
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Current Sound Info", EditorStyles.boldLabel);

            // Use reflection to access currentParameters (it's serialized but private)
            SerializedProperty paramsProp = serializedObject.FindProperty("currentParameters");
            if (paramsProp != null)
            {
                SerializedProperty waveformProp = paramsProp.FindPropertyRelative("waveform");
                SerializedProperty frequencyProp = paramsProp.FindPropertyRelative("baseFrequency");
                SerializedProperty durationProp = paramsProp.FindPropertyRelative("duration");

                if (waveformProp != null && frequencyProp != null && durationProp != null)
                {
                    string waveformName = ((WaveformType)waveformProp.enumValueIndex).ToString();
                    EditorGUILayout.LabelField("Waveform:", waveformName);
                    EditorGUILayout.LabelField("Frequency:", $"{frequencyProp.floatValue:F1} Hz");
                    EditorGUILayout.LabelField("Duration:", $"{durationProp.floatValue:F2} seconds");

                    // Calculate estimated file size
                    int fileSize = 44 + Mathf.CeilToInt(durationProp.floatValue * 44100) * 2;
                    EditorGUILayout.LabelField("Estimated WAV size:", $"{fileSize / 1024f:F1} KB");
                }
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
