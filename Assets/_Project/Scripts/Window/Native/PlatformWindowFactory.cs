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
    }
}
