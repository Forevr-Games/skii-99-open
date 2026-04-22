namespace Devvit
{
    /// <summary>
    /// Factory for creating IDevvitService instances.
    /// Automatically selects between mock (Editor) and real (Build) implementations based on compilation symbols.
    /// </summary>
    public static class DevvitServiceFactory
    {
        private static IDevvitService _instance;

        /// <summary>
        /// Gets the singleton IDevvitService instance.
        /// Returns DevvitServiceMock in Unity Editor, DevvitServiceBuild in builds.
        /// </summary>
        public static IDevvitService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Create();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates the appropriate service implementation based on the current platform.
        /// </summary>
        private static IDevvitService Create()
        {
#if UNITY_EDITOR
            return new DevvitServiceMock();
#else
            return new DevvitServiceBuild();
#endif
        }

        /// <summary>
        /// Allows manual injection of a service instance (useful for testing).
        /// </summary>
        /// <param name="service">The service instance to use.</param>
        public static void SetInstance(IDevvitService service)
        {
            _instance = service;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resets the singleton instance on domain reload in the Editor.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            _instance = null;
        }
#endif
    }
}
