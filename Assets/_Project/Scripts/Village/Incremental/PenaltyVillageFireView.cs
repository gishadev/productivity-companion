using UnityEngine;

namespace gishadev.companion.Village
{
    // Driven by VillageController: scene objects outside the widget canvas aren't injected.
    public sealed class PenaltyVillageFireView : MonoBehaviour
    {
        [SerializeField] private GameObject fireObj;

        private void Awake()
        {
            if (fireObj == null)
                Debug.LogError($"[Village] {name}: no fire object assigned; penalties will not show.", this);
        }

        public void SetOnFire(bool onFire)
        {
            if (fireObj != null && fireObj.activeSelf != onFire) fireObj.SetActive(onFire);
        }
    }
}
