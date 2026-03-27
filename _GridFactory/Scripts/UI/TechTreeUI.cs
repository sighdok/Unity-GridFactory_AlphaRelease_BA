using UnityEngine;

using GridFactory.Tech;

namespace GridFactory.UI
{
    public class TechTreeUI : MonoBehaviour
    {
        [SerializeField] private GameObject techTreeRoot;

        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            techTreeRoot.SetActive(false); // Start closed
        }

        public void Open()
        {
            techTreeRoot.SetActive(true);
            TechTreeNodeUGUI.RefreshAll();
            _isOpen = true;
        }

        public void Close()
        {
            techTreeRoot.SetActive(false);
            TechTreeManager.Instance.SelectedCenter = null;
            _isOpen = false;

        }

        public void Toggle()
        {
            if (techTreeRoot.activeSelf)
                Close();
            else
                Open();
        }
    }
}
