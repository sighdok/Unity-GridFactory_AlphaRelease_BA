using UnityEngine;

using TMPro;

using GridFactory.Blueprints;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class BlueprintUI : MonoBehaviour
    {
        [Header("Root of the Meta Menu UI (panel/canvas root)")]
        [SerializeField] private GameObject blueprintMenuRoot;
        [SerializeField] private BlueprintImportExportWindow importExportWindow;

        [SerializeField] private BlueprintManager manager;

        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_InputField nameInput;

        private BlueprintDefinition currentBlueprint;

        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
        }


        private void Awake()
        {
            if (blueprintMenuRoot != null)
                blueprintMenuRoot.SetActive(false); // Start closed
        }

        private void Start()
        {
            if (manager == null)
                manager = FindFirstObjectByType<BlueprintManager>();

            //RefreshInfo("");
        }

        public void Open()
        {
            if (blueprintMenuRoot == null) return;
            blueprintMenuRoot.SetActive(true);
            BlueprintListUI.Instance.RefreshList();
            _isOpen = true;
        }

        public void Close()
        {
            if (blueprintMenuRoot == null) return;
            blueprintMenuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (blueprintMenuRoot == null) return;

            if (blueprintMenuRoot.activeSelf) Close();
            else Open();
        }

        public void OnLoadClicked(BlueprintDefinition bp)
        {

            if (GameManager.Instance.CurrentMode != GameMode.Blueprint)
                GameManager.Instance.SetMode(GameMode.Blueprint, false);


            UIConfirmationManager.Instance.Show(
                "Load Blueprint?",
                () => BlueprintManager.Instance.ApplyBlueprint(bp, new Vector2Int(0, 0)),
                () => { }
            );
        }

        public void OnSaveClicked()
        {
            string name = string.IsNullOrEmpty(nameInput.text)
                ? $"BP_{System.DateTime.Now:HHmmss}"
                : nameInput.text;

            var bpInfo = manager.CreateBlueprintFromCurrentGrid(name, null, false);
            BluePrintInfo info = bpInfo.blueprintInfo;

            string myRichText = "";
            bool errorsOrWarnings = false;
            if (info != null)
            {
                if (info.errors.Count > 0)
                {
                    errorsOrWarnings = true;
                    myRichText += "<size=80%>";
                    foreach (string err in info.errors)
                    {
                        myRichText += err + "\n";

                    }
                    myRichText += "</size>";
                }

                if (info.warnings.Count > 0)
                {
                    errorsOrWarnings = true;
                    myRichText += "<size=80%>";
                    foreach (string warn in info.warnings)
                    {
                        myRichText += warn + "\n";

                    }
                    myRichText += "</size>";
                }
                if (!errorsOrWarnings)
                {



                    myRichText += "<b>Input:</b>\n<size=80%>";
                    foreach (ItemDefinition item in info.inputItems)

                        myRichText += item.displayName + "\n"; ;


                    myRichText += "</size>\n";
                    myRichText += "<b>Output:</b>\n<size=80%>";
                    if (info.outputItem != null)
                        myRichText += info.outputsMin + " " + info.outputItem.displayName + " / Min\n";

                    myRichText += "<size=60%>(at 100% Input & Energy)</size></size>\n\n";
                    myRichText += "<b>Energy:</b>\n<size=80%>" + (info.expectedEnergyConsumption * (1 / TickManager.Instance.TickInterval)) + " / Sek";
                    UIConfirmationManager.Instance.Show(
                        "Save Blueprint?",
                        () =>
                        {
                            manager.CreateBlueprintFromCurrentGrid(name);
                            nameInput.text = "";
                        },
                        () => { }, myRichText
                    );
                }
            }
            else
            {
                UIConfirmationManager.Instance.Show(
                    "Error in Blueprint!",
                    () => { },
                    () => { }, myRichText, true
                );
            }
        }

        public void OnDeleteClicked(BlueprintDefinition bp)
        {
            var machineList = BlueprintManager.Instance.GetMachinesWithBlueprint(bp);
            int machineCountWithBlueprint = machineList.Count;

            UIConfirmationManager.Instance.Show(
                               "<color=#934E4B>ATTENTION:</color>\nDelete Blueprint '" + bp.displayName + "'?",
                               () =>
                               {
                                   manager.DeleteBlueprint(bp);

                               },
                               () => { }, machineCountWithBlueprint + " machines in use.", false, false, 3f);



        }
        public void OnOverwriteClicked(BlueprintDefinition bp)
        {
            if (GameManager.Instance.CurrentMode == GameMode.Blueprint)
            {
                BluePrintInfo bpInfo = manager.CreateBlueprintFromCurrentGrid(bp.displayName, null, false).blueprintInfo;

                if (bpInfo != null)
                {
                    string myRichText = "";
                    bool errorsOrWarnings = false;

                    if (bpInfo.errors.Count > 0)
                    {
                        errorsOrWarnings = true;
                        myRichText += "<size=80%>";
                        foreach (string err in bpInfo.errors)
                        {
                            myRichText += err + "\n";

                        }
                        myRichText += "</size>";
                    }

                    if (bpInfo.warnings.Count > 0)
                    {
                        errorsOrWarnings = true;
                        myRichText += "<size=80%>";
                        foreach (string warn in bpInfo.warnings)
                        {
                            myRichText += warn + "\n";

                        }
                        myRichText += "</size>";
                    }
                    if (!errorsOrWarnings && bpInfo != null)
                    {



                        myRichText += "<b>Input:</b>\n<size=80%>";
                        foreach (ItemDefinition item in bpInfo.inputItems)

                            myRichText += item.displayName + "\n"; ;


                        myRichText += "</size>\n";
                        myRichText += "<b>Output:</b>\n<size=80%>";

                        if (bpInfo.outputItem != null)
                            myRichText += bpInfo.outputsMin + " " + bpInfo.outputItem.displayName + " / Min\n";

                        myRichText += "<size=60%>(at 100% Input & Energy)</size></size>\n\n";
                        myRichText += "<b>Energy:</b>\n<size=80%>" + (bpInfo.expectedEnergyConsumption * (1 / TickManager.Instance.TickInterval)) + " / Sek";
                        UIConfirmationManager.Instance.Show(
                            "<color=#934E4B>ATTENTION:</color>\nOverwrite Blueprint '" + bp.displayName + "'?",
                            () =>
                            {
                                manager.OverwriteBlueprint(bp);

                            },
                            () => { }, myRichText, false, false, 3f);
                    }
                    else
                    {
                        UIConfirmationManager.Instance.Show(
                            "Error in Blueprint!",
                            () => { },
                            () => { }, myRichText, true
                        );
                    }
                }
                else
                {
                    UIConfirmationManager.Instance.Show(
                       "Error in Grid! No useable definition found.",
                       () => { },
                       () => { }, "", true
                   );
                }
            }
            else
            {
                UIConfirmationManager.Instance.Show(
                    "Switch to the void to overwrite a portal",
                    () => { },
                    () => { }, "", true
                );
            }
        }
    }
}
