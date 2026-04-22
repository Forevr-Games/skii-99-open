using UnityEngine;

namespace Devvit
{
    /// <summary>
    /// Internal utility that provides MonoBehaviour context for running coroutines.
    /// This is required because IDevvitService implementations are pure C# classes.
    /// The GameObject is automatically created on first access and persists across scenes.
    /// </summary>
    internal class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        /// <summary>
        /// Gets the singleton instance, creating it if necessary.
        /// </summary>
        internal static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Programmatically create GameObject
                    GameObject go = new GameObject("[Devvit Coroutine Runner]");

                    // Mark as internal/system object (hidden from Hierarchy and Inspector)
                    go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                    // Add component programmatically
                    _instance = go.AddComponent<CoroutineRunner>();

                    // Persist across scene loads (only in Play mode)
#if UNITY_EDITOR
                    if (UnityEngine.Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
#else
                    DontDestroyOnLoad(go);
#endif
                }
                return _instance;
            }
        }

        // Prevent duplicate instances. Awake() runs if a CoroutineRunner was
        // placed in the scene manually — in that case, the Instance getter above
        // never ran, so DontDestroyOnLoad needs to be called here as well.
        // When the Instance getter creates the object in code, it calls
        // DontDestroyOnLoad there; then Awake fires and calls it again, which
        // is harmless — Unity is idempotent about repeated DontDestroyOnLoad calls.
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Only use DontDestroyOnLoad in Play mode
#if UNITY_EDITOR
            if (UnityEngine.Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
#else
            DontDestroyOnLoad(gameObject);
#endif
        }

        // Cleanup on application quit
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}
