using UnityEngine;

namespace GridFactory.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;

        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            menuRoot.SetActive(false);
        }

        public void Open()
        {
            menuRoot.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            menuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (menuRoot.activeSelf)
                Close();
            else
                Open();
        }
    }
}
