using UnityEngine;

namespace GridFactory.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private GameObject shopMenuRoot;

        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            shopMenuRoot.SetActive(false);
        }

        public void Open()
        {
            shopMenuRoot.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            shopMenuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (shopMenuRoot.activeSelf)
                Close();
            else
                Open();
        }
    }
}
