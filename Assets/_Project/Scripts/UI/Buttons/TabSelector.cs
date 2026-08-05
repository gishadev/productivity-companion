using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace gishadev.companion.UI
{
    /// <summary>
    /// A panel-less variant of <see cref="TabGroup"/>. It manages only the visual
    /// selection state of its tabs and exposes the active index for manual control;
    /// reacting to the selection (showing content, etc.) is left to listeners of
    /// <see cref="onTabChanged"/>. Tabs may be assigned in the inspector or supplied
    /// at runtime via <see cref="SetTabs"/>.
    /// </summary>
    public class TabSelector : MonoBehaviour, ITabGroup
    {
        [Header("Tabs")] [SerializeField] private List<TabButton> tabs = new();

        [Header("Behaviour")] [Tooltip("Select the first tab automatically on Start.")] [SerializeField]
        private bool selectFirstOnStart = true;

        [Header("Events")] public UnityEvent<int> onTabChanged;

        private TabButton _activeTab;

        public int ActiveIndex => tabs.IndexOf(_activeTab);
        public int TabCount => tabs.Count;

        private void Start()
        {
            if (_activeTab == null && tabs.Count > 0)
                Initialize(selectFirstOnStart ? 0 : -1);
        }

        /// <summary>Replaces the tabs at runtime and selects <paramref name="startIndex"/> (-1 = none).</summary>
        public void SetTabs(IEnumerable<TabButton> newTabs, int startIndex = 0)
        {
            tabs = new List<TabButton>(newTabs);
            Initialize(startIndex);
        }

        private void Initialize(int startIndex)
        {
            foreach (var tab in tabs)
                tab.Initialize(this);

            _activeTab = null;
            if (startIndex >= 0)
                SelectTab(startIndex);
        }

        public void OnTabSelected(TabButton tab) => SelectTab(tabs.IndexOf(tab));
        public bool IsActive(TabButton tab) => tab == _activeTab;

        /// <summary>Manually selects a tab by index. Ignored if out of range.</summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;

            _activeTab?.SetActive(false);
            _activeTab = tabs[index];
            _activeTab.SetActive(true);

            onTabChanged?.Invoke(index);
        }

        public void Next()
        {
            if (tabs.Count == 0) return;
            SelectTab((ActiveIndex + 1) % tabs.Count);
        }

        public void Previous()
        {
            if (tabs.Count == 0) return;
            SelectTab((ActiveIndex - 1 + tabs.Count) % tabs.Count);
        }
    }
}