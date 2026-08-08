namespace gishadev.companion.Window.Native
{
    /// <summary>Non-Windows targets. Every read is invalid, so callers need no platform guards.</summary>
    public sealed class NullForegroundWindowProvider : IForegroundWindowProvider
    {
        public bool IsAvailable => false;

        public ForegroundWindowInfo GetForegroundWindow() => ForegroundWindowInfo.Invalid;
    }
}
