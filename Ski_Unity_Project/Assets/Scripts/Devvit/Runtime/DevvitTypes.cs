using System;

/// <summary>
/// C# data types for Devvit API communication.
///
/// These types mirror the TypeScript definitions in:
///   devvit-ski/src/shared/types/api.ts
///
/// All types are marked [Serializable] so Unity's JsonUtility can
/// serialize them to JSON (for HTTP request bodies) and deserialize
/// them from JSON (for HTTP response bodies).
///
/// Important: JsonUtility only serializes public fields, not properties.
/// Field names must exactly match the JSON keys returned by the server.
/// </summary>
namespace Devvit
{
  // ==========================================================================
  // Initialization
  // ==========================================================================

  /// <summary>
  /// Response from GET /api/init — contains all context needed to start a game session.
  /// Received once when the game first loads inside a Reddit post.
  /// </summary>
  [Serializable]
  public class DevvitInitData
  {
    /// <summary>The current Reddit user's username (without "u/" prefix).</summary>
    public string username;

    /// <summary>CDN URL for the user's snoovatar image. Empty string if none.</summary>
    public string snoovatarUrl;

    /// <summary>The Reddit post ID this game session is associated with.</summary>
    public string postId;

    /// <summary>
    /// The user's best previous score for this post, retrieved from Redis.
    /// Used to show the player their previous best on the main menu.
    /// </summary>
    public DevvitScoreWithData previousScore;

    /// <summary>
    /// Parsed custom post data. Populated from rawPostData if the post was created
    /// with embedded game data (e.g. a challenge post). Null for standard posts.
    /// </summary>
    public DevvitPostData postData;

    /// <summary>
    /// Raw JSON string of the post's embedded data, as received from the server.
    /// The server sets this from context.postData (the JSON embedded when the post
    /// was created via submitCustomPost). Null or empty for standard posts.
    /// </summary>
    public string rawPostData;

    /// <summary>
    /// The ID of the stickied comment on a challenge post, if one exists.
    /// Challengers reply to this comment to keep their scores threaded together.
    /// </summary>
    public string stickyCommentId;
  }

  // ==========================================================================
  // Score Persistence
  // ==========================================================================

  /// <summary>
  /// Request body for POST /api/level-completed — saves a session score to Redis.
  /// Persists the score so it appears as "previous score" on the next visit.
  /// </summary>
  [Serializable]
  public class LevelCompletionData
  {
    /// <summary>
    /// Discriminator string sent with every request body so the server can identify
    /// the message type in logs. Always set to "level-completed".
    /// </summary>
    public string type;
    public string username;
    public string postId;
    public DevvitScoreWithData score;
  }

  /// <summary>Response from POST /api/level-completed.</summary>
  [Serializable]
  public class LevelCompletionResponse
  {
    /// <summary>Discriminator string identifying the response type in logs.</summary>
    public string type;
    public bool success;
    public string message;
  }

  // ==========================================================================
  // Leaderboard
  // ==========================================================================

  /// <summary>
  /// Request body for POST /api/daily-game-completed — submits a score to the
  /// post's leaderboard (a Redis sorted set). Server handles "best score wins" logic.
  /// </summary>
  [System.Serializable]
  public class DailyGameCompletedRequest
  {
    /// <summary>Discriminator string identifying the request type. Always "daily-game-completed".</summary>
    public string type;
    public string username;
    public string postId;
    public float score;
  }

  /// <summary>
  /// Response from POST /api/daily-game-completed — includes the player's rank
  /// after submission (1 = first place).
  /// </summary>
  [System.Serializable]
  public class DailyGameCompletedResponse
  {
    public string type;
    public bool success;
    public string message;
    /// <summary>1-based rank position. 0 if rank could not be determined.</summary>
    public int rank;
  }

  /// <summary>A single player entry in the leaderboard.</summary>
  [System.Serializable]
  public class LeaderboardEntry
  {
    /// <summary>1-based rank position (1 = first place).</summary>
    public int rank;
    public string username;
    public float score;
    /// <summary>CDN URL for the player's snoovatar. Empty string if unavailable.</summary>
    public string snoovatarUrl;
  }

  /// <summary>
  /// Response from GET /api/leaderboard/:postId — the top 10 players for a post.
  /// </summary>
  [System.Serializable]
  public class LeaderboardResponse
  {
    public string type;
    public string postId;
    /// <summary>Top 10 entries sorted by score descending.</summary>
    public LeaderboardEntry[] entries;
    /// <summary>Total number of players who have submitted a score for this post.</summary>
    public int totalPlayers;
  }

  /// <summary>
  /// Response from GET /api/leaderboard/:postId/user/:username — a specific user's rank.
  /// Check <see cref="ranked"/> before reading rank/score fields.
  /// </summary>
  [System.Serializable]
  public class UserRankResponse
  {
    public string type;
    public string username;
    public string postId;
    /// <summary>Whether the user has a score on this leaderboard.</summary>
    public bool ranked;
    /// <summary>1-based rank (only valid when ranked=true).</summary>
    public int rank;
    /// <summary>The user's score (only valid when ranked=true).</summary>
    public float score;
    /// <summary>Whether the user is in the top 10 (only valid when ranked=true).</summary>
    public bool isTopTen;
    public string message;
  }

  // ==========================================================================
  // Post Creation
  // ==========================================================================

  /// <summary>
  /// Request body for POST /api/create-custom-post — creates a new Reddit post.
  /// Used for the challenge post viral loop: player shares their score as a post
  /// that others can try to beat.
  /// </summary>
  [Serializable]
  public class CreateCustomPostRequest
  {
    /// <summary>The post title displayed on Reddit.</summary>
    public string title;
    /// <summary>
    /// JSON-serialized game data to embed in the post via postData.
    /// The server parses this and passes it to submitCustomPost({ postData: ... }).
    /// Future players who open this post will receive this data via /api/init.
    /// </summary>
    public string gameData;
    /// <summary>
    /// If true, the post is attributed to the logged-in user.
    /// If false, it's posted by the app's service account.
    /// </summary>
    public bool asUser;
    /// <summary>
    /// Required when asUser=true. Reddit's content policy requires this for
    /// user-attributed submissions.
    /// </summary>
    public UserGeneratedContent userGeneratedContent;
  }

  /// <summary>Response from POST /api/create-custom-post.</summary>
  [Serializable]
  public class CreateCustomPostResponse
  {
    public bool success;
    /// <summary>Full Reddit URL to the created post (e.g. "https://reddit.com/r/sub/comments/...").</summary>
    public string postUrl;
    public string message;
  }

  /// <summary>
  /// User-generated content required when creating or submitting content as a user.
  /// Reddit's content policy requires this for any user-attributed submissions.
  /// </summary>
  [Serializable]
  public class UserGeneratedContent
  {
    public string text;
    public string[] imageUrls;
  }

  // ==========================================================================
  // Comment Submission
  // ==========================================================================

  /// <summary>
  /// Request body for POST /api/submit-comment — submits a comment from the game.
  /// Used in the challenge flow for challengers to post their score.
  /// </summary>
  [Serializable]
  public class SubmitCommentRequest
  {
    /// <summary>The comment text.</summary>
    public string text;
    /// <summary>
    /// The post or comment ID to reply to.
    /// Post ID (e.g. "t3_abc123") creates a top-level comment.
    /// Comment ID (e.g. "t1_def456") creates a reply to that comment.
    /// </summary>
    public string replyToId;
    /// <summary>If true, comment is attributed to the logged-in user; otherwise posted as the app.</summary>
    public bool asUser;
  }

  /// <summary>Response from POST /api/submit-comment.</summary>
  [Serializable]
  public class SubmitCommentResponse
  {
    public bool success;
    /// <summary>Full Reddit URL to the submitted comment.</summary>
    public string commentUrl;
    public string message;
  }

  // ==========================================================================
  // Score Data Format
  // ==========================================================================

  /// <summary>
  /// Score data structure used for both persistence (Redis) and passing scores
  /// between game sessions.
  ///
  /// Serialization format: "score;extraData"
  ///   Example: "1234.56;789.0" where score=1234.56 and extraData="789.0"
  ///
  /// Why this format?
  ///   Redis stores all values as plain strings. The semicolon delimiter lets us
  ///   store a primary score (for leaderboard comparison) and optional extra data
  ///   (e.g. the furthest distance) in a single Redis value without nested JSON.
  ///
  /// The TypeScript equivalent is DevvitScoreData in shared/types/api.ts.
  /// parseDevvitScoreData() in api.ts deserializes this same format.
  /// </summary>
  [Serializable]
  public class DevvitScoreWithData
  {
    /// <summary>The primary score value used for leaderboard comparison.</summary>
    public float score;
    /// <summary>
    /// Optional extra data stored alongside the score. In this game, it's the
    /// furthest distance achieved (as a string). Can be any string your game needs.
    /// </summary>
    public string extraData;

    public DevvitScoreWithData()
    {
      score = 0f;
      extraData = "";
    }

    public DevvitScoreWithData(float score, string extraData = "")
    {
      this.score = score;
      this.extraData = extraData;
    }

    public override string ToString()
    {
      return this.Serialize();
    }

    /// <summary>
    /// Serializes to the Redis storage format: "score;extraData".
    /// Must match the format expected by parseDevvitScoreData() in api.ts.
    /// </summary>
    public string Serialize()
    {
      return score.ToString("F2") + ";" + extraData;
    }

    /// <summary>
    /// Deserializes from the Redis storage format: "score;extraData".
    /// Returns a zero-score instance if the input is null, empty, or malformed.
    /// </summary>
    public static DevvitScoreWithData Deserialize(string scoreDataString)
    {
      var parts = scoreDataString.Split(';');
      if (parts.Length >= 2)
      {
        float.TryParse(parts[0], out float score);
        string extraData = parts[1];
        return new DevvitScoreWithData(score, extraData);
      }
      else if (parts.Length == 1)
      {
        float.TryParse(parts[0], out float score);
        return new DevvitScoreWithData(score);
      }
      else
      {
        return new DevvitScoreWithData();
      }
    }
  }
}
