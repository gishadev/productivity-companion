namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Reads which application currently owns the foreground. Safe to call unconditionally: with no
    /// native support every read returns <see cref="ForegroundWindowInfo.Invalid"/>.
    /// </summary>
    public interface IForegroundWindowProvider
    {
        bool IsAvailable { get; }

        ForegroundWindowInfo GetForegroundWindow();
    }
}
