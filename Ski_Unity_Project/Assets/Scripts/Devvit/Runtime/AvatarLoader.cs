using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Devvit
{
    /// <summary>
    /// Utility for loading Reddit snoovatar images from CDN URLs into Unity Sprites.
    /// Used by UI components (e.g. DevvitUserProfileUI) to display player avatars
    /// after receiving the snoovatarUrl from /api/init.
    /// </summary>
    public static class AvatarLoader
    {
        /// <summary>
        /// Downloads an avatar image from a URL and converts it to a Unity Sprite.
        /// </summary>
        /// <param name="url">URL of the image to download.</param>
        /// <param name="onComplete">Callback invoked with the Sprite on success, or null on failure.</param>
        public static void LoadAvatar(string url, Action<Sprite> onComplete)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[AvatarLoader] URL is null or empty");
                onComplete?.Invoke(null);
                return;
            }

            CoroutineRunner.Instance.StartCoroutine(LoadAvatarCoroutine(url, onComplete));
        }

        private static IEnumerator LoadAvatarCoroutine(string url, Action<Sprite> onComplete)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AvatarLoader] Failed to download avatar from {url}: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                {
                    Debug.LogError("[AvatarLoader] Downloaded texture is null");
                    onComplete?.Invoke(null);
                    yield break;
                }

                // Convert texture to sprite
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                onComplete?.Invoke(sprite);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarLoader] Error creating sprite: {e.Message}");
                onComplete?.Invoke(null);
            }
        }
    }
}
