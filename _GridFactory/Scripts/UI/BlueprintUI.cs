using UnityEngine;

using TMPro;

using GridFactory.Blueprints;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class BlueprintUI : MonoBehaviour
    {
        private static BlueprintManager BPM => BlueprintManager.Instance;
        private static GameManager GM => GameManager.Instance;
        private static BlueprintListUI BPLUI => BlueprintListUI.Instance;
        private static UIConfirmationManager UICONFIRMM => UIConfirmationManager.Instance;

        [SerializeField] private GameObject blueprintMenuRoot;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_InputField nameInput;

        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            blueprintMenuRoot.SetActive(false); // Start closed
        }

        public void Open()
        {
            blueprintMenuRoot.SetActive(true);
            BPLUI.RefreshList();
            _isOpen = true;
        }

        public void Close()
        {
            blueprintMenuRoot.SetActive(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (blueprintMenuRoot.activeSelf)
                Close();
            else
                Open();
        }

        public void OnLoadClicked(BlueprintDefinition bp)
        {
            if (GM.CurrentMode != GameMode.Blueprint)
                GM.SetMode(GameMode.Blueprint, false);

            UICONFIRMM.Show(
                "Load Blueprint?",
                () => BPM.ApplyBlueprint(bp, new Vector2Int(0, 0)),
                () => { }
            );
        }

        public void OnSaveClicked()
        {
            string name = string.IsNullOrEmpty(nameInput.text) ? $"BP_{System.DateTime.Now:HHmmss}" : nameInput.text;
            var bpInfo = BPM.CreateBlueprintFromCurrentGrid(name, null, false);
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
                        myRichText += err + "\n";

                    myRichText += "</size>";
                }

                if (info.warnings.Count > 0)
                {
                    errorsOrWarnings = true;
                    myRichText += "<size=80%>";

                    foreach (string warn in info.warnings)
                        myRichText += warn + "\n";
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

                    UICONFIRMM.Show(
                        "Save Blueprint?",
                        () =>
                        {
                            BPM.CreateBlueprintFromCurrentGrid(name);
                            nameInput.text = "";
                        },
                        () => { }, myRichText
                    );
                }
            }
            else
            {
                UICONFIRMM.Show(
                  "Error in Blueprint!",
                  () => { },
                  () => { }, myRichText, true
              );
            }
        }

        public void OnDeleteClicked(BlueprintDefinition bp)
        {
            var machineList = BPM.GetMachinesWithBlueprint(bp);
            int machineCountWithBlueprint = machineList.Count;

            UICONFIRMM.Show(
               "<color=#934E4B>ATTENTION:</color>\nDelete Blueprint '" + bp.displayName + "'?",
               () => BPM.DeleteBlueprint(bp),
               () => { },
               machineCountWithBlueprint + " machines in use.",
               false, false, 3f
           );



        }
        public void OnOverwriteClicked(BlueprintDefinition bp)
        {
            if (GM.CurrentMode == GameMode.Blueprint)
            {
                BluePrintInfo bpInfo = BPM.CreateBlueprintFromCurrentGrid(bp.displayName, null, false).blueprintInfo;
                if (bpInfo != null)
                {
                    string myRichText = "";
                    bool errorsOrWarnings = false;
                    if (bpInfo.errors.Count > 0)
                    {
                        errorsOrWarnings = true;
                        myRichText += "<size=80%>";

                        foreach (string err in bpInfo.errors)
                            myRichText += err + "\n";

                        myRichText += "</size>";
                    }

                    if (bpInfo.warnings.Count > 0)
                    {
                        errorsOrWarnings = true;
                        myRichText += "<size=80%>";

                        foreach (string warn in bpInfo.warnings)
                            myRichText += warn + "\n";

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

                        UICONFIRMM.Show(
                           "<color=#934E4B>ATTENTION:</color>\nOverwrite Blueprint '" + bp.displayName + "'?",
                           () => BPM.OverwriteBlueprint(bp),
                           () => { },
                           myRichText,
                           false, false, 3f);
                    }
                    else
                    {
                        UICONFIRMM.Show(
                          "Error in Blueprint!",
                          () => { },
                          () => { }, myRichText, true
                      );
                    }
                }
                else
                {
                    UICONFIRMM.Show(
                     "Error in Grid! No useable definition found.",
                     () => { },
                     () => { }, "", true
                 );
                }
            }
            else
            {
                UICONFIRMM.Show(
                  "Switch to the void to overwrite a portal",
                  () => { },
                  () => { }, "", true
              );
            }
        }
    }
}
