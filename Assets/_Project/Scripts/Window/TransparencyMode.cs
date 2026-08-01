namespace gishadev.companion.Window
{
    /// <summary>
    /// Both transparency paths are exposed to the user because neither is universally reliable:
    /// per-pixel alpha is the better-looking option but misbehaves on some GPU/driver combinations,
    /// and the color key is the ugly-but-works fallback.
    /// </summary>
    public enum TransparencyMode
    {
        /// <summary>Ordinary opaque window. Useful for debugging and the safe default.</summary>
        Off = 0,

        /// <summary>WS_EX_LAYERED + LWA_ALPHA + a DWM frame extension. Supports soft/antialiased edges.</summary>
        PerPixelAlpha = 1,

        /// <summary>WS_EX_LAYERED + LWA_COLORKEY. Hard-edged, but survives drivers that break alpha.</summary>
        ColorKey = 2
    }

    /// <summary>Identifies which setting changed, so listeners can re-apply only the affected piece.</summary>
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
