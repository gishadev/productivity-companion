namespace gishadev.companion.Focus
{
    /// <summary>How the foreground application counts toward the user's activity.</summary>
    public enum FocusCategory
    {
        /// <summary>Neither listed, and the default for anything unknown.</summary>
        Regular = 0,
        Productive = 1,
        Unproductive = 2
    }
}
