using UnityEngine;

using TMPro;

using GridFactory.Blueprints;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class BlueprintInfoPanel : MonoBehaviour
    {
        private static BlueprintManager BPM => BlueprintManager.Instance;

        [SerializeField] private GameObject infoPanelMenuRoot;
        [SerializeField] private TMP_Text infoText;


        void OnEnable()
        {
            BPM.OnBlueprintInfoUpdated += RefreshInfo;
        }

        void OnDisable()
        {
            BPM.OnBlueprintInfoUpdated -= RefreshInfo;
        }

        public void RefreshInfo(BluePrintInfo info)
        {
            string myRichText = "";
            bool errorsOrWarnings = false;

            if (info.errors.Count > 0)
            {
                errorsOrWarnings = true;
                myRichText += "<color=#934E4B><b>ERR:</b></color>\n<color=red><size=80%>";
                foreach (string err in info.errors)
                    myRichText += err + "\n";
                myRichText += "</size></color>";
            }

            if (info.warnings.Count > 0)
            {
                errorsOrWarnings = true;
                myRichText += "<color=#8D934B><b>WARN:</b></color>\n<color=yellow><size=80%>";
                foreach (string warn in info.warnings)
                    myRichText += warn + "\n";
                myRichText += "</size></color>";
            }
            if (!errorsOrWarnings && info != null)
            {
                myRichText += "<color=#519FBC><b>Input:</b>\n</color><size=80%><color=grey>";
                foreach (ItemDefinition item in info.inputItems)
                    myRichText += item.displayName + "\n"; ;

                myRichText += "</color> </size>\n";
                myRichText += "<color=#C17B28><b>Output:</b>\n</color><size=80%><color=grey>";

                if (info.outputItem != null)
                    myRichText += info.outputsMin + " " + info.outputItem.displayName + " / Min\n";

                myRichText += "<size=60%>(at 100% Input & Energy)</size></size>\n\n";
                myRichText += "<b>Energy:</b>\n<size=80%>" + (info.expectedEnergyConsumption * (1 / TickManager.Instance.TickInterval)) + " / Sek";
            }

            infoText.SetText(myRichText);
        }
    }
}
