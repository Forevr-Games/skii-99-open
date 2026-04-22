using System;
using UnityEditor;
using UnityEngine;

namespace Devvit
{
    /// <summary>
    /// Editor window for configuring mock Devvit service data.
    /// Accessed via Devvit > Mock Service Config menu.
    /// </summary>
    public class DevvitMockConfigWindow : EditorWindow
    {
        [System.Serializable]
        private class LeaderboardEntry
        {
            public string username;
            public string scoreData;
            public string snoovatarUrl;
        }

        private string username;
        private string snoovatarUrl;
        private string postId;
        private string previousScore;
        private bool enablePostData;
        private string postDataJson;
        private float networkDelayMin;
        private float networkDelayMax;
        private System.Collections.Generic.List<LeaderboardEntry> leaderboardEntries = new System.Collections.Generic.List<LeaderboardEntry>();

        private bool jsonValid = true;
        private string jsonError = "";

        private Vector2 scrollPosition;

        [MenuItem("Devvit/Mock Service Config")]
        public static void ShowWindow()
        {
            DevvitMockConfigWindow window = GetWindow<DevvitMockConfigWindow>("Devvit Mock Config");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            // Load from EditorPrefs
            LoadSettings();
        }

        private void LoadSettings()
        {
            username = DevvitMockConfig.MockUsername;
            snoovatarUrl = DevvitMockConfig.MockSnoovatarUrl;
            postId = DevvitMockConfig.MockPostId;
            previousScore = DevvitMockConfig.MockPreviousScore;
            enablePostData = DevvitMockConfig.EnableMockPostData;
            postDataJson = DevvitMockConfig.MockPostDataJson;
            networkDelayMin = DevvitMockConfig.NetworkDelayMin;
            networkDelayMax = DevvitMockConfig.NetworkDelayMax;

            // Parse leaderboard entries from raw string
            ParseLeaderboardEntries();
        }

        private void ParseLeaderboardEntries()
        {
            leaderboardEntries.Clear();
            string[] entries = DevvitMockConfig.MockLeaderboardEntries;

            foreach (string entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                string[] parts = entry.Split(':');
                if (parts.Length >= 2)
                {
                    leaderboardEntries.Add(new LeaderboardEntry
                    {
                        username = parts[0],
                        scoreData = parts[1],
                        snoovatarUrl = parts.Length >= 3 ? parts[2] : ""
                    });
                }
            }
        }

        private void SaveSettings()
        {
            DevvitMockConfig.MockUsername = username;
            DevvitMockConfig.MockSnoovatarUrl = snoovatarUrl;
            DevvitMockConfig.MockPostId = postId;
            DevvitMockConfig.MockPreviousScore = previousScore;
            DevvitMockConfig.EnableMockPostData = enablePostData;
            DevvitMockConfig.MockPostDataJson = postDataJson;
            DevvitMockConfig.NetworkDelayMin = networkDelayMin;
            DevvitMockConfig.NetworkDelayMax = networkDelayMax;

            // Serialize leaderboard entries to raw string
            SerializeLeaderboardEntries();
        }

        private void SerializeLeaderboardEntries()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var entry in leaderboardEntries)
            {
                parts.Add($"{entry.username}:{entry.scoreData}:{entry.snoovatarUrl}");
            }
            DevvitMockConfig.MockLeaderboardEntriesRaw = string.Join(";", parts);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Devvit Mock Service Configuration", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This configuration is used when running in Unity Editor. " +
                "Build versions will use real Devvit API calls. " +
                "Settings are stored in EditorPrefs (machine-specific).",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // User Data Section
            EditorGUILayout.LabelField("Mock User Data", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            username = EditorGUILayout.TextField("Username", username);
            snoovatarUrl = EditorGUILayout.TextField("Snoovatar URL", snoovatarUrl);
            postId = EditorGUILayout.TextField("Post ID", postId);
            previousScore = EditorGUILayout.TextField("Previous Time", previousScore);

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.Space(10);

            // Post Data Section
            EditorGUILayout.LabelField("Mock Post Data (JSON)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            enablePostData = EditorGUILayout.Toggle("Enable Mock Post Data", enablePostData);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.HelpBox(
                enablePostData
                    ? "Mock post data is ENABLED. The JSON below will be sent to the game.\nDisable to test fallback quiz flow."
                    : "Mock post data is DISABLED. The game will use fallback quizzes.\nEnable to test with custom post data.",
                enablePostData ? MessageType.Info : MessageType.Warning
            );

            // Disable the text area if post data is disabled
            EditorGUI.BeginDisabledGroup(!enablePostData);
            EditorGUI.BeginChangeCheck();
            postDataJson = EditorGUILayout.TextArea(postDataJson, GUILayout.Height(240));
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
            EditorGUI.EndDisabledGroup();

            // JSON Validation
            EditorGUILayout.Space(5);
            EditorGUI.BeginDisabledGroup(!enablePostData);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate JSON", GUILayout.Width(120)))
            {
                ValidateJson();
            }
            EditorGUILayout.EndHorizontal();

            if (!jsonValid && enablePostData)
            {
                EditorGUILayout.HelpBox($"Invalid JSON: {jsonError}", MessageType.Error);
            }
            else if (!string.IsNullOrEmpty(postDataJson) && enablePostData)
            {
                EditorGUILayout.HelpBox("JSON is valid!", MessageType.Info);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // Network Simulation Section
            EditorGUILayout.LabelField("Network Simulation", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Network Delay Range (seconds)");
            EditorGUILayout.MinMaxSlider(ref networkDelayMin, ref networkDelayMax, 0f, 3f);
            EditorGUILayout.LabelField($"{networkDelayMin:F2} - {networkDelayMax:F2}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.HelpBox(
                $"Each request will have a random delay between {networkDelayMin:F2}s and {networkDelayMax:F2}s. " +
                "Set both to 0 for instant responses during rapid testing.",
                MessageType.None
            );

            EditorGUILayout.Space(10);

            // Leaderboard Section
            EditorGUILayout.LabelField("Mock Leaderboard", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Configure mock leaderboard entries. These will be displayed in the editor when testing.",
                MessageType.Info
            );

            EditorGUI.BeginChangeCheck();

            // Track which entry to remove (can't modify list during iteration)
            int entryToRemove = -1;

            // Display array of entries
            for (int i = 0; i < leaderboardEntries.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(30));
                leaderboardEntries[i].username = EditorGUILayout.TextField(leaderboardEntries[i].username, GUILayout.Width(120));
                leaderboardEntries[i].scoreData = EditorGUILayout.TextField(leaderboardEntries[i].scoreData, GUILayout.Width(60));
                leaderboardEntries[i].snoovatarUrl = EditorGUILayout.TextField(leaderboardEntries[i].snoovatarUrl);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    entryToRemove = i;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Remove entry after iteration completes
            if (entryToRemove >= 0)
            {
                leaderboardEntries.RemoveAt(entryToRemove);
                GUI.changed = true;
            }

            // Add new entry button
            if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
            {
                leaderboardEntries.Add(new LeaderboardEntry
                {
                    username = $"Player{leaderboardEntries.Count + 1}",
                    scoreData = (100 - (leaderboardEntries.Count * 10)).ToString(), // Example score data
                    snoovatarUrl = ""
                });
                GUI.changed = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }

            EditorGUILayout.Space(10);

            // Quick Actions Section
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Configuration",
                    "Are you sure you want to reset all mock data to defaults?",
                    "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Test Mock Service"))
            {
                TestMockService();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void ValidateJson()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(postDataJson))
                {
                    jsonValid = true;
                    jsonError = "";
                    return;
                }

                // Try parsing with JsonUtility (basic validation)
                JsonUtility.FromJson<object>(postDataJson);
                jsonValid = true;
                jsonError = "";
            }
            catch (Exception e)
            {
                jsonValid = false;
                jsonError = e.Message;
            }
        }

        private void ResetToDefaults()
        {
            DevvitMockConfig.ResetToDefaults();
            LoadSettings();
            jsonValid = true;
            jsonError = "";
            Debug.Log("[DevvitMockConfig] Reset to defaults");
        }

        private void TestMockService()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Test Mock Service",
                    "The mock service test requires Play mode to be active because it uses coroutines.\n\n" +
                    "Please enter Play mode and test by calling DevvitServiceFactory.Instance from your game code.",
                    "OK"
                );
                return;
            }

            Debug.Log("=== Testing Mock Devvit Service ===");

            IDevvitService service = DevvitServiceFactory.Instance;

            service.FetchInitData(data =>
            {
                if (data != null)
                {
                    Debug.Log("✓ FetchInitData SUCCESS");
                    Debug.Log($"  Username: {data.username}");
                    Debug.Log($"  PostId: {data.postId}");
                    Debug.Log($"  PreviousScoreData: {data.previousScore}");
                    Debug.Log($"  SnoovatarUrl: {data.snoovatarUrl}");
                    Debug.Log($"  PostData: {data.postData?.RawJson ?? "null"}");
                }
                else
                {
                    Debug.LogError("✗ FetchInitData FAILED");
                }
            });

            service.CompleteLevel(new DevvitScoreWithData(123.45f), success =>
            {
                if (success)
                {
                    Debug.Log("✓ CompleteLevel SUCCESS");
                }
                else
                {
                    Debug.LogError("✗ CompleteLevel FAILED");
                }
            });
        }
    }
}
