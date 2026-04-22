using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Devvit
{
    /// <summary>
    /// UI component that displays Reddit user profile information (username, avatar, previous score).
    ///
    /// How to use:
    ///   Subscribe to SaveDataManager.OnDevvitInitialized and call DisplayProfile()
    ///   from that handler. SaveDataManager already fetches /api/init on startup, so
    ///   consuming that data here avoids a duplicate HTTP request.
    ///
    ///   Example:
    ///     saveDataManager.OnDevvitInitialized += () =>
    ///         profileUI.DisplayProfile(savedInitData);
    ///
    /// Do NOT call DevvitServiceFactory.Instance.FetchInitData() from this component.
    /// That would fire a second /api/init request on every game load.
    /// </summary>
    public class DevvitUserProfileUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text previousScoreText;

        /// <summary>
        /// Displays profile data received from the Devvit service.
        /// Call this from a SaveDataManager.OnDevvitInitialized subscriber,
        /// passing the init data that SaveDataManager already fetched.
        /// </summary>
        public void DisplayProfile(DevvitInitData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[DevvitUserProfileUI] Init data is null");
                return;
            }

            // Display username
            if (usernameText != null && !string.IsNullOrEmpty(data.username))
            {
                usernameText.text = $"u/{data.username}";
            }

            // Display previous score only if the player has one
            // (extraData holds the furthest distance; its presence signals a prior session)
            if (previousScoreText != null && !string.IsNullOrEmpty(data.previousScore.extraData))
            {
                previousScoreText.text = $"Previous Score: {data.previousScore.score}";
            }

            // Download and display avatar
            if (avatarImage != null && !string.IsNullOrEmpty(data.snoovatarUrl))
            {
                DevvitServiceFactory.Instance.DownloadImage(data.snoovatarUrl, OnAvatarDownloaded);
            }
        }

        private void OnAvatarDownloaded(Texture2D texture)
        {
            if (texture == null || avatarImage == null)
            {
                Debug.LogWarning("[DevvitUserProfileUI] Failed to download or display avatar");
                return;
            }

            // Convert texture to sprite and assign to Image
            Sprite avatarSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            avatarImage.sprite = avatarSprite;
            avatarImage.preserveAspect = true;

            Debug.Log("[DevvitUserProfileUI] Avatar displayed successfully");
        }
    }
}
