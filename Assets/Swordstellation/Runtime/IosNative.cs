#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Swordstellation.Runtime
{
    /// C# side of Assets/Plugins/iOS/SwordNative.mm. Empty outside iOS devices.
    public static class IosNative
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string SwordLaunchArgument(string key);

        public static string LaunchArgument(string key) => SwordLaunchArgument(key);
#else
        public static string LaunchArgument(string key) => null;
#endif
    }
}
