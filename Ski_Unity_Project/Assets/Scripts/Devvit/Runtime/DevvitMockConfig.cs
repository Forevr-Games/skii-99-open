#if UNITY_EDITOR
using UnityEditor;

namespace Devvit
{
    /// <summary>
    /// Static configuration wrapper for mock Devvit service settings.
    /// Uses EditorPrefs for machine-specific storage (not committed to version control).
    /// Each developer can have their own mock data configuration.
    /// Only compiled in Editor builds.
    /// </summary>
    public static class DevvitMockConfig
    {
        private const string PREFIX = "DevvitMock.";

        /// <summary>
        /// Mock username for testing.
        /// </summary>
        public static string MockUsername
        {
            get => EditorPrefs.GetString(PREFIX + "Username", "TestUser123");
            set => EditorPrefs.SetString(PREFIX + "Username", value);
        }

        /// <summary>
        /// Mock snoovatar URL for testing.
        /// </summary>
        public static string MockSnoovatarUrl
        {
            get => EditorPrefs.GetString(PREFIX + "SnoovatarUrl",
                "https://i.redd.it/snoovatar/avatars/default.png");
            set => EditorPrefs.SetString(PREFIX + "SnoovatarUrl", value);
        }

        /// <summary>
        /// Mock post ID for testing.
        /// </summary>
        public static string MockPostId
        {
            get => EditorPrefs.GetString(PREFIX + "PostId", "test_post_abc123");
            set => EditorPrefs.SetString(PREFIX + "PostId", value);
        }

        /// <summary>
        /// Mock previous score string for testing.
        /// Format: "score" or "score;extraData" — the semicolon-delimited format
        /// used by DevvitScoreWithData.Serialize(). The "f" float suffix must NOT
        /// be included here; this is a plain numeric string parsed by float.TryParse.
        /// </summary>
        public static string MockPreviousScore
        {
            get => EditorPrefs.GetString(PREFIX + "PreviousScore", "42.50");
            set => EditorPrefs.SetString(PREFIX + "PreviousScore", value);
        }

        /// <summary>
        /// Enable or disable mock post data.
        /// When false, the mock service will return null/empty post data to test fallback flow.
        /// </summary>
        public static bool EnableMockPostData
        {
            get => EditorPrefs.GetBool(PREFIX + "EnablePostData", true);
            set => EditorPrefs.SetBool(PREFIX + "EnablePostData", value);
        }

        /// <summary>
        /// Mock post data JSON for testing.
        /// Must be valid JSON matching ChallengePostData (the ski game's post data structure):
        ///   { "author": string, "score": float, "distance": float, "stickyCommentId": string }
        /// Leave stickyCommentId empty to simulate a challenge post without a sticky comment.
        /// </summary>
        public static string MockPostDataJson
        {
            get => EditorPrefs.GetString(PREFIX + "PostDataJson",
                "{\n  \"author\": \"SkiChampion\",\n  \"score\": 5000,\n  \"distance\": 1200,\n  \"stickyCommentId\": \"\"\n}");
            set => EditorPrefs.SetString(PREFIX + "PostDataJson", value);
        }

        /// <summary>
        /// Minimum network delay in seconds to simulate real network conditions.
        /// A random delay between min and max will be used for each request.
        /// </summary>
        public static float NetworkDelayMin
        {
            get => EditorPrefs.GetFloat(PREFIX + "NetworkDelayMin", 0.3f);
            set => EditorPrefs.SetFloat(PREFIX + "NetworkDelayMin", value);
        }

        /// <summary>
        /// Maximum network delay in seconds to simulate real network conditions.
        /// A random delay between min and max will be used for each request.
        /// </summary>
        public static float NetworkDelayMax
        {
            get => EditorPrefs.GetFloat(PREFIX + "NetworkDelayMax", 1.0f);
            set => EditorPrefs.SetFloat(PREFIX + "NetworkDelayMax", value);
        }

        /// <summary>
        /// Mock leaderboard entries as a delimited string.
        /// Format: Each entry is "username:score:snoovatarUrl" separated by semicolons.
        /// Example: "Player1:100:url1;Player2:90:url2;Player3:80:url3"
        /// </summary>
        public static string MockLeaderboardEntriesRaw
        {
            get => EditorPrefs.GetString(PREFIX + "LeaderboardEntries",
                "SnowShredder:150:;PowderKing:140:;AlpineAce:130:;SlopeMaster:120:;AvalancheAce:110:;FrostyFlyer:100:;IceRider:90:;ChillRider:80:;WinterWarrior:70:;BlizzardBoss:60:");
            set => EditorPrefs.SetString(PREFIX + "LeaderboardEntries", value);
        }

        /// <summary>
        /// Get mock leaderboard entries as an array.
        /// </summary>
        public static string[] MockLeaderboardEntries
        {
            get
            {
                string raw = MockLeaderboardEntriesRaw;
                if (string.IsNullOrEmpty(raw))
                    return new string[0];

                return raw.Split(';');
            }
        }

        /// <summary>
        /// Resets all mock configuration to default values.
        /// </summary>
        public static void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(PREFIX + "Username");
            EditorPrefs.DeleteKey(PREFIX + "SnoovatarUrl");
            EditorPrefs.DeleteKey(PREFIX + "PostId");
            EditorPrefs.DeleteKey(PREFIX + "PreviousScore");
            EditorPrefs.DeleteKey(PREFIX + "EnablePostData");
            EditorPrefs.DeleteKey(PREFIX + "PostDataJson");
            EditorPrefs.DeleteKey(PREFIX + "NetworkDelayMin");
            EditorPrefs.DeleteKey(PREFIX + "NetworkDelayMax");
            EditorPrefs.DeleteKey(PREFIX + "LeaderboardEntries");
        }
    }
}
#endif
