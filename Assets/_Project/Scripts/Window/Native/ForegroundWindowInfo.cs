namespace gishadev.companion.Window.Native
{
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

        public bool IsOwnProcess { get; }

        // Lowercase, without ".exe".
        public string ProcessName { get; }

        // Not classified on yet.
        public string Title { get; }
    }
}
