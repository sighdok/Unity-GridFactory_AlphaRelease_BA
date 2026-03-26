using System.Collections;

using UnityEngine;

using DialogueQuests;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    public class TutorialModule_Dialogue : TutorialModule
    {
        [Space(5)]
        [SerializeField] private Actor actor;
        [Space(5)]
        [SerializeField] private NarrativeEvent narrativeEvent;
        [Space(5)]
        [SerializeField] private float startDelay;

        public override IEnumerator ActiveTheModuleEnum()
        {
            yield return new WaitForSeconds(startDelay);
            narrativeEvent.Trigger(actor);
        }
    }
}