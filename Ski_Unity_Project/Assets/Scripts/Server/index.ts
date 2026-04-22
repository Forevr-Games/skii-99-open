import express from "express";
import {
  InitResponse,
  SaveHighScoreRequest,
  SaveHighScoreResponse,
  LevelCompletedRequest,
  LevelCompletedResponse,
  DailyGameCompletedRequest,
  DailyGameCompletedResponse,
  LeaderboardResponse,
  UserRankResponse,
} from "../shared/types/api";
import {
  createServer,
  context,
  getServerPort,
  reddit,
  redis,
} from "@devvit/web/server";
import { createPost } from "./core/post";

const app = express();

// Middleware for JSON body parsing
app.use(express.json());
// Middleware for URL-encoded body parsing
app.use(express.urlencoded({ extended: true }));
// Middleware for plain text body parsing
app.use(express.text());

const router = express.Router();

// Example to show how to send initial data to the Unity Game
router.get<
  { postId: string },
  InitResponse | { status: string; message: string }
>("/api/init", async (_req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("API Init Error: postId not found in devvit context");
    res.status(400).json({
      status: "error",
      message: "postId is required but missing from context",
    });
    return;
  }

  try {
    const username = await reddit.getCurrentUsername();
    const currentUsername = username ?? "anonymous";

    // Fetch user info for snoovatar
    let snoovatarUrl = "";
    if (username && context.userId) {
      const user = await reddit.getUserById(context.userId);
      if (user) {
        snoovatarUrl = (await user.getSnoovatarUrl()) ?? "";
      }
    }

    // Fetch high score and furthest distance
    const highScoreKey = `${postId}:${currentUsername}:highScore`;
    const distanceKey = `${postId}:${currentUsername}:furthestDistance`;
    const highScore = await redis.get(highScoreKey);
    const furthestDistance = await redis.get(distanceKey);

    res.json({
      type: "init",
      postId: postId,
      username: currentUsername,
      snoovatarUrl: snoovatarUrl,
      highScore: highScore ?? "",
      furthestDistance: furthestDistance ?? "",
    });
  } catch (error) {
    console.error(`API Init Error for post ${postId}:`, error);
    let errorMessage = "Unknown error during initialization";
    if (error instanceof Error) {
      errorMessage = `Initialization failed: ${error.message}`;
    }
    res.status(400).json({ status: "error", message: errorMessage });
  }
});

router.post<
  unknown,
  SaveHighScoreResponse | { status: string; message: string },
  SaveHighScoreRequest
>("/api/save-high-score", async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("No postId in context");
    res.status(400).json({
      status: "error",
      message: "postId is required",
    });
    return;
  }

  try {
    const { username, score, distance } = req.body;

    if (!username || !score || !distance) {
      console.error("Missing required fields in request");
      res.status(400).json({
        status: "error",
        message: "username, score, and distance are required",
      });
      return;
    }

    let isNewHighScore = false;
    let isNewDistance = false;

    // Check and update high score if this score is better
    const highScoreKey = `${postId}:${username}:highScore`;
    const existingHighScore = await redis.get(highScoreKey);
    const existingScoreFloat = existingHighScore ? parseFloat(existingHighScore) : 0;
    const newScoreFloat = parseFloat(score);

    if (newScoreFloat > existingScoreFloat) {
      await redis.set(highScoreKey, score);
      isNewHighScore = true;
    }

    // Check and update furthest distance if this distance is better
    const distanceKey = `${postId}:${username}:furthestDistance`;
    const existingDistance = await redis.get(distanceKey);
    const existingDistanceFloat = existingDistance ? parseFloat(existingDistance) : 0;
    const newDistanceFloat = parseFloat(distance);

    if (newDistanceFloat > existingDistanceFloat) {
      await redis.set(distanceKey, distance);
      isNewDistance = true;
    }

    res.json({
      type: "save-high-score",
      success: true,
      isNewHighScore,
      isNewDistance,
      message: isNewHighScore || isNewDistance
        ? "New record(s) saved!"
        : "No new records",
    });
  } catch (error) {
    console.error(`API Save High Score Error for post ${postId}:`, error);
    let errorMessage = "Unknown error saving high score";
    if (error instanceof Error) {
      errorMessage = `Failed to save high score: ${error.message}`;
    }
    res.status(500).json({
      type: "save-high-score",
      success: false,
      isNewHighScore: false,
      isNewDistance: false,
      message: errorMessage,
    });
  }
});

router.post<
  unknown,
  LevelCompletedResponse | { status: string; message: string },
  LevelCompletedRequest
>("/api/level-completed", async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("No postId in context");
    res.status(400).json({
      status: "error",
      message: "postId is required",
    });
    return;
  }

  try {
    const { username, time } = req.body;

    if (!username || !time) {
      console.error("Missing username or time in request");
      res.status(400).json({
        status: "error",
        message: "username and time are required",
      });
      return;
    }

    // Store the completion time in Redis with key format: postId:username
    const redisKey = `${postId}:${username}`;
    await redis.set(redisKey, time);

    res.json({
      type: "level-completed",
      success: true,
      message: "Time saved successfully",
    });
  } catch (error) {
    console.error(`API Level Completed Error for post ${postId}:`, error);
    let errorMessage = "Unknown error saving completion time";
    if (error instanceof Error) {
      errorMessage = `Failed to save time: ${error.message}`;
    }
    res.status(500).json({
      type: "level-completed",
      success: false,
      message: errorMessage,
    });
  }
});

router.post<
  unknown,
  DailyGameCompletedResponse | { status: string; message: string },
  DailyGameCompletedRequest
>("/api/daily-game-completed", async (req, res): Promise<void> => {
  const { postId } = context;

  if (!postId) {
    console.error("No postId in context");
    res.status(400).json({
      status: "error",
      message: "postId is required",
    });
    return;
  }

  try {
    const { username, score } = req.body;

    if (!username || score === undefined || score === null) {
      console.error("Missing username or score in request");
      res.status(400).json({
        status: "error",
        message: "username and score are required",
      });
      return;
    }

    const leaderboardKey = `leaderboard:${postId}`;
    const scoreKey = `score:${postId}:${username}`;

    // Check if user already has a score
    const existingScore = await redis.zScore(leaderboardKey, username);

    // Only update if new score is higher (or if no existing score)
    if (existingScore === undefined || score > existingScore) {
      // Store individual user's score
      await redis.set(scoreKey, score.toString());

      // Add/update score in sorted set (higher score is better)
      await redis.zAdd(leaderboardKey, {
        member: username,
        score: score
      });

      // Get user's rank (calculate reverse rank: total_count - rank - 1)
      // Since higher score is better, we need reverse ranking
      const totalPlayers = await redis.zCard(leaderboardKey);
      const ascendingRank = await redis.zRank(leaderboardKey, username);

      const response: DailyGameCompletedResponse = {
        type: "daily-game-completed",
        success: true,
        message: "New high score saved successfully!",
      };

      if (ascendingRank !== undefined) {
        response.rank = totalPlayers - ascendingRank;
      }

      res.json(response);
    } else {
      // Score is lower or equal, don't update
      const totalPlayers = await redis.zCard(leaderboardKey);
      const ascendingRank = await redis.zRank(leaderboardKey, username);

      const response: DailyGameCompletedResponse = {
        type: "daily-game-completed",
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
    let errorMessage = "Unknown error saving score";
    if (error instanceof Error) {
      errorMessage = `Failed to save score: ${error.message}`;
    }
    res.status(500).json({
      type: "daily-game-completed",
      success: false,
      message: errorMessage,
    });
  }
});

router.get<
  { postId: string },
  LeaderboardResponse | { status: string; message: string }
>("/api/leaderboard/:postId", async (req, res): Promise<void> => {
  const { postId } = req.params;

  try {
    const leaderboardKey = `leaderboard:${postId}`;

    // Get top 10 players (reverse order for highest scores first)
    const topPlayers = await redis.zRange(leaderboardKey, 0, 9, {
      by: 'rank',
      reverse: true
    });

    // Transform into leaderboard entries with avatars
    const entries = await Promise.all(
      topPlayers.map(async (player, index) => {
        let snoovatarUrl = "";
        try {
          const user = await reddit.getUserByUsername(player.member);
          if (user) {
            snoovatarUrl = (await user.getSnoovatarUrl()) ?? "";
          }
        } catch (e) {
          console.error(`Could not fetch avatar for ${player.member}`);
        }

        return {
          rank: index + 1,
          username: player.member,
          score: player.score,
          snoovatarUrl
        };
      })
    );

    const totalPlayers = await redis.zCard(leaderboardKey);

    res.json({
      type: "leaderboard",
      postId,
      entries,
      totalPlayers
    });
  } catch (error) {
    console.error(`Leaderboard fetch error:`, error);
    res.status(500).json({
      status: "error",
      message: "Failed to fetch leaderboard"
    });
  }
});

router.get<
  { postId: string; username: string },
  UserRankResponse | { status: string; message: string }
>("/api/leaderboard/:postId/user/:username", async (req, res): Promise<void> => {
  const { postId, username } = req.params;

  try {
    const leaderboardKey = `leaderboard:${postId}`;

    // Get user's rank (ascending)
    const ascendingRank = await redis.zRank(leaderboardKey, username);

    if (ascendingRank === undefined) {
      res.json({
        type: "user-rank",
        username,
        postId,
        ranked: false,
        message: "User has not completed this game"
      });
      return;
    }

    // Get user's score
    const score = await redis.zScore(leaderboardKey, username);

    // Calculate reverse rank (higher score = better rank)
    const totalPlayers = await redis.zCard(leaderboardKey);
    const rank = totalPlayers - ascendingRank;

    const response: UserRankResponse = {
      type: "user-rank",
      username,
      postId,
      ranked: true,
      rank,
      isTopTen: rank <= 10
    };

    if (score !== undefined) {
      response.score = score;
    }

    res.json(response);
  } catch (error) {
    console.error(`User rank fetch error:`, error);
    res.status(500).json({
      status: "error",
      message: "Failed to fetch user rank"
    });
  }
});

router.post('/internal/on-app-install', async (_req, res): Promise<void> => {
  try {
    const post = await createPost();

    res.json({
      status: 'success',
      message: `Post created in subreddit ${context.subredditName} with id ${post.id}`,
    });
  } catch (error) {
    console.error(`Error creating post: ${error}`);
    res.status(400).json({
      status: 'error',
      message: 'Failed to create post',
    });
  }
});

router.post('/internal/menu/post-create', async (_req, res): Promise<void> => {
  try {
    const post = await createPost();
    post

    res.json({
      navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
    });
  } catch (error) {
    console.error(`Error creating post: ${error}`);
    res.status(400).json({
      status: 'error',
      message: 'Failed to create post',
    });
  }
});

app.use(router);

const server = createServer(app);
server.on("error", (err) => console.error(`server error; ${err.stack}`));
server.listen(getServerPort());
