namespace gishadev.companion.Window.Native
{
    public interface IForegroundWindowProvider
    {
        bool IsAvailable { get; }

        ForegroundWindowInfo GetForegroundWindow();
    }
}
