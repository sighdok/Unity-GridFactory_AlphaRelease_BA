

using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    public class TutorialModule_ButtonControl : TutorialModule
    {
        [Space(5)]
        [SerializeField] private Transform targetTransform;
        [Space(5)]
        [SerializeField] private bool disable = true;
        [Space(5)]
        [SerializeField] private bool recursive = false;
        [Space(5)]
        [SerializeField] private float startDelay = 0;

        public override IEnumerator ActiveTheModuleEnum()
        {
            yield return new WaitForSeconds(startDelay);
            if (targetTransform)
            {
                if (targetTransform.GetComponent<Button>())
                {
                    Button btn = targetTransform.GetComponent<Button>();
                    if (disable)
                        btn.interactable = false;
                    else
                        btn.interactable = true;
                }
                if (recursive)
                {
                    Button[] buttons = targetTransform.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        if (disable)
                            btn.interactable = false;
                        else
                            btn.interactable = true;

                    }
                }
            }
        }
    }
}
