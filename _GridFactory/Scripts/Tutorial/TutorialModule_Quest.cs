using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using DialogueQuests;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    public class TutorialModule_Quest : TutorialModule
    {
        [Space(5)]
        [SerializeField] private QuestData quest;
        [SerializeField] private int step = 0;
        [SerializeField] private float startDelay;

        public override IEnumerator ActiveTheModuleEnum()
        {
            yield return new WaitForSeconds(startDelay);
            if (step == -1)
            {
                NarrativeManager.Get().StartQuest(quest);
            }
            else if (step == 99)
            {

                NarrativeManager.Get().CompleteQuest(quest);
            }
            else
            {
                List<QuestStep> openStepsAfterComplete = new List<QuestStep>();
                if (quest.steps.Length > 0)
                {
                    for (int i = 0; i < quest.steps.Length; i++)
                    {
                        if (i == step)
                        {
                            quest.SetQuestStep(step);
                            quest.steps[i].achived = true;
                        }
                        else
                        {
                            if (!quest.steps[i].achived)
                                openStepsAfterComplete.Add(quest.steps[i]);
                        }
                    }

                    if (openStepsAfterComplete.Count == 0)
                        NarrativeManager.Get().CompleteQuest(quest);
                }
            }
            TutorialGridFactoryController.Instance.RefreshQuestPanel();
        }
    }
}