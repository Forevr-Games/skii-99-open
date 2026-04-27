/**
 * Shared API Types — TypeScript ↔ C# Contract
 *
 * These types define the request/response shapes for all API endpoints.
 * They must stay in sync with the C# equivalents in:
 *   Assets/Scripts/Devvit/Runtime/DevvitTypes.cs
 *
 * Data flow overview:
 *   1. Game starts → Unity calls GET /api/init → server returns DevvitInitData
 *   2. Game ends   → Unity calls POST /api/daily-game-completed → leaderboard updated
 *   3. Player shares score → Unity calls POST /api/create-custom-post or /api/submit-comment
 *   4. Leaderboard screen → Unity calls GET /api/leaderboard/:postId
 */

// =============================================================================
// Initialization
// =============================================================================

/**
 * Response from GET /api/init — seeds the game with all context needed for
 * the current session: who the player is, their previous score, and any
 * custom data embedded in this post (e.g. challenge data from another player).
 */
export type DevvitInitData = {
  type: "init";
  /** The Reddit post ID this game session is associated with */
  postId: string;
  /** Reddit username of the current user, or "anonymous" if not logged in */
  username: string;
  /** CDN URL for the user's snoovatar image; empty string if none */
  snoovatarUrl: string;
  /** The user's best previous score on this specific post (from Redis) */
  previousScore: DevvitScoreData;
  /**
   * Raw JSON string of the post's custom data, set when the post was created
   * via submitCustomPost({ postData: {...} }). Used for challenge posts to
   * carry the original player's score to challengers. Undefined for standard posts.
   */
  rawPostData?: string;
  /**
   * The ID of the stickied comment on a challenge post. Challengers reply to
   * this comment to keep all scores threaded together. Undefined for standard posts.
   */
  stickyCommentId?: string;
};

// =============================================================================
// Score Persistence
// =============================================================================

/**
 * Request body for POST /api/level-completed — saves a session score to Redis.
 * Used to persist the "previous score" that appears when the user returns to this post.
 */
export type LevelCompletionData = {
  type: "level-completed";
  username: string;
  postId: string;
  /** The score to persist, serialized as "score;extraData" */
  score: DevvitScoreData;
};

/** Response from POST /api/level-completed */
export type LevelCompletionResponse = {
  type: "level-completed";
  success: boolean;
  message?: string;
};

// =============================================================================
// Leaderboard
// =============================================================================

/**
 * Request body for POST /api/daily-game-completed — submits a score to the
 * post's leaderboard (stored in a Redis sorted set). The server handles
 * "best score wins" logic; lower scores are not saved.
 */
export type DailyGameCompletedRequest = {
  type: "daily-game-completed";
  username: string;
  postId: string;
  /** Numeric score value used for leaderboard ranking */
  score: number;
};

/**
 * Response from POST /api/daily-game-completed — includes the player's
 * current rank after the score submission.
 */
export type DailyGameCompletedResponse = {
  type: "daily-game-completed";
  success: boolean;
  message?: string;
  /** The player's rank (1 = first place). Omitted if rank cannot be determined. */
  rank?: number;
};

/** A single entry in the leaderboard */
export type LeaderboardEntry = {
  /** 1-based rank position (1 = first place) */
  rank: number;
  username: string;
  score: number;
  /** CDN URL for the player's snoovatar; empty string if unavailable */
  snoovatarUrl: string;
};

/**
 * Response from GET /api/leaderboard/:postId — the top 10 players for a post.
 */
export type LeaderboardResponse = {
  type: "leaderboard";
  postId: string;
  /** Top 10 entries, sorted by score descending */
  entries: LeaderboardEntry[];
  /** Total number of players who have submitted a score for this post */
  totalPlayers: number;
};

/**
 * Response from GET /api/leaderboard/:postId/user/:username — a specific
 * user's rank on the leaderboard. `ranked: false` means the user has no score yet.
 */
export type UserRankResponse = {
  type: "user-rank";
  username: string;
  postId: string;
  /** Whether the user has a score on this leaderboard */
  ranked: boolean;
  /** 1-based rank (only present when ranked=true) */
  rank?: number;
  /** The user's score (only present when ranked=true) */
  score?: number;
  /** Whether the user is in the top 10 (only present when ranked=true) */
  isTopTen?: boolean;
  message?: string;
};

// =============================================================================
// Score Data Format
// =============================================================================

/**
 * Score data structure used for both persistence and leaderboard submission.
 *
 * Serialization format (for Redis storage):
 *   "score;extraData"  e.g. "1234.56;789.0"
 *
 * Why this format?
 *   Redis stores values as plain strings. To persist multiple values (score +
 *   extra game data like distance), we use a semicolon-delimited string.
 *   The `parseDevvitScoreData` function handles deserialization.
 *   The C# equivalent `DevvitScoreWithData.Serialize()` produces the same format.
 */
export type DevvitScoreData = {
  score: number;
  /**
   * Optional extra game-specific data stored alongside the score.
   * In this game, it stores the furthest distance achieved (as a string).
   * You can store any additional data you want to persist with the score here.
   */
  extraData?: string;
};

/**
 * Deserializes a Redis score string ("score;extraData") into a DevvitScoreData object.
 * Returns { score: 0 } if the input is undefined or malformed.
 */
export function parseDevvitScoreData(scoreData: string | undefined): DevvitScoreData {
  if (!scoreData) {
    return { score: 0 };
  }
  const [scoreStr, extraData] = scoreData.split(';');
  const result: DevvitScoreData = {
    score: parseFloat(scoreStr || '0'),
  };
  if (extraData) {
    result.extraData = extraData;
  }
  return result;
}

// =============================================================================
// Post Creation
// =============================================================================

/**
 * Request body for POST /api/create-custom-post — creates a new Reddit custom
 * post from within the game. Used for the challenge post viral loop.
 */
export type CreateCustomPostRequest = {
  /** The post title shown on Reddit */
  title: string;
  /**
   * JSON-serialized game data to embed in the post via postData.
   * Accessible in future server requests as context.postData.
   * Must be a valid JSON string (pre-serialized by the Unity client).
   */
  gameData: string;
  /** If true, post is attributed to the logged-in user (requires userGeneratedContent) */
  asUser: boolean;
  /** Required when asUser=true — the user's generated content for Reddit policy compliance */
  userGeneratedContent?: UserGeneratedContent;
};

/** Response from POST /api/create-custom-post */
export type CreateCustomPostResponse = {
  success: boolean;
  /** Full Reddit URL to the created post, e.g. "https://reddit.com/r/sub/comments/..." */
  postUrl?: string;
  message?: string;
};

/**
 * User-generated content required when creating or submitting content as a user.
 * Reddit's content policy requires this for any user-attributed submissions.
 */
export type UserGeneratedContent = {
  /** Text content authored by the user */
  text: string;
  /** Optional image URLs included with the content */
  imageUrls?: string[];
};

// =============================================================================
// Comment Submission
// =============================================================================

/**
 * Request body for POST /api/submit-comment — submits a comment from the game.
 * Used in the challenge flow for challengers to post their score.
 */
export type SubmitCommentRequest = {
  /** The comment text */
  text: string;
  /**
   * The ID of the post or comment to reply to.
   * Post ID (e.g. "t3_abc123") → top-level comment
   * Comment ID (e.g. "t1_def456") → reply to that comment
   */
  replyToId: string;
  /** If true, comment is attributed to the logged-in user */
  asUser: boolean;
};

/** Response from POST /api/submit-comment */
export type SubmitCommentResponse = {
  success: boolean;
  /** Full Reddit URL to the submitted comment */
  commentUrl?: string;
  message?: string;
}
