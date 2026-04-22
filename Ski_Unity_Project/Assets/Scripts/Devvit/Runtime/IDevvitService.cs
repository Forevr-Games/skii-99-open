using System;
using UnityEngine;

namespace Devvit
{
    /// <summary>
    /// Interface for Devvit Reddit API bridge services.
    /// Provides methods for fetching initialization data, downloading images, and sending completion events.
    /// </summary>
    public interface IDevvitService
    {
        /// <summary>
        /// Fetches initial game data from the Devvit backend, including user info, post data, and previous session data.
        /// </summary>
        /// <param name="onComplete">Callback invoked with DevvitInitData on success, or null on failure.</param>
        void FetchInitData(Action<DevvitInitData> onComplete);

        /// <summary>
        /// Downloads an image from a URL and returns it as a Texture2D.
        /// Useful for fetching user avatars, post images, etc.
        /// </summary>
        /// <param name="url">The URL of the image to download.</param>
        /// <param name="onComplete">Callback invoked with Texture2D on success, or null on failure.</param>
        void DownloadImage(string url, Action<Texture2D> onComplete);

        /// <summary>
        /// Sends level completion time to the backend for persistence.
        /// </summary>
        /// <param name="score">The score to submit (format defined by the game, e.g., a float representing time or points).</param>
        /// <param name="onComplete">Callback invoked with true on success, false on failure.</param>
        void CompleteLevel(DevvitScoreWithData score, Action<bool> onComplete);

        /// <summary>
        /// Submits a game score to the leaderboard.
        /// </summary>
        /// <param name="score"> The score to submit (format defined by the game, e.g., a float representing time or points).</param>
        /// <param name="onComplete">Callback with (success, rank)</param>
        void SubmitGameScore(float score, Action<bool, int> onComplete);

        /// <summary>
        /// Fetches the top leaderboard entries.
        /// </summary>
        /// <param name="onComplete">Callback with leaderboard response</param>
        void FetchLeaderboard(Action<LeaderboardResponse> onComplete);

        /// <summary>
        /// Fetches the current user's rank.
        /// </summary>
        /// <param name="onComplete">Callback with user rank response</param>
        void FetchMyRank(Action<UserRankResponse> onComplete);

        /// <summary>
        /// Fetches a specific user's rank.
        /// </summary>
        /// <param name="username">The username to look up</param>
        /// <param name="onComplete">Callback with user rank response</param>
        void FetchUserRank(string username, Action<UserRankResponse> onComplete);

        /// <summary>
        /// Creates a custom Reddit post with the given title and game data.
        /// This is a generic method that accepts any JSON payload.
        /// </summary>
        /// <param name="title">The post title</param>
        /// <param name="gameData">The game data as a JSON string (already stringified)</param>
        /// <param name="onComplete">Callback with (success, postUrl) - postUrl is null on failure</param>
        void CreateCustomPost(CreateCustomPostRequest requestData, Action<bool, string> onComplete);


        /// <summary>
        /// Submits a comment to a Reddit post.
        /// </summary>
        /// <param name="requestData">The comment data, including postId, comment text, and optional game data.</param>
        /// <param name="onComplete">Callback with (success, commentUrl) - commentUrl is null on failure</param>
        void SubmitComment(SubmitCommentRequest requestData, Action<SubmitCommentResponse> onComplete);

        /// <summary>
        /// Opens a URL in the browser. In WebGL builds, this routes through the TypeScript
        /// layer since Application.OpenURL doesn't work in the sandboxed environment.
        /// </summary>
        /// <param name="url">The URL to open</param>
        void OpenUrl(string url);
    }
}
