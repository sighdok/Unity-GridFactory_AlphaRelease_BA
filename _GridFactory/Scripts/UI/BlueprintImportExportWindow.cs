using UnityEngine;

using TMPro;

using GridFactory.Blueprints;

namespace GridFactory.UI
{
    public class BlueprintImportExportWindow : MonoBehaviour
    {

        private BlueprintDefinition currentBp;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField textPanel;
        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }

        private void Awake()
        {
            panelRoot.SetActive(false);
        }

        public void Open(BlueprintDefinition bp)
        {
            currentBp = bp;
            panelRoot.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            textPanel.text = "";
            currentBp = null;
            panelRoot.SetActive(false);
            _isOpen = false;
        }

        /*
        public void ExportBlueprint()
        {
            if (currentBp != null)
            {
                var encodedBp = BlueprintManager.Instance.ExportBlueprintText(currentBp);
                textPanel.text = encodedBp;
            }
        }

        public void ImportBlueprint()
        {
            if (textPanel.text != "")
            {
                BlueprintDefinition importedBp = BlueprintManager.Instance.ImportBlueprintText(textPanel.text);
                BlueprintManager.Instance.ApplyBlueprint(importedBp, new Vector2Int(0, 0)); // exakt wie gespeichert
                Close();
            }
        }
        */
    }
}
