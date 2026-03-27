using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace DialogueQuests
{
    public class QuestStepLine : MonoBehaviour
    {
        public TMP_Text step_desc;
        public TMP_Text step_title;
        public Image step_completed;

        public void SetStep(QuestStep step)
        {
            step_title.SetText(step.title);
            step_desc.SetText(step.desc);
            if (step.achived)
                step_desc.SetText("<s>" + step.desc + "</s>");

            gameObject.SetActive(true);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }

}
