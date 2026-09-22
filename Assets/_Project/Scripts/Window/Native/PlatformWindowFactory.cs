using UnityEngine;

namespace gishadev.companion.Window.Native
{
    public static class PlatformWindowFactory
    {
        public static IPlatformWindow Create()
        {
#if UNITY_STANDALONE_WIN
            // Also defined in the editor for a Windows build target.
            if (!Application.isEditor)
                return new Win32PlatformWindow();
#endif
            return new NullPlatformWindow();
        }

        // Live in the editor too: it only reads, and focus tracking needs to be testable in play mode.
        public static IForegroundWindowProvider CreateForegroundWindowProvider()
        {
#if UNITY_STANDALONE_WIN
            return new Win32ForegroundWindowProvider();
#else
            return new NullForegroundWindowProvider();
#endif
        }
    }
}
