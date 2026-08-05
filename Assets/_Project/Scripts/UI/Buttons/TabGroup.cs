using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace gishadev.companion.UI
{
    public class TabGroup : MonoBehaviour, ITabGroup
    {
        [Header("Tabs")] [SerializeField] private List<TabButton> tabs = new();

        [Header("Content Panels (must match tab order)")] [SerializeField]
        private List<GameObject> panels = new();

        [Header("Persistence")] [SerializeField]
        private string saveKey = "ActiveTabIndex";

        [SerializeField] private bool rememberSelection = true;

        [Header("Events")] public UnityEvent<int> onTabChanged;

        private TabButton _activeTab;

        private void Start()
        {
            foreach (var tab in tabs)
                tab.Initialize(this);

            int startIndex = rememberSelection
                ? PlayerPrefs.GetInt(saveKey, 0)
                : 0;

            SelectTab(Mathf.Clamp(startIndex, 0, tabs.Count - 1));
        }

        public void OnTabSelected(TabButton tab) => SelectTab(tabs.IndexOf(tab));
        public bool IsActive(TabButton tab) => tab == _activeTab;

        private void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;

            _activeTab?.SetActive(false);
            _activeTab = tabs[index];
            _activeTab.SetActive(true);

            for (int i = 0; i < panels.Count; i++)
                panels[i]?.SetActive(i == index);

            if (rememberSelection)
                PlayerPrefs.SetInt(saveKey, index);

            onTabChanged?.Invoke(index);
        }

        public int ActiveIndex => tabs.IndexOf(_activeTab);
    }
}