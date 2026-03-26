using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueQuests
{

    public class MissionPanel : QuestPanel
    {



        [SerializeField] private GameObject panelRoot;

        protected override void Start()
        {
            base.Start();
            panelRoot.SetActive(false);
        }



        public void CheckForUpdate()
        {
            List<QuestData> all_quest;
            all_quest = QuestData.GetAllActive();

            if (all_quest.Count > 0)
            {
                panelRoot.SetActive(true);
                base.Show(true);
                RefreshPanel();

            }
            else
            {
                base.Hide(true);
                panelRoot.SetActive(false);
            }
        }



        // LISTEN TO QUEST MANAGER AND SHOW / UPDATE IF THERE ARE QUESTS
    }

}
