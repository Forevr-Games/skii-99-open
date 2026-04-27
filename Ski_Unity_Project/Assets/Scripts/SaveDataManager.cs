using UnityEngine;
using System;
using Devvit;

/// <summary>
/// Central bridge between the Devvit service layer and the game's components.
///
/// Responsibilities:
///   - On startup: fetches init data from Devvit (username, previous score, post data)
///   - Caches data in PlayerPrefs for offline access and fast startup
///   - Provides the game with scores, username, and challenge post context
///   - Handles score saving (both local and to Devvit's Redis)
///   - Handles score sharing (creates challenge posts or submits comments)
///   - Initializes RedditLeaderboard with the username/postId from init
///
/// Data flow on startup:
///   Awake() → LoadLocalData() (instant, from PlayerPrefs)
///   Start()  → FetchInitData() → HandleDevvitInitDataReceived()
///            → Updates cached values from server
///            → Calls leaderboard.Initialize() (avoids duplicate /api/init request)
///            → Fires OnDevvitInitialized so UI can update
///
/// Falls back gracefully to local data when Devvit is unavailable (e.g., Unity Editor
/// without mock config, or network errors).
/// </summary>
public class SaveDataManager : MonoBehaviour
{
  private Devvit.IDevvitService devvitService;

  // PlayerPrefs keys for local persistence
  private const string KEY_SCORE_DATA = "ScoreData";
  private const string KEY_USERNAME = "Username";

  // Cached session data
  private float highScore = 0f;
  private float furthestDistance = 0f;
  private string username = "Guest";
  private string postId = string.Empty;
  private bool isDevvitAvailable = false;
  private bool isInitializing = false;

  // Challenge post context — populated when this session is on a challenge post
  // (i.e., the post was created by another player via ShareScore)
  private bool isChallengePost = false;
  private ChallengePostData fetchedPostData = null;
  private string stickyCommentId = string.Empty;

  public bool IsChallengePost => isChallengePost;
  public ChallengePostData FetchedPostData => fetchedPostData;

  // Events for data updates
  public event Action<float> OnHighScoreUpdated;
  public event Action<float> OnFurthestDistanceUpdated;
  /// <summary>
  /// Fired when Devvit initialization completes (success or failure).
  /// UI components should wait for this before displaying user-specific data.
  /// </summary>
  public event Action OnDevvitInitialized;

  private void Awake()
  {
    // Load local data immediately so the game has something to show before
    // the async Devvit init completes.
    LoadLocalData();
  }

  private void Start()
  {
    devvitService = Devvit.DevvitServiceFactory.Instance;
    if (devvitService != null)
    {
      InitializeFromDevvit();
    }
  }

  /// <summary>
  /// Kicks off the async fetch of init data from the Devvit server.
  /// In editor: DevvitServiceMock returns fake data after a simulated delay.
  /// In build: DevvitServiceBuild makes a real GET /api/init request.
  /// </summary>
  private void InitializeFromDevvit()
  {
    isInitializing = true;
    devvitService.FetchInitData(HandleDevvitInitDataReceived);
  }

  /// <summary>
  /// Callback invoked when init data is received from Devvit (or mock).
  /// Updates all cached values and initializes dependent systems.
  /// </summary>
  private void HandleDevvitInitDataReceived(DevvitInitData initData)
  {
    string devvitUsername = initData.username;
    isDevvitAvailable = !string.IsNullOrEmpty(devvitUsername);

    if (!string.IsNullOrEmpty(devvitUsername))
    {
      username = devvitUsername;
    }

    // Merge server score with local score — take the higher of the two.
    // This handles the case where the player played offline and their local
    // score is higher than what's in Redis.
    highScore = Mathf.Max(highScore, initData.previousScore.score);
    if (float.TryParse(initData.previousScore.extraData, out float previousDistance))
    {
      furthestDistance = Mathf.Max(furthestDistance, previousDistance);
    }

    if (!string.IsNullOrEmpty(initData.postId))
    {
      postId = initData.postId;

      // rawPostData is set when this post was created as a challenge post
      // (via ShareScore → CreateChallengePost). It contains the original
      // player's score, distance, and username. If present, show it as a
      // challenge target on the main menu.
      if (!string.IsNullOrEmpty(initData.rawPostData))
      {
        try
        {
          fetchedPostData = JsonUtility.FromJson<ChallengePostData>(initData.rawPostData);
          isChallengePost = true;
          if (!string.IsNullOrEmpty(fetchedPostData.stickyCommentId))
            stickyCommentId = fetchedPostData.stickyCommentId;
        }
        catch (Exception e)
        {
          Debug.LogError($"[SaveDataManager] Failed to parse post data: {e.Message}");
          isChallengePost = false;
        }
      }
      else
      {
        isChallengePost = false;
      }
    }

    // initData.stickyCommentId is the canonical source — it's set server-side
    // by the /api/init handler directly on the response. It may also appear inside
    // rawPostData (parsed above) for older challenge posts created before the
    // top-level field was added to the init response. The top-level field takes
    // precedence here so that the most up-to-date value is always used.
    if (!string.IsNullOrEmpty(initData.stickyCommentId))
      stickyCommentId = initData.stickyCommentId;

    // Initialize RedditLeaderboard with the user/post context we just received.
    // This avoids a second /api/init request from RedditLeaderboard.Start().
    var lb = FindAnyObjectByType<RedditLeaderboard>();
    if (lb != null)
    {
      lb.Initialize(username, postId);
    }

    isInitializing = false;

    Debug.Log($"[SaveDataManager] Devvit initialized. User: {username}, Post: {postId}, HighScore: {highScore}, IsChallengePost: {isChallengePost}");

    OnDevvitInitialized?.Invoke();
  }

  /// <summary>
  /// Loads save data from PlayerPrefs. Called in Awake() for instant startup data.
  /// </summary>
  private void LoadLocalData()
  {
    string scoreJson = PlayerPrefs.GetString(KEY_SCORE_DATA + postId, string.Empty);
    var scoreData = DevvitScoreWithData.Deserialize(scoreJson);
    highScore = scoreData.score;
    if (float.TryParse(scoreData.extraData, out float savedDistance))
    {
      furthestDistance = savedDistance;
    }

    username = PlayerPrefs.GetString(KEY_USERNAME, "Guest");
  }

  /// <summary>
  /// Saves current data to PlayerPrefs for offline persistence.
  /// </summary>
  private void SaveLocalData()
  {
    string scoreJson = new DevvitScoreWithData(highScore, furthestDistance.ToString()).Serialize();
    PlayerPrefs.SetString(KEY_SCORE_DATA + postId, scoreJson);
    PlayerPrefs.SetString(KEY_USERNAME, username);
    PlayerPrefs.Save();
  }

  // ==========================================================================
  // Data Accessors
  // ==========================================================================

  /// <summary>Gets the current high score.</summary>
  public float GetHighScore() => highScore;

  /// <summary>Gets the furthest distance achieved.</summary>
  public float GetFurthestDistance() => furthestDistance;

  /// <summary>Returns the high score and distance packed into a DevvitScoreWithData.</summary>
  public DevvitScoreWithData GetScoreData() => new DevvitScoreWithData(highScore, furthestDistance.ToString());

  /// <summary>Gets the current Reddit username.</summary>
  public string GetUsername() => username;

  /// <summary>Whether the Devvit service is connected and returned valid user data.</summary>
  public bool IsDevvitAvailable() => isDevvitAvailable;

  /// <summary>Whether the init fetch is still in progress.</summary>
  public bool IsInitializing() => isInitializing;

  // ==========================================================================
  // Score Saving
  // ==========================================================================

  /// <summary>
  /// Saves a new score and distance. Persists locally immediately and to Devvit
  /// asynchronously if connected. Always submits to the leaderboard when online
  /// (the server handles keeping only the best score).
  /// </summary>
  public void SaveScore(float score, float distance, Action<bool, bool> onComplete = null)
  {
    bool isNewHighScore = score > highScore;
    bool isNewDistance = distance > furthestDistance;

    if (isNewHighScore)
    {
      highScore = score;
      OnHighScoreUpdated?.Invoke(highScore);
    }

    if (isNewDistance)
    {
      furthestDistance = distance;
      OnFurthestDistanceUpdated?.Invoke(furthestDistance);
    }

    SaveLocalData();

    if (isDevvitAvailable && devvitService != null)
    {
      DevvitScoreWithData scoreData = new DevvitScoreWithData(score, distance.ToString());

      try
      {
        // Save session score to Redis (persists as "previous score" for next visit)
        devvitService.CompleteLevel(scoreData, (success) =>
        {
          if (!success) Debug.LogWarning("[SaveDataManager] Failed to save score to Devvit.");
          onComplete?.Invoke(isNewHighScore, isNewDistance);
        });

        // Submit to leaderboard — server only keeps the best score per user,
        // so it's safe to always submit even if this isn't a personal best.
        devvitService.SubmitGameScore(score, (success, rank) =>
        {
          if (!success)
            Debug.LogWarning("[SaveDataManager] Failed to submit game score to leaderboard.");
        });
      }
      catch (Exception ex)
      {
        Debug.LogError($"[SaveDataManager] Error saving score: {ex.Message}\n{ex.StackTrace}");
      }
    }
    else
    {
      onComplete?.Invoke(isNewHighScore, isNewDistance);
    }
  }

  // ==========================================================================
  // Score Sharing (Challenge Post / Viral Loop)
  // ==========================================================================

  /// <summary>
  /// Shares the player's score on Reddit. Two paths depending on context:
  ///
  /// Normal post (isChallengePost=false):
  ///   Creates a new challenge post with the player's score embedded as postData.
  ///   Other players who open this post see it as a challenge to beat.
  ///   Also creates a stickied comment (posted as the app) so challengers have
  ///   a single thread to reply to with their scores.
  ///
  /// Challenge post (isChallengePost=true):
  ///   The player opened someone else's challenge post and beat (or attempted) it.
  ///   Submits a comment as the player, replying to the stickied comment (or the
  ///   post itself if no sticky exists).
  /// </summary>
  public void ShareScore(float score, float distance, Action<bool, string> onComplete = null)
  {
    if (isChallengePost && fetchedPostData != null)
    {
      // Player is responding to someone else's challenge — comment on their post
      SubmitPostChallengeComment(score, distance, onComplete);
    }
    else
    {
      // Player is creating a new challenge for others — make a new post
      CreateChallengePost(score, distance, onComplete);
    }
  }

  /// <summary>
  /// Creates a new Reddit custom post with the player's score as a challenge.
  /// Embeds ChallengePostData as the post's postData so challengers can see
  /// the target score when they open the post.
  /// After creating the post, creates a stickied comment as the app for
  /// challengers to reply to.
  /// </summary>
  private void CreateChallengePost(float score, float distance, System.Action<bool, string> onComplete)
  {
    if (!isDevvitAvailable || devvitService == null)
    {
      Debug.LogWarning("[SaveDataManager] Cannot share score - Devvit not available");
      onComplete?.Invoke(false, null);
      return;
    }

    // ChallengePostData is embedded in the post's postData field.
    // When another player opens this post, /api/init returns this data as rawPostData,
    // which SaveDataManager parses back into a ChallengePostData object.
    ChallengePostData postData = new ChallengePostData(username, score, distance);
    string title = $"I scored {Mathf.RoundToInt(score)} points and skied {Mathf.RoundToInt(distance)}m! Can you beat my score?";
    string gameData = JsonUtility.ToJson(postData);

    // userGeneratedContent is required by Reddit's content policy when posting as a user.
    // It must reflect content the user actually generated (their score, their action).
    UserGeneratedContent ugcData = new UserGeneratedContent
    {
      text = $"I scored {score} points and skied {distance}m in the Ski Game! Can you beat my score?",
    };

    CreateCustomPostRequest requestData = new CreateCustomPostRequest
    {
      title = title,
      gameData = gameData,
      asUser = true,        // Post is attributed to the player, not the app account
      userGeneratedContent = ugcData
    };

    devvitService.CreateCustomPost(requestData, (success, postUrl) =>
    {
      if (!success || string.IsNullOrEmpty(postUrl))
      {
        Debug.LogError("[SaveDataManager] Failed to create challenge post");
        onComplete?.Invoke(false, null);
        return;
      }

      Debug.Log($"[SaveDataManager] Challenge post created: {postUrl}");
      // The server creates the pinned comment and stores its ID in Redis.
      // Challengers will receive it via /api/init when they open this post.
      onComplete?.Invoke(true, postUrl);
    });
  }

  /// <summary>
  /// Submits the player's score as a comment on the challenge post they're playing.
  /// Replies to the stickied comment if one exists, otherwise replies to the post directly.
  /// Posted as the player (asUser=true) so their Reddit account is attributed.
  /// </summary>
  private void SubmitPostChallengeComment(float score, float distance, System.Action<bool, string> onComplete)
  {
    if (!isDevvitAvailable || devvitService == null || string.IsNullOrEmpty(postId))
    {
      Debug.LogWarning("[SaveDataManager] Cannot submit comment - Devvit not available or post ID missing");
      onComplete?.Invoke(false, null);
      return;
    }

    int scoreInt = Mathf.RoundToInt(score);
    int distanceInt = Mathf.RoundToInt(distance);
    string commentText = $"I scored {scoreInt} points and skied {distanceInt}m! Can you beat my score?";

    // Reply to the stickied comment if available (keeps challenger responses threaded).
    // Fall back to replying to the post itself if no sticky exists.
    string replyTarget = !string.IsNullOrEmpty(stickyCommentId) ? stickyCommentId : postId;

    SubmitCommentRequest requestData = new SubmitCommentRequest
    {
      replyToId = replyTarget,
      text = commentText,
      asUser = true   // Comment is posted by the challenger, not the app
    };

    devvitService.SubmitComment(requestData, (response) =>
    {
      if (response.success)
      {
        Debug.Log($"[SaveDataManager] Challenge comment submitted: {response.commentUrl}");
        onComplete?.Invoke(true, response.commentUrl);
      }
      else
      {
        Debug.LogError("[SaveDataManager] Failed to submit challenge comment");
        onComplete?.Invoke(false, null);
      }
    });
  }

  // ==========================================================================
  // Leaderboard Access
  // ==========================================================================

  /// <summary>Fetches the leaderboard from the server. Returns null via callback if unavailable.</summary>
  public void FetchLeaderboard(Action<LeaderboardResponse> onComplete)
  {
    if (isDevvitAvailable && devvitService != null)
    {
      devvitService.FetchLeaderboard(onComplete);
    }
    else
    {
      onComplete?.Invoke(null);
    }
  }

  /// <summary>Fetches the current user's rank from the server. Returns null via callback if unavailable.</summary>
  public void FetchUserRank(Action<UserRankResponse> onComplete)
  {
    if (isDevvitAvailable && devvitService != null && !string.IsNullOrEmpty(username))
    {
      devvitService.FetchUserRank(username, onComplete);
    }
    else
    {
      onComplete?.Invoke(null);
    }
  }

  /// <summary>Clears all locally saved data. Does not affect data stored in Devvit's Redis.</summary>
  public void ClearLocalData()
  {
    PlayerPrefs.DeleteKey(KEY_SCORE_DATA);
    PlayerPrefs.DeleteKey(KEY_USERNAME);
    PlayerPrefs.Save();

    highScore = 0f;
    furthestDistance = 0f;
    username = "Guest";
  }
}

/// <summary>
/// Data embedded in a challenge post's postData field when created via ShareScore.
/// Deserialized from rawPostData in /api/init when another player opens the post.
/// </summary>
public class ChallengePostData
{
  /// <summary>Reddit username of the player who created the challenge.</summary>
  public string author;
  /// <summary>The score challengers need to beat.</summary>
  public float score;
  /// <summary>The distance challengers need to beat.</summary>
  public float distance;
  /// <summary>ID of the app's stickied comment where challengers post their responses.</summary>
  public string stickyCommentId;

  public ChallengePostData(string author, float score, float distance)
  {
    this.author = author;
    this.score = score;
    this.distance = distance;
  }
}
