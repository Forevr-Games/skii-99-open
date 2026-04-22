export type InitResponse = {
  type: "init";
  postId: string;
  username: string;
  snoovatarUrl: string;
  highScore: string; // empty string if no high score exists
  furthestDistance: string; // empty string if no furthest distance exists
};

export type SaveHighScoreRequest = {
  type: "save-high-score";
  username: string;
  postId: string;
  score: string;
  distance: string;
};

export type SaveHighScoreResponse = {
  type: "save-high-score";
  success: boolean;
  isNewHighScore: boolean;
  isNewDistance: boolean;
  message?: string;
};

export type LevelCompletedRequest = {
  type: "level-completed";
  username: string;
  postId: string;
  time: string;
};

export type LevelCompletedResponse = {
  type: "level-completed";
  success: boolean;
  message?: string;
};

export type DailyGameCompletedRequest = {
  type: "daily-game-completed";
  username: string;
  postId: string;
  score: number;
};

export type DailyGameCompletedResponse = {
  type: "daily-game-completed";
  success: boolean;
  message?: string;
  rank?: number;
};

export type LeaderboardEntry = {
  rank: number;
  username: string;
  score: number;
  snoovatarUrl: string;
};

export type LeaderboardResponse = {
  type: "leaderboard";
  postId: string;
  entries: LeaderboardEntry[];
  totalPlayers: number;
};

export type UserRankResponse = {
  type: "user-rank";
  username: string;
  postId: string;
  ranked: boolean;
  rank?: number;
  score?: number;
  isTopTen?: boolean;
  message?: string;
};