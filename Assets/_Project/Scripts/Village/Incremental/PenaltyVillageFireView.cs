using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Sets the village alight while a penalty stands. Like every other view here it subscribes to
    /// nothing and is told what to show: scene objects outside the widget canvas are not injected, and
    /// the container is built after their Start runs even when they are.
    /// </summary>
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
