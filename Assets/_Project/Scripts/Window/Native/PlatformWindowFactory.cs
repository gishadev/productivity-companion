using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>Single decision point for whether native window manipulation is live.</summary>
    public static class PlatformWindowFactory
    {
        public static IPlatformWindow Create()
        {
#if UNITY_STANDALONE_WIN
            // Also defined in the editor when the build target is Windows, so the runtime check is what
            // keeps HWND poking out of play mode.
            if (!Application.isEditor)
                return new Win32PlatformWindow();
#endif
            return new NullPlatformWindow();
        }

        /// <summary>
        /// No editor exemption, unlike <see cref="Create"/>: reading the foreground window mutates
        /// nothing, and having it live in play mode is what makes focus tracking testable without a build.
        /// </summary>
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
