using GridFactory.Tech;
using UnityEngine;

namespace GridFactory.UI
{
    public class TechTreeUI : MonoBehaviour
    {
        [Header("Root of the Meta Menu UI (panel/canvas root)")]
        [SerializeField] private GameObject techTreeRoot;
        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }

        private void Awake()
        {
            if (techTreeRoot != null)
                techTreeRoot.SetActive(false); // Start closed
        }

        public void Open()
        {
            if (techTreeRoot == null) return;
            techTreeRoot.SetActive(true);
            TechTreeNodeUGUI.RefreshAll();
            _isOpen = true;
        }

        public void Close()
        {
            if (techTreeRoot == null) return;
            techTreeRoot.SetActive(false);
            TechTreeManager.Instance.SelectedCenter = null;
            _isOpen = false;

        }

        public void Toggle()
        {
            if (techTreeRoot == null) return;

            if (techTreeRoot.activeSelf) Close();
            else Open();
        }
    }
}
