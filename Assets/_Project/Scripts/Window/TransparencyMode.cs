namespace gishadev.companion.Window
{
    // Per-pixel alpha misbehaves on some drivers; ColorKey is the fallback.
    public enum TransparencyMode
    {
        Off = 0,

        PerPixelAlpha = 1,

        ColorKey = 2
    }

    public enum WindowSetting
    {
        Transparency,
        ClickThrough,
        AlwaysOnTop,
        HideFromTaskbar,
        TargetFrameRate,
        PreventDisplaySleep
    }
}
