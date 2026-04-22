using System;
using UnityEngine;

namespace Devvit
{
    /// <summary>
    /// Generic wrapper for the raw JSON embedded in a custom Reddit post's postData field.
    /// Post data is arbitrary JSON set at post-creation time via submitCustomPost({ postData: {...} }).
    ///
    /// Two approaches to consuming postData — pick one and be consistent:
    ///
    ///   Option A (used in SaveDataManager): Skip DevvitPostData entirely and parse
    ///     initData.rawPostData directly into your concrete type:
    ///     <code>
    ///     var data = JsonUtility.FromJson&lt;ChallengePostData&gt;(initData.rawPostData);
    ///     </code>
    ///
    ///   Option B (using this class): Use Deserialize&lt;T&gt;() as a helper:
    ///     <code>
    ///     var data = initData.postData.Deserialize&lt;ChallengePostData&gt;();
    ///     </code>
    ///
    /// Both approaches are equivalent. SaveDataManager uses Option A (raw parse)
    /// because it avoids the intermediate wrapper object.
    /// </summary>
    [Serializable]
    public class DevvitPostData
    {
        [SerializeField] private string rawJson;

        /// <summary>
        /// Gets the raw JSON string of the post data.
        /// </summary>
        public string RawJson => rawJson;

        /// <summary>
        /// Deserializes the post data into a typed object using Unity's JsonUtility.
        /// Note: JsonUtility has limitations with nested objects and dictionaries.
        /// For complex structures, manually parse RawJson.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <returns>Deserialized object, or default(T) on failure.</returns>
        public T Deserialize<T>()
        {
            try
            {
                if (string.IsNullOrEmpty(rawJson))
                {
                    return default(T);
                }

                return JsonUtility.FromJson<T>(rawJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize postData: {e.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Creates a DevvitPostData instance from a JSON string.
        /// </summary>
        /// <param name="json">JSON string to wrap.</param>
        /// <returns>New DevvitPostData instance.</returns>
        public static DevvitPostData FromJson(string json)
        {
            var data = new DevvitPostData
            {
                rawJson = json
            };
            return data;
        }
    }
}
