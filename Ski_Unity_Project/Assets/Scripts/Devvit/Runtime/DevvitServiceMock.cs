#if UNITY_EDITOR
// This entire file is excluded from builds by the #if UNITY_EDITOR guard.
// It only exists in the Unity Editor and is completely stripped from WebGL builds.
// DevvitServiceBuild is used in all non-editor builds.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Devvit
{
    /// <summary>
    /// Editor-only mock implementation of IDevvitService.
    ///
    /// Allows the game to run and test all Devvit-dependent features in the Unity Editor
    /// without deploying to Reddit or needing a network connection. All API calls
    /// return configurable fake data from DevvitMockConfig (stored in EditorPrefs).
    ///
    /// Why simulate network delay?
    ///   Real Devvit API calls take 50–500ms. Simulating delay in the mock helps catch
    ///   race conditions and timing bugs that would only appear in production otherwise
    ///   (e.g., UI showing stale data before init completes, callbacks in wrong order).
    ///   Configure the delay range in Reddit > Devvit Mock Config.
    ///
    /// Why Application.OpenURL() here but not in DevvitServiceBuild?
    ///   In the Editor, Application.OpenURL() works fine — no sandbox restrictions.
    ///   In WebGL/Devvit, it's blocked, so the build version routes through /api/open-url.
    ///   Using Application.OpenURL() in the mock gives a realistic preview of the behavior.
    /// </summary>
    public class DevvitServiceMock : IDevvitService
    {
        // Leaderboard state
        private string currentUsername;
        private List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
        private bool leaderboardInitialized = false;
        public void FetchInitData(Action<DevvitInitData> onComplete)
        {
            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    // Check if mock post data is enabled
                    string rawJson = DevvitMockConfig.EnableMockPostData
                        ? DevvitMockConfig.MockPostDataJson
                        : null;

                    DevvitInitData data = new DevvitInitData
                    {
                        username = DevvitMockConfig.MockUsername,
                        snoovatarUrl = DevvitMockConfig.MockSnoovatarUrl,
                        postId = DevvitMockConfig.MockPostId,
                        previousScore = DevvitScoreWithData.Deserialize(DevvitMockConfig.MockPreviousScore),
                        rawPostData = rawJson,
                        postData = string.IsNullOrEmpty(rawJson)
                            ? null
                            : DevvitPostData.FromJson(rawJson)
                    };

                    Debug.Log($"[DevvitServiceMock] FetchInitData: username={data.username}, postId={data.postId} previousTime={data.previousScore:F2}s rawPostData={data.rawPostData ?? "null"} (mockPostDataEnabled={DevvitMockConfig.EnableMockPostData})");
                    onComplete?.Invoke(data);
                })
            );
        }

        public void DownloadImage(string url, Action<Texture2D> onComplete)
        {
            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        Debug.LogWarning("[DevvitServiceMock] DownloadImage: URL is null or empty");
                        onComplete?.Invoke(null);
                        return;
                    }

                    // In mock mode, create a simple colored texture
                    Texture2D mockTexture = new Texture2D(128, 128);
                    Color mockColor = new Color(
                        UnityEngine.Random.value,
                        UnityEngine.Random.value,
                        UnityEngine.Random.value
                    );

                    for (int y = 0; y < mockTexture.height; y++)
                    {
                        for (int x = 0; x < mockTexture.width; x++)
                        {
                            mockTexture.SetPixel(x, y, mockColor);
                        }
                    }
                    mockTexture.Apply();

                    Debug.Log($"[DevvitServiceMock] DownloadImage: Created mock texture {mockTexture.width}x{mockTexture.height} (color: {mockColor})");
                    onComplete?.Invoke(mockTexture);
                })
            );
        }

        public void CompleteLevel(DevvitScoreWithData score, Action<bool> onComplete)
        {
            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    Debug.Log($"[DevvitServiceMock] CompleteLevel: score={score}s (mock always succeeds)");
                    onComplete?.Invoke(true); // Always succeed in mock
                })
            );
        }

        /// <summary>
        /// Simulates network delay before invoking callback.
        /// </summary>
        private IEnumerator SimulateDelay(float seconds, Action callback)
        {
            if (seconds <= 0)
            {
                callback?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }

        // ==================== LEADERBOARD METHODS ====================

        private void InitializeLeaderboard()
        {
            if (leaderboardInitialized) return;

            currentUsername = DevvitMockConfig.MockUsername;
            leaderboardEntries.Clear();

            string[] mockEntries = DevvitMockConfig.MockLeaderboardEntries;
            for (int i = 0; i < mockEntries.Length; i++)
            {
                // Parse format: "username:score:snoovatarUrl"
                string[] parts = mockEntries[i].Split(':');
                if (parts.Length >= 2)
                {
                    float score = 0;
                    if (float.TryParse(parts[1], out float parsedScore))
                    {
                        score = parsedScore;
                    }
                    leaderboardEntries.Add(new LeaderboardEntry
                    {
                        rank = i + 1,
                        username = parts[0],
                        score = score,
                        snoovatarUrl = parts.Length >= 3 ? parts[2] : ""
                    });
                }
            }

            leaderboardInitialized = true;
            Debug.Log($"[DevvitServiceMock] Initialized leaderboard with {leaderboardEntries.Count} entries");
        }

        public void SubmitGameScore(float score, Action<bool, int> onComplete)
        {
            InitializeLeaderboard();

            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    // Remove existing entry for current user if it exists
                    leaderboardEntries.RemoveAll(e => e.username == currentUsername);

                    // Add new entry
                    leaderboardEntries.Add(new LeaderboardEntry
                    {
                        rank = 0, // Will be recalculated
                        username = currentUsername,
                        score = score,
                        snoovatarUrl = DevvitMockConfig.MockSnoovatarUrl
                    });

                    // Sort by score descending and recalculate ranks
                    leaderboardEntries = leaderboardEntries.OrderByDescending(e => e.score).ToList();
                    for (int i = 0; i < leaderboardEntries.Count; i++)
                    {
                        leaderboardEntries[i].rank = i + 1;
                    }

                    // Find current user's rank
                    int rank = leaderboardEntries.FindIndex(e => e.username == currentUsername) + 1;

                    Debug.Log($"[DevvitServiceMock] SubmitGameScore: score={score}, rank={rank}/{leaderboardEntries.Count}");
                    onComplete?.Invoke(true, rank);
                })
            );
        }

        public void FetchLeaderboard(Action<LeaderboardResponse> onComplete)
        {
            InitializeLeaderboard();

            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    // Return top 10 entries
                    var topEntries = leaderboardEntries.Take(10).ToArray();

                    var response = new LeaderboardResponse
                    {
                        type = "leaderboard",
                        postId = DevvitMockConfig.MockPostId,
                        entries = topEntries,
                        totalPlayers = leaderboardEntries.Count
                    };

                    Debug.Log($"[DevvitServiceMock] FetchLeaderboard: {topEntries.Length} entries, {leaderboardEntries.Count} total players");
                    onComplete?.Invoke(response);
                })
            );
        }

        public void FetchMyRank(Action<UserRankResponse> onComplete)
        {
            InitializeLeaderboard();
            FetchUserRank(currentUsername, onComplete);
        }

        public void FetchUserRank(string username, Action<UserRankResponse> onComplete)
        {
            InitializeLeaderboard();

            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    var entry = leaderboardEntries.FirstOrDefault(e => e.username == username);

                    if (entry?.username != null)
                    {
                        var response = new UserRankResponse
                        {
                            type = "user-rank",
                            username = username,
                            postId = DevvitMockConfig.MockPostId,
                            ranked = true,
                            rank = entry.rank,
                            score = entry.score,
                            isTopTen = entry.rank <= 10,
                            message = $"Rank #{entry.rank}"
                        };

                        Debug.Log($"[DevvitServiceMock] FetchUserRank: {username} rank={entry.rank}, score={entry.score}, top10={response.isTopTen}");
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        var response = new UserRankResponse
                        {
                            type = "user-rank",
                            username = username,
                            postId = DevvitMockConfig.MockPostId,
                            ranked = false,
                            rank = 0,
                            score = new float(),
                            isTopTen = false,
                            message = "User not ranked"
                        };

                        Debug.Log($"[DevvitServiceMock] FetchUserRank: {username} not ranked");
                        onComplete?.Invoke(response);
                    }
                })
            );
        }

        // ==================== CUSTOM POST CREATION ====================

        public void CreateCustomPost(CreateCustomPostRequest requestData, Action<bool, string> onComplete)
        {
            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    // Generate a mock post URL
                    string mockPostId = $"mock-{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                    string mockPostUrl = $"https://reddit.com/r/MockSubreddit/comments/{mockPostId}";

                    Debug.Log($"[DevvitServiceMock] CreateCustomPost: title=\"{requestData.title}\", postUrl={mockPostUrl} (mock always succeeds)");
                    onComplete?.Invoke(true, mockPostUrl);
                })
            );
        }


        // ==================== COMMENT SUBMISSION ====================

        public void SubmitComment(SubmitCommentRequest requestData, Action<SubmitCommentResponse> onComplete)
        {
            float delay = UnityEngine.Random.Range(
                DevvitMockConfig.NetworkDelayMin,
                DevvitMockConfig.NetworkDelayMax
            );

            CoroutineRunner.Instance.StartCoroutine(
                SimulateDelay(delay, () =>
                {
                    // Generate a mock comment URL
                    string mockCommentId = $"mock-comment-{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                    string mockCommentUrl = $"https://reddit.com/r/MockSubreddit/comments/{requestData.replyToId}/_/{mockCommentId}";

                    Debug.Log($"[DevvitServiceMock] SubmitComment: postId={requestData.replyToId}, comment=\"{requestData.text}\", commentUrl={mockCommentUrl} (mock always succeeds)");

                    var response = new SubmitCommentResponse
                    {
                        success = true,
                        commentUrl = mockCommentUrl,
                        message = "Comment submitted successfully (mock)"
                    };
                    onComplete?.Invoke(response);
                })
            );
        }

        // ==================== URL OPENING ====================

        public void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[DevvitServiceMock] OpenUrl called with empty URL");
                return;
            }

            Debug.Log($"[DevvitServiceMock] Opening URL: {url}");
            Application.OpenURL(url);
        }
    }
}
#endif
