/**
 * Devvit Web Server — Main API Routes
 *
 * This file is the Express server that runs inside the Devvit runtime.
 * It handles all communication between the Unity WebGL game (running in the
 * browser) and Reddit's platform (users, posts, comments, Redis storage).
 *
 * How Devvit Web works:
 *   - When a Reddit user opens a post that uses this app, Devvit serves both
 *     the client HTML/JS (src/client/) and this server from the same origin.
 *   - Unity makes HTTP requests to relative URLs like "/api/init" — these
 *     are routed here by the Devvit runtime.
 *   - The `context` object (imported below) is automatically injected per-request
 *     by Devvit and contains the current post/user context without any manual
 *     session management.
 *
 * Key imports from @devvit/web/server:
 *   - `context`  — Per-request context: postId, userId, subredditName, postData
 *   - `reddit`   — Reddit API client (pre-authenticated, no tokens needed)
 *   - `redis`    — Devvit's Redis client (scoped to this app's data)
 *   - `createServer` / `getServerPort` — Devvit-managed server lifecycle
 */
import express from 'express';
import {
  DevvitInitData,
  LevelCompletionData,
  LevelCompletionResponse,
  DailyGameCompletedRequest,
  DailyGameCompletedResponse,
  LeaderboardResponse,
  UserRankResponse,
  parseDevvitScoreData,
  CreateCustomPostResponse,
  CreateCustomPostRequest,
  SubmitCommentResponse,
  SubmitCommentRequest,
} from '../shared/types/api';
import { createServer, context, getServerPort, reddit, redis } from '@devvit/web/server';
import { createPost } from './core/post';
import { registerCreateQuiz } from './menu/create-quiz';
import { registerCreateImageWarehouse } from './menu/create-image-warehouse';
import { registerCreateTestPost } from './menu/create-test-post';
import { registerCreateCustomPost } from './menu/create-custom-post';
import { submitComment } from './core/comment';

const app = express();

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.text());

const router = express.Router();

// =============================================================================
// /api/init — Game Initialization
// =============================================================================
//
// This is the FIRST request the Unity game makes when it loads. It seeds the
// game with everything needed for this session:
//   - Who the player is (Reddit username)
//   - Their snoovatar URL (fetched server-side because it requires auth)
//   - Their best previous score for THIS post (stored in Redis)
//   - Any custom data embedded in the post when it was created (e.g. challenge data)
//
// Why fetch the snoovatar server-side?
//   Reddit's user API requires authentication. The Devvit server is already
//   authenticated as the app, so we fetch the avatar here and pass the URL
//   to Unity. Unity then downloads the image directly from the CDN URL.
//
// Redis key format: "postId:username"
//   Each post has its own score namespace so the same user can have different
//   best scores on different posts (e.g., different challenge posts).
//
router.get<{ postId: string; }, DevvitInitData | { status: string; message: string; }>(
  '/api/init',
  async (_req, res): Promise<void> => {
    const { postId } = context;

    if (!postId) {
      console.error('API Init Error: postId not found in devvit context');
      res.status(400).json({
        status: 'error',
        message: 'postId is required but missing from context',
      });
      return;
    }

    try {
      // reddit.getCurrentUsername() returns the Reddit username of the user
      // who opened this post. Returns undefined for logged-out users.
      const username = await reddit.getCurrentUsername();
      const currentUsername = username ?? 'anonymous';

      // Fetch the user's snoovatar URL.
      // context.userId is the internal Reddit user ID (e.g. "t2_abc123").
      // We need getUserById() rather than getUserByUsername() because
      // getSnoovatarUrl() is only available on the User object from getUserById().
      let snoovatarUrl = '';
      if (username && context.userId) {
        const user = await reddit.getUserById(context.userId);
        if (user) {
          snoovatarUrl = (await user.getSnoovatarUrl()) ?? '';
        }
      }

      // Retrieve the user's previous best score for this post from Redis.
      // Scores are serialized as "score;extraData" strings (see DevvitScoreData
      // in shared/types/api.ts). parseDevvitScoreData converts back to an object.
      const redisKey = `${postId}:${currentUsername}`;
      const previousScorestring = await redis.get(redisKey);

      // context.postData contains arbitrary JSON that was embedded in the post
      // when it was created via submitCustomPost({ postData: {...} }).
      // This is how challenge data (original player's score, author, etc.) flows
      // from one player's share action to the next player's game session.
      const postDataJson = context.postData ? JSON.stringify(context.postData) : undefined;

      const response: DevvitInitData = {
        type: 'init',
        postId: postId,
        username: currentUsername,
        snoovatarUrl: snoovatarUrl,
        previousScore: parseDevvitScoreData(previousScorestring),
      };

      if (postDataJson !== undefined) {
        response.rawPostData = postDataJson;
      }

      console.log(`[/api/init] Initialized for user: ${currentUsername}, postId: ${postId}`);
      res.json(response);
    } catch (error) {
      console.error(`API Init Error for post ${postId}:`, error);
      let errorMessage = 'Unknown error during initialization';
      if (error instanceof Error) {
        errorMessage = `Initialization failed: ${error.message}`;
      }
      res.status(400).json({ status: 'error', message: errorMessage });
    }
  }
);

// =============================================================================
// /api/level-completed — Save Per-Session Score to Redis
// =============================================================================
//
// Saves a user's score after a game session. This persists the "previous score"
// that will be shown when they return to this post.
//
// Storage strategy: simple key-value
//   Key:   "postId:username"  (namespaced per post so scores don't bleed across posts)
//   Value: "score;extraData"  (serialized DevvitScoreData — see shared/types/api.ts)
//
// Note: This endpoint saves the raw session score, not the leaderboard score.
// For leaderboard ranking, see /api/daily-game-completed below.
//
router.post<
  unknown,
  LevelCompletionResponse | { status: string; message: string; },
  LevelCompletionData
>('/api/level-completed', async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    res.status(400).json({ status: 'error', message: 'postId is required' });
    return;
  }

  try {
    const { username, score: scoreData } = req.body;

    if (!username || !scoreData) {
      res.status(400).json({ status: 'error', message: 'username and scoreData are required' });
      return;
    }

    // Store as a string — Redis stores all values as strings, and our
    // DevvitScoreData serialization format handles encoding score + extraData.
    const redisKey = `${postId}:${username}`;
    await redis.set(redisKey, scoreData.toString());

    res.json({
      type: 'level-completed',
      success: true,
      message: 'ScoreData saved successfully',
    });
  } catch (error) {
    console.error(`API Level Completed Error for post ${postId}:`, error);
    let errorMessage = 'Unknown error saving completion scoreData';
    if (error instanceof Error) {
      errorMessage = `Failed to save scoreData: ${error.message}`;
    }
    res.status(500).json({
      type: 'level-completed',
      success: false,
      message: errorMessage,
    });
  }
});

// =============================================================================
// /api/daily-game-completed — Submit Score to Leaderboard
// =============================================================================
//
// Submits a score to the post's leaderboard and returns the player's rank.
// Only updates the leaderboard if the new score is higher than the existing one
// (the server handles "best score wins" logic, not the client).
//
// Why Redis Sorted Sets?
//   Redis sorted sets (ZADD/ZRANGE/ZRANK) are built for exactly this use case:
//   - ZADD: insert or update a member's score in O(log n)
//   - ZRANGE with BYSCORE REV: fetch top N players in descending score order
//   - ZRANK: get a member's position (0-based, ascending) in O(log n)
//   - ZCARD: count total members in O(1)
//
// Leaderboard key format: "leaderboard:postId"
//   Scoped per post — each challenge post has its own leaderboard.
//
// Rank calculation:
//   Redis ZRANK returns the 0-based ascending index (lowest score = 0).
//   We invert this to get a rank where higher scores = better rank:
//     rank = totalPlayers - ascendingRank
//   Example: 5 players total, user is at ascending index 4 (highest score)
//     rank = 5 - 4 = 1  ← rank #1 (first place)
//
router.post<
  unknown,
  DailyGameCompletedResponse | { status: string; message: string; },
  DailyGameCompletedRequest
>('/api/daily-game-completed', async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    res.status(400).json({ status: 'error', message: 'postId is required' });
    return;
  }

  try {
    const { username, score } = req.body;

    if (!username || score === undefined) {
      res.status(400).json({ status: 'error', message: 'username and score are required' });
      return;
    }

    const leaderboardKey = `leaderboard:${postId}`;
    const scoreKey = `score:${postId}:${username}`;

    // Check whether the user already has a score on this leaderboard.
    // zScore returns undefined if the member does not exist in the sorted set.
    const existingScore = await redis.zScore(leaderboardKey, username);

    if (existingScore === undefined || score > existingScore) {
      // New high score — update both the individual score key and the sorted set.
      await redis.set(scoreKey, score.toString());

      // zAdd inserts the member if new, or updates the score if it already exists.
      // The 'score' field here is the numeric value Redis uses for ordering.
      await redis.zAdd(leaderboardKey, {
        member: username,
        score: score,
      });

      // Calculate rank: ZRANK returns ascending index (0 = lowest score).
      // We invert it so rank #1 = highest score.
      const totalPlayers = await redis.zCard(leaderboardKey);
      const ascendingRank = await redis.zRank(leaderboardKey, username);

      const response: DailyGameCompletedResponse = {
        type: 'daily-game-completed',
        success: true,
        message: 'New high score saved successfully!',
      };

      if (ascendingRank !== undefined) {
        response.rank = totalPlayers - ascendingRank;
      }

      res.json(response);
    } else {
      // Score is not a new high — don't update the leaderboard, but still
      // return the user's current rank so the game can display it.
      const totalPlayers = await redis.zCard(leaderboardKey);
      const ascendingRank = await redis.zRank(leaderboardKey, username);

      const response: DailyGameCompletedResponse = {
        type: 'daily-game-completed',
        success: true,
        message: `Score ${score} not saved. Your best score is ${existingScore}.`,
      };

      if (ascendingRank !== undefined) {
        response.rank = totalPlayers - ascendingRank;
      }

      res.json(response);
    }
  } catch (error) {
    console.error(`API Daily Game Completed Error for post ${postId}:`, error);
    let errorMessage = 'Unknown error saving score';
    if (error instanceof Error) {
      errorMessage = `Failed to save score: ${error.message}`;
    }
    res.status(500).json({
      type: 'daily-game-completed',
      success: false,
      message: errorMessage,
    });
  }
});

// =============================================================================
// /api/leaderboard/:postId — Fetch Top 10 Players
// =============================================================================
//
// Returns the top 10 players for a post, including their snoovatar URLs.
//
// Why fetch avatars here instead of in /api/init?
//   The leaderboard shows multiple users' avatars simultaneously. Fetching
//   them on-demand here (with Promise.all for parallelism) is more efficient
//   than pre-fetching 10 avatars during init.
//
// Note: Avatar fetching is best-effort — if a user's avatar can't be fetched,
//   we return an empty string and the game shows a fallback avatar.
//
router.get<{ postId: string; }, LeaderboardResponse | { status: string; message: string; }>(
  '/api/leaderboard/:postId',
  async (req, res): Promise<void> => {
    const { postId } = req.params;

    try {
      const leaderboardKey = `leaderboard:${postId}`;

      // zRange with reverse:true returns members in descending score order.
      // Indices 0–9 give us the top 10.
      const topPlayers = await redis.zRange(leaderboardKey, 0, 9, {
        by: 'rank',
        reverse: true,
      });

      // Fetch avatars for all top players in parallel.
      const entries = await Promise.all(
        topPlayers.map(async (player, index) => {
          let snoovatarUrl = '';
          try {
            const user = await reddit.getUserByUsername(player.member);
            if (user) {
              snoovatarUrl = (await user.getSnoovatarUrl()) ?? '';
            }
          } catch (e) {
            // Non-fatal — game shows a fallback avatar if this fails
            console.error(`Could not fetch avatar for ${player.member}`);
          }

          return {
            rank: index + 1,
            username: player.member,
            score: player.score,
            snoovatarUrl,
          };
        })
      );

      const totalPlayers = await redis.zCard(leaderboardKey);

      res.json({
        type: 'leaderboard',
        postId,
        entries,
        totalPlayers,
      });
    } catch (error) {
      console.error(`Leaderboard fetch error:`, error);
      res.status(500).json({
        status: 'error',
        message: 'Failed to fetch leaderboard',
      });
    }
  }
);

// =============================================================================
// /api/leaderboard/:postId/user/:username — Fetch a Specific User's Rank
// =============================================================================
//
// Returns a specific user's rank and score on a post's leaderboard.
// Used to show the current user's rank after they submit a score.
//
router.get<
  { postId: string; username: string; },
  UserRankResponse | { status: string; message: string; }
>('/api/leaderboard/:postId/user/:username', async (req, res): Promise<void> => {
  const { postId, username } = req.params;

  try {
    const leaderboardKey = `leaderboard:${postId}`;

    // zRank returns the 0-based ascending index, or undefined if user not found.
    const ascendingRank = await redis.zRank(leaderboardKey, username);

    if (ascendingRank === undefined) {
      res.json({
        type: 'user-rank',
        username,
        postId,
        ranked: false,
        message: 'User has not completed this game',
      });
      return;
    }

    const score = await redis.zScore(leaderboardKey, username);

    // Invert ascending rank to get descending rank (higher score = lower number = better rank)
    const totalPlayers = await redis.zCard(leaderboardKey);
    const rank = totalPlayers - ascendingRank;

    const response: UserRankResponse = {
      type: 'user-rank',
      username,
      postId,
      ranked: true,
      rank,
      isTopTen: rank <= 10,
    };

    if (score !== undefined) {
      response.score = score;
    }

    res.json(response);
  } catch (error) {
    console.error(`User rank fetch error:`, error);
    res.status(500).json({
      status: 'error',
      message: 'Failed to fetch user rank',
    });
  }
});

// =============================================================================
// /internal/on-app-install — App Installation Lifecycle Hook
// =============================================================================
//
// Devvit calls this endpoint automatically when the app is installed on a
// subreddit (via the Devvit CLI or Reddit's developer portal).
//
// We use it to automatically create the first game post so moderators don't
// have to manually create one after installing the app.
//
// This is an internal route (not called by Unity) — the "/internal/" prefix
// is a convention to signal that it's a Devvit lifecycle endpoint.
//
router.post('/internal/on-app-install', async (_req, res): Promise<void> => {
  try {
    const post = await createPost('Ski99');

    console.log(`[on-app-install] Post created in ${context.subredditName} with id ${post.id}`);

    // Devvit validates /internal/ responses against UiResponse — must use
    // showToast / navigateTo / showForm; plain { status, message } is rejected.
    res.json({
      showToast: `Ski99 post created! (${post.id})`,
    });
  } catch (error) {
    console.error(`Error creating post: ${error}`);
    res.json({
      showToast: 'Failed to create post — check server logs.',
    });
  }
});

// =============================================================================
// /api/open-url — Open a URL from Unity
// =============================================================================
//
// Unity's Application.OpenURL() does NOT work inside Devvit's WebGL sandbox
// because the game runs in a sandboxed <iframe> without permission to navigate
// the parent window.
//
// Workaround: Unity sends the URL to this server endpoint. The server responds
// with `{ navigateTo: url }`, which the Devvit client intercepts and uses to
// trigger navigation in the parent window — outside the sandbox.
//
// This pattern is specific to Devvit's web framework and is the official way
// to open external links from within a custom post.
//
router.post('/api/open-url', async (req, res): Promise<void> => {
  const { url } = req.body;

  if (!url) {
    console.error('[/api/open-url] Missing url');
    res.status(400).json({ success: false });
    return;
  }

  console.log(`[/api/open-url] Opening URL: ${url}`);

  // The Devvit client automatically handles any response containing `navigateTo`
  // and opens the URL in a new tab (or navigates the current page, depending
  // on the Devvit version and context).
  res.json({
    navigateTo: url,
  });
});

// =============================================================================
// /api/create-custom-post — Create a New Reddit Post from Unity
// =============================================================================
//
// Allows the Unity game to create a new custom Reddit post. Used for the
// "challenge post" viral loop: after a good run, the player can share their
// score as a new post that others can try to beat.
//
// The `gameData` field is a JSON string embedded in the post via `postData`.
// When another user opens that post, `context.postData` contains this data,
// allowing the game to display the original player's score as a challenge target.
//
// See core/post.ts for the submitCustomPost implementation details.
//
router.post<
  unknown,
  CreateCustomPostResponse | { status: string; message: string; },
  CreateCustomPostRequest
>('/api/create-custom-post', async (req, res): Promise<void> => {
  try {
    const { title, gameData, asUser, userGeneratedContent } = req.body;

    if (!title || !gameData) {
      res.status(400).json({
        success: false,
        message: 'title and gameData are required',
      });
      return;
    }

    // gameData arrives as a JSON string (Unity serialized it with JsonUtility.ToJson).
    // We need to parse it back to an object before passing it to submitCustomPost,
    // because postData must be an object — not a string.
    let parsedGameData;
    try {
      parsedGameData = JSON.parse(gameData);
    } catch (parseError) {
      res.status(400).json({
        success: false,
        message: `Invalid JSON in gameData: ${parseError instanceof Error ? parseError.message : 'Unknown error'}`,
      });
      return;
    }

    const runAsUser = asUser !== undefined ? asUser : false;
    const post = await createPost(title, runAsUser, userGeneratedContent, parsedGameData);
    const postUrl = `https://reddit.com/r/${context.subredditName}/comments/${post.id}`;

    console.log(`[/api/create-custom-post] Post created: ${postUrl}`);

    res.json({
      success: true,
      postUrl: postUrl,
    });
  } catch (error) {
    console.error('[/api/create-custom-post] Error:', error);
    res.status(500).json({
      success: false,
      message: `Failed to create post: ${error instanceof Error ? error.message : 'Unknown error'}`,
    });
  }
});

// =============================================================================
// /api/submit-comment — Submit a Comment from Unity
// =============================================================================
//
// Allows the Unity game to post a comment on a Reddit post or reply to an
// existing comment. Used in the challenge post flow: when a challenger beats
// the original player's score, their result is posted as a comment on the
// original challenge post.
//
// `replyToId` can be either:
//   - A post ID (e.g. "t3_abc123") — creates a top-level comment
//   - A comment ID (e.g. "t1_def456") — creates a reply to that comment
//
// The sticky comment pattern: when a challenge post is created, the app
// immediately posts a stickied comment (as APP) with the formatted score.
// Challengers reply to this comment, keeping all challenge responses threaded.
//
// See core/comment.ts for the submitComment implementation details.
//
router.post<
  unknown,
  SubmitCommentResponse | { status: string; message: string; },
  SubmitCommentRequest
>('/api/submit-comment', async (req, res): Promise<void> => {
  try {
    const { text, replyToId, asUser } = req.body;

    if (!text || !replyToId) {
      res.status(400).json({
        success: false,
        message: 'text and replyToId are required',
      });
      return;
    }

    const runAsUser = asUser !== undefined ? asUser : false;
    const comment = await submitComment(text, replyToId, runAsUser);

    console.log(`[/api/submit-comment] Comment submitted: ${comment.url}`);

    res.json({
      success: true,
      commentUrl: comment.url,
    });
  } catch (error) {
    console.error('[/api/submit-comment] Error:', error);
    res.status(500).json({
      success: false,
      message: `Failed to submit comment: ${error instanceof Error ? error.message : 'Unknown error'}`,
    });
  }
});

// =============================================================================
// Supplementary Devvit Menu Action Examples
// =============================================================================
// These register additional Reddit menu actions (right-click on posts/subreddit).
// They are not required for the core game — see src/server/menu/README.md
// for details on what each one demonstrates.
registerCreateQuiz(router);
registerCreateCustomPost(router);
registerCreateTestPost(router);
registerCreateImageWarehouse(router);

app.use(router);

const server = createServer(app);
server.on('error', (err) => console.error(`server error; ${err.stack}`));
server.listen(getServerPort());
