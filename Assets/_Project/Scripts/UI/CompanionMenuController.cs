using gishadev.tools.UI;
using UnityEngine;

namespace gishadev.companion.UI
{
    public class CompanionMenuController : MenuController
    {
        [SerializeField] private PopupPage settings;

        protected override void Start()
        {
            base.Start();
        }

        public void OnSettingsClicked()
        {
            PushPage(settings);
        }
    }
}