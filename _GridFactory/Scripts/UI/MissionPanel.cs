using System.Collections.Generic;

using UnityEngine;

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
    }
}
