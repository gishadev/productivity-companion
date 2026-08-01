using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Single decision point for whether native window manipulation is live. Everything else in the
    /// codebase talks to <see cref="IPlatformWindow"/> and never checks the platform itself.
    /// </summary>
    public static class PlatformWindowFactory
    {
        public static IPlatformWindow Create()
        {
#if UNITY_STANDALONE_WIN
            // UNITY_STANDALONE_WIN is also defined in the editor when the build target is Windows,
            // so the runtime check is what actually keeps HWND poking out of play mode.
            if (!Application.isEditor)
                return new Win32PlatformWindow();
#endif
            return new NullPlatformWindow();
        }
    }
}
