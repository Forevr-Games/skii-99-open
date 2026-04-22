using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Devvit
{
    /// <summary>
    /// Production implementation of IDevvitService that makes real HTTP requests
    /// to the Devvit Express server (src/server/index.ts).
    ///
    /// Used in WebGL builds only. Replaced by DevvitServiceMock in the Unity Editor.
    /// Selected automatically by DevvitServiceFactory based on compilation symbols.
    ///
    /// Why relative URLs ("/api/init" instead of "https://...")?
    ///   Unity WebGL runs inside an iframe that the Devvit server itself serves.
    ///   Both the HTML/JS and the API endpoints come from the same origin, so
    ///   relative URLs work without any CORS configuration.
    ///
    /// Why coroutines instead of async/await?
    ///   Unity's WebGL platform has limited support for C# async/await with
    ///   UnityWebRequest. Coroutines are Unity's proven pattern for async HTTP
    ///   operations and work reliably across all platforms including WebGL.
    ///   CoroutineRunner provides a persistent MonoBehaviour to host the coroutines
    ///   since this class is a plain C# object (not a MonoBehaviour).
    ///
    /// Request pattern used throughout:
    ///   1. Create UnityWebRequest with the endpoint URL
    ///   2. Serialize request data to JSON with JsonUtility.ToJson()
    ///   3. Set Content-Type header to application/json
    ///   4. yield return request.SendWebRequest() — waits for response
    ///   5. Deserialize response with JsonUtility.FromJson()
    ///   6. Invoke the callback with the result
    /// </summary>
    public class DevvitServiceBuild : IDevvitService
    {
        // Cached from the init response — reused in subsequent requests
        // (e.g., CompleteLevel needs the username to associate the score with)
        private string currentUsername;
        private string currentPostId;

        // Lazy-loaded reference to the RedditLeaderboard MonoBehaviour in the scene.
        // Leaderboard operations are delegated here because they need a MonoBehaviour
        // to run coroutines. Initialized by SaveDataManager.HandleDevvitInitDataReceived().
        private RedditLeaderboard leaderboard;

        // ==========================================================================
        // Initialization
        // ==========================================================================

        public void FetchInitData(Action<DevvitInitData> onComplete)
        {
            CoroutineRunner.Instance.StartCoroutine(FetchInitDataCoroutine(onComplete));
        }

        private IEnumerator FetchInitDataCoroutine(Action<DevvitInitData> onComplete)
        {
            // GET /api/init returns: username, snoovatarUrl, postId, previousScore, rawPostData
            UnityWebRequest request = UnityWebRequest.Get("/api/init");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DevvitServiceBuild] FetchInitData failed: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                DevvitInitData response = JsonUtility.FromJson<DevvitInitData>(request.downloadHandler.text);

                // Cache username and postId — needed for subsequent API calls
                currentUsername = response.username;
                currentPostId = response.postId;

                // Parse rawPostData into the strongly-typed DevvitPostData object.
                // rawPostData is the JSON string embedded when the post was created.
                DevvitInitData data = new DevvitInitData
                {
                    username = response.username,
                    snoovatarUrl = response.snoovatarUrl,
                    postId = response.postId,
                    previousScore = response.previousScore,
                    rawPostData = response.rawPostData,
                    postData = string.IsNullOrEmpty(response.rawPostData)
                        ? null
                        : DevvitPostData.FromJson(response.rawPostData)
                };

                Debug.Log($"[DevvitServiceBuild] FetchInitData: user={data.username}, post={data.postId}, hasPostData={!string.IsNullOrEmpty(data.rawPostData)}");
                onComplete?.Invoke(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevvitServiceBuild] Failed to parse init response: {e.Message}");
                onComplete?.Invoke(null);
            }
        }

        // ==========================================================================
        // Image Download
        // ==========================================================================

        public void DownloadImage(string url, Action<Texture2D> onComplete)
        {
            CoroutineRunner.Instance.StartCoroutine(DownloadImageCoroutine(url, onComplete));
        }

        private IEnumerator DownloadImageCoroutine(string url, Action<Texture2D> onComplete)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[DevvitServiceBuild] DownloadImage: URL is null or empty");
                onComplete?.Invoke(null);
                yield break;
            }

            // UnityWebRequestTexture handles the download and decoding of the image.
            // The URL is the CDN URL returned by getSnoovatarUrl() from the Devvit server.
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DevvitServiceBuild] DownloadImage failed: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                onComplete?.Invoke(texture);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevvitServiceBuild] Failed to process downloaded image: {e.Message}");
                onComplete?.Invoke(null);
            }
        }

        // ==========================================================================
        // Score Persistence
        // ==========================================================================

        public void CompleteLevel(DevvitScoreWithData score, Action<bool> onComplete)
        {
            CoroutineRunner.Instance.StartCoroutine(CompleteLevelCoroutine(score, onComplete));
        }

        private IEnumerator CompleteLevelCoroutine(DevvitScoreWithData score, Action<bool> onComplete)
        {
            UnityWebRequest request = new UnityWebRequest("/api/level-completed", "POST");

            LevelCompletionData data = new LevelCompletionData
            {
                type = "level-completed",
                username = currentUsername,
                postId = currentPostId,
                score = score
            };

            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;

            if (!success)
            {
                Debug.LogError($"[DevvitServiceBuild] CompleteLevel failed: {request.error}");
            }
            else
            {
                try
                {
                    LevelCompletionResponse response = JsonUtility.FromJson<LevelCompletionResponse>(request.downloadHandler.text);
                    success = response.success;

                    if (!success && !string.IsNullOrEmpty(response.message))
                    {
                        Debug.LogWarning($"[DevvitServiceBuild] Server returned success=false: {response.message}");
                    }
                }
                catch (Exception e)
                {
                    // If we can't parse the response but the HTTP request succeeded,
                    // treat it as success — the data was likely saved even if the
                    // response format is unexpected.
                    Debug.LogWarning($"[DevvitServiceBuild] Failed to parse completion response: {e.Message}");
                }
            }

            try
            {
                onComplete?.Invoke(success);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevvitServiceBuild] Error invoking completion callback: {ex.Message}");
            }
        }

        // ==========================================================================
        // Leaderboard (delegated to RedditLeaderboard MonoBehaviour)
        // ==========================================================================
        //
        // Leaderboard operations are delegated to the RedditLeaderboard MonoBehaviour
        // that exists in the scene. This is because coroutines need a living
        // MonoBehaviour — this class is a plain C# object and can't host them directly.
        // RedditLeaderboard is initialized by SaveDataManager after init completes.

        private RedditLeaderboard GetLeaderboard()
        {
            if (leaderboard == null)
            {
                leaderboard = UnityEngine.Object.FindAnyObjectByType<RedditLeaderboard>();
                if (leaderboard == null)
                {
                    Debug.LogError("[DevvitServiceBuild] RedditLeaderboard not found in scene!");
                }
            }
            return leaderboard;
        }

        public void SubmitGameScore(float score, Action<bool, int> onComplete)
        {
            RedditLeaderboard lb = GetLeaderboard();
            if (lb != null)
            {
                lb.SubmitGameScore(score, onComplete);
            }
            else
            {
                Debug.LogError("[DevvitServiceBuild] Cannot submit score - RedditLeaderboard not found");
                onComplete?.Invoke(false, 0);
            }
        }

        public void FetchLeaderboard(Action<LeaderboardResponse> onComplete)
        {
            RedditLeaderboard lb = GetLeaderboard();
            if (lb != null)
            {
                lb.FetchLeaderboard(onComplete);
            }
            else
            {
                Debug.LogError("[DevvitServiceBuild] Cannot fetch leaderboard - RedditLeaderboard not found");
                onComplete?.Invoke(null);
            }
        }

        public void FetchMyRank(Action<UserRankResponse> onComplete)
        {
            RedditLeaderboard lb = GetLeaderboard();
            if (lb != null)
            {
                lb.FetchMyRank(onComplete);
            }
            else
            {
                Debug.LogError("[DevvitServiceBuild] Cannot fetch rank - RedditLeaderboard not found");
                onComplete?.Invoke(null);
            }
        }

        public void FetchUserRank(string username, Action<UserRankResponse> onComplete)
        {
            RedditLeaderboard lb = GetLeaderboard();
            if (lb != null)
            {
                lb.FetchUserRank(username, onComplete);
            }
            else
            {
                Debug.LogError("[DevvitServiceBuild] Cannot fetch user rank - RedditLeaderboard not found");
                onComplete?.Invoke(null);
            }
        }

        // ==========================================================================
        // Custom Post Creation
        // ==========================================================================

        public void CreateCustomPost(CreateCustomPostRequest requestData, Action<bool, string> onComplete)
        {
            CoroutineRunner.Instance.StartCoroutine(CreateCustomPostCoroutine(requestData, onComplete));
        }

        private IEnumerator CreateCustomPostCoroutine(CreateCustomPostRequest requestData, Action<bool, string> onComplete)
        {
            UnityWebRequest request = new UnityWebRequest("/api/create-custom-post", "POST");

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            bool success = false;
            string postUrl = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DevvitServiceBuild] CreateCustomPost failed: {request.error}");
            }
            else
            {
                try
                {
                    CreateCustomPostResponse response = JsonUtility.FromJson<CreateCustomPostResponse>(request.downloadHandler.text);
                    success = response.success;
                    postUrl = response.postUrl;

                    if (!success)
                    {
                        Debug.LogWarning($"[DevvitServiceBuild] CreateCustomPost server error: {response.message}");
                    }
                    else
                    {
                        Debug.Log($"[DevvitServiceBuild] Post created: {postUrl}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DevvitServiceBuild] Failed to parse CreateCustomPost response: {e.Message}");
                }
            }

            try
            {
                onComplete?.Invoke(success, postUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevvitServiceBuild] Error invoking CreateCustomPost callback: {ex.Message}");
            }
        }

        // ==========================================================================
        // Comment Submission
        // ==========================================================================

        public void SubmitComment(SubmitCommentRequest requestData, Action<SubmitCommentResponse> onComplete)
        {
            CoroutineRunner.Instance.StartCoroutine(SubmitCommentCoroutine(requestData, onComplete));
        }

        private IEnumerator SubmitCommentCoroutine(SubmitCommentRequest requestData, Action<SubmitCommentResponse> onComplete)
        {
            UnityWebRequest request = new UnityWebRequest("/api/submit-comment", "POST");

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            SubmitCommentResponse response = new SubmitCommentResponse { success = false };

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DevvitServiceBuild] SubmitComment failed: {request.error}");
                response.message = request.error;
            }
            else
            {
                try
                {
                    response = JsonUtility.FromJson<SubmitCommentResponse>(request.downloadHandler.text);
                    if (!response.success)
                    {
                        Debug.LogWarning($"[DevvitServiceBuild] SubmitComment server error: {response.message}");
                    }
                    else
                    {
                        Debug.Log($"[DevvitServiceBuild] Comment submitted: {response.commentUrl}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DevvitServiceBuild] Failed to parse SubmitComment response: {e.Message}");
                    response.success = false;
                    response.message = "Failed to parse server response";
                }
            }

            try
            {
                onComplete?.Invoke(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevvitServiceBuild] Error invoking SubmitComment callback: {ex.Message}");
            }
        }

        // ==========================================================================
        // URL Opening
        // ==========================================================================

        [Serializable]
        private class OpenUrlRequest
        {
            public string url;
        }

        public void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[DevvitServiceBuild] OpenUrl called with empty URL");
                return;
            }

            Debug.Log($"[DevvitServiceBuild] Opening URL via Devvit: {url}");
            CoroutineRunner.Instance.StartCoroutine(OpenUrlCoroutine(url));
        }

        private IEnumerator OpenUrlCoroutine(string url)
        {
            // POST /api/open-url — server responds with { navigateTo: url }
            // The Devvit client framework intercepts this response and opens the URL
            // in the parent window (outside the sandboxed WebGL iframe).
            // This is the workaround for Application.OpenURL not working in WebGL/Devvit.
            UnityWebRequest request = new UnityWebRequest("/api/open-url", "POST");

            OpenUrlRequest data = new OpenUrlRequest { url = url };

            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DevvitServiceBuild] OpenUrl failed: {request.error}");
            }
        }
    }
}
