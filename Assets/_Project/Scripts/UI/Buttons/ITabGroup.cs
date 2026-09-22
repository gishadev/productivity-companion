namespace gishadev.companion.UI
{
    public interface ITabGroup
    {
        void OnTabSelected(TabButton tab);
        bool IsActive(TabButton tab);
    }
}
