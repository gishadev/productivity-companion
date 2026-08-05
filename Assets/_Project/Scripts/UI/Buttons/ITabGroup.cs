namespace gishadev.companion.UI
{
    /// <summary>
    /// Contract a <see cref="TabButton"/> needs from whatever manages it, so the
    /// same button can be driven by different tab containers (e.g. <see cref="TabGroup"/>
    /// or <see cref="TabSelector"/>).
    /// </summary>
    public interface ITabGroup
    {
        void OnTabSelected(TabButton tab);
        bool IsActive(TabButton tab);
    }
}
