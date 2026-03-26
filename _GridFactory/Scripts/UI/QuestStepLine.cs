using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DialogueQuests
{

    public class QuestStepLine : MonoBehaviour
    {

        /// <summary>
        ///     public TMP_Text step_title;
        /// </summary>
        public TMP_Text step_desc;
        public TMP_Text step_title;
        public Image step_completed;



        private QuestData quest;


        void Awake()
        {

        }



        public void SetStep(QuestStep step)
        {
            if (step.title != "")
                step_title.SetText(step.title);
            step_desc.SetText(step.desc);
            if (step.achived)
                step_desc.SetText("<s>" + step.desc + "</s>");
            /*
          quest_title.text = quest.GetTitle();

          if (quest_text != null)
              quest_text.SetText(quest.GetDesc());

          if (quest_icon != null)
          {
              quest_icon.sprite = quest.icon;
              quest_icon.enabled = quest.icon != null;
          }

          if (quest_completed != null)
          {
              bool completed = quest.IsCompleted();
              bool failed = quest.IsFailed();
              quest_completed.enabled = completed || failed;
              quest_completed.sprite = completed ? success_sprite : fail_sprite;
          }
*/

            gameObject.SetActive(true);
        }



        public void Hide()
        {
            gameObject.SetActive(false);
        }


    }

}
