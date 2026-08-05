namespace gishadev.companion.Window
{
    /// <summary>
    /// Both paths are exposed because neither is universally reliable: per-pixel alpha looks better but
    /// misbehaves on some GPU/driver combinations, and the color key is the ugly-but-works fallback.
    /// </summary>
    public enum TransparencyMode
    {
        Off = 0,

        /// <summary>WS_EX_LAYERED + LWA_ALPHA + DWM frame extension. Supports antialiased edges.</summary>
        PerPixelAlpha = 1,

        /// <summary>WS_EX_LAYERED + LWA_COLORKEY. Hard-edged, but survives drivers that break alpha.</summary>
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
