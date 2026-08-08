namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Snapshot of whichever window owns the foreground. Carries no HWND: the handle stays inside
    /// <see cref="Native"/>.
    /// </summary>
    public readonly struct ForegroundWindowInfo
    {
        public static readonly ForegroundWindowInfo Invalid = default;

        public ForegroundWindowInfo(string processName, string title, bool isOwnProcess)
        {
            ProcessName = processName;
            Title = title;
            IsOwnProcess = isOwnProcess;
            IsValid = true;
        }

        public bool IsValid { get; }

        /// <summary>True when the player itself holds the foreground.</summary>
        public bool IsOwnProcess { get; }

        /// <summary>Lowercase, without the ".exe" suffix.</summary>
        public string ProcessName { get; }

        /// <summary>
        /// Unused by classification today; captured so title-based rules can be added without
        /// reopening the native seam.
        /// </summary>
        public string Title { get; }
    }
}
