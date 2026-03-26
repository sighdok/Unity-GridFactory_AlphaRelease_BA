using UnityEngine;
using UnityEngine.UI;

namespace GridFactory.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Root of the Meta Menu UI (panel/canvas root)")]
        [SerializeField] private GameObject menuRoot;

        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }


        private void Awake()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false); // Start closed


        }

        public void Open()
        {
            if (menuRoot == null) return;

            menuRoot.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            if (menuRoot == null) return;

            menuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (menuRoot == null) return;

            if (menuRoot.activeSelf) Close();
            else Open();
        }




    }
}
