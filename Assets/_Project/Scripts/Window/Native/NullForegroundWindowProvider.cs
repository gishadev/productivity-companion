namespace gishadev.companion.Window.Native
{
    public sealed class NullForegroundWindowProvider : IForegroundWindowProvider
    {
        public bool IsAvailable => false;

        public ForegroundWindowInfo GetForegroundWindow() => ForegroundWindowInfo.Invalid;
    }
}
