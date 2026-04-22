using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using Devvit;

/// <summary>
/// Scene-resident MonoBehaviour that handles leaderboard HTTP requests to the Devvit server.
///
/// Why does this exist as a separate MonoBehaviour?
///   Unity coroutines (used for UnityWebRequest) must run on a MonoBehaviour that
///   exists in the scene. The service classes (DevvitServiceBuild, DevvitServiceMock)
///   are plain C# objects — not MonoBehaviours — so they can't start coroutines
///   directly. DevvitServiceBuild delegates leaderboard operations here, where the
///   coroutines actually run.
///
/// Initialization flow:
///   SaveDataManager calls Initialize(username, postId) after it receives init data
///   from the Devvit server. This avoids a second /api/init HTTP request.
///   Do NOT call StartCoroutine(FetchInitData()) here — that would duplicate the
///   request that SaveDataManager (and DevvitServiceBuild) already makes.
/// </summary>
public class RedditLeaderboard : MonoBehaviour
{
  private string currentUsername;
  private string currentPostId;
  private bool isInitialized = false;

  /// <summary>
  /// Initializes the leaderboard with user/post context received from SaveDataManager.
  /// Must be called before any leaderboard operations will work.
  /// </summary>
  public void Initialize(string username, string postId)
  {
    currentUsername = username;
    currentPostId = postId;
    isInitialized = true;
    Debug.Log($"[RedditLeaderboard] Initialized for user: {currentUsername}, post: {currentPostId}");
  }

  // ==========================================================================
  // Score Submission
  // ==========================================================================

  /// <summary>
  /// Submits a score to the leaderboard. If the leaderboard isn't initialized yet
  /// (init data still in flight), waits for initialization before submitting.
  /// </summary>
  public void SubmitGameScore(float score, Action<bool, int> onComplete = null)
  {
    if (!isInitialized)
    {
      Debug.LogWarning("[RedditLeaderboard] Not initialized yet — waiting before submitting score...");
      StartCoroutine(WaitForInitAndSubmitScore(score, onComplete));
      return;
    }
    StartCoroutine(PostGameScore(score, onComplete));
  }

  private IEnumerator WaitForInitAndSubmitScore(float score, Action<bool, int> onComplete)
  {
    while (!isInitialized)
    {
      yield return null;
    }
    StartCoroutine(PostGameScore(score, onComplete));
  }

  private IEnumerator PostGameScore(float score, Action<bool, int> onComplete)
  {
    // POST /api/daily-game-completed
    // Server uses a Redis sorted set for the leaderboard. It only saves the score
    // if it's higher than the player's existing best (handled server-side).
    UnityWebRequest request = new UnityWebRequest("/api/daily-game-completed", "POST");

    DailyGameCompletedRequest data = new DailyGameCompletedRequest
    {
      type = "daily-game-completed",
      username = currentUsername,
      postId = currentPostId,
      score = score
    };

    string jsonData = JsonUtility.ToJson(data);
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
      DailyGameCompletedResponse response = JsonUtility.FromJson<DailyGameCompletedResponse>(request.downloadHandler.text);

      if (response.success)
      {
        Debug.Log($"[RedditLeaderboard] Score submitted. Rank: {response.rank}");
        onComplete?.Invoke(true, response.rank);
      }
      else
      {
        Debug.LogWarning($"[RedditLeaderboard] Score submission failed: {response.message}");
        onComplete?.Invoke(false, 0);
      }
    }
    else
    {
      // This warning is expected when running in the Unity Editor (no Devvit server running).
      Debug.LogWarning("[RedditLeaderboard] Error submitting score: " + request.error + " — expected in Unity Editor");
      onComplete?.Invoke(false, 0);
    }
  }

  // ==========================================================================
  // Leaderboard Fetch
  // ==========================================================================

  /// <summary>Fetches the top 10 leaderboard entries for the current post.</summary>
  public void FetchLeaderboard(Action<LeaderboardResponse> onComplete = null)
  {
    StartCoroutine(GetLeaderboard(onComplete));
  }

  private IEnumerator GetLeaderboard(Action<LeaderboardResponse> onComplete)
  {
    // GET /api/leaderboard/:postId
    // Server returns top 10 players with snooavatar URLs, fetched from Redis sorted set.
    string url = $"/api/leaderboard/{currentPostId}";
    UnityWebRequest request = UnityWebRequest.Get(url);

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
      LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
      Debug.Log($"[RedditLeaderboard] Fetched: {response.entries.Length} entries, {response.totalPlayers} total players");
      onComplete?.Invoke(response);
    }
    else
    {
      Debug.LogWarning("[RedditLeaderboard] Error fetching leaderboard: " + request.error + " — expected in Unity Editor");
      onComplete?.Invoke(null);
    }
  }

  // ==========================================================================
  // User Rank Fetch
  // ==========================================================================

  /// <summary>Fetches a specific user's rank on the current post's leaderboard.</summary>
  public void FetchUserRank(string username, Action<UserRankResponse> onComplete = null)
  {
    StartCoroutine(GetUserRank(username, onComplete));
  }

  private IEnumerator GetUserRank(string username, Action<UserRankResponse> onComplete)
  {
    // GET /api/leaderboard/:postId/user/:username
    // Server calculates rank as (totalPlayers - ascendingRedisRank) so rank #1 = highest score.
    string url = $"/api/leaderboard/{currentPostId}/user/{username}";
    UnityWebRequest request = UnityWebRequest.Get(url);

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
      UserRankResponse response = JsonUtility.FromJson<UserRankResponse>(request.downloadHandler.text);

      if (response.ranked)
      {
        Debug.Log($"[RedditLeaderboard] User {username}: rank {response.rank}, score {response.score}, top10: {response.isTopTen}");
      }
      else
      {
        Debug.Log($"[RedditLeaderboard] User {username} not yet ranked");
      }

      onComplete?.Invoke(response);
    }
    else
    {
      Debug.LogWarning("[RedditLeaderboard] Error fetching user rank: " + request.error + " — expected in Unity Editor");
      onComplete?.Invoke(null);
    }
  }

  /// <summary>Fetches the current user's rank (shorthand for FetchUserRank with the stored username).</summary>
  public void FetchMyRank(Action<UserRankResponse> onComplete = null)
  {
    FetchUserRank(currentUsername, onComplete);
  }
}
