using UnityEngine;

namespace GridFactory.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Root of the Meta Menu UI (panel/canvas root)")]
        [SerializeField] private GameObject shopMenuRoot;

        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }

        private void Awake()
        {
            if (shopMenuRoot != null)
                shopMenuRoot.SetActive(false); // Start closed
        }

        public void Open()
        {
            if (shopMenuRoot == null) return;
            shopMenuRoot.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            if (shopMenuRoot == null) return;
            shopMenuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (shopMenuRoot == null) return;

            if (shopMenuRoot.activeSelf) Close();
            else Open();
        }
    }
}
