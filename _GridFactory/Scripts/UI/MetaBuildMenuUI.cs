using UnityEngine;

using GridFactory.Meta;
using Unity.VisualScripting;

namespace GridFactory.UI
{
    public class MetaBuildMenuUI : MonoBehaviour
    {
        [SerializeField] GameObject subConveyor;
        [SerializeField] GameObject subResource;
        [SerializeField] GameObject subMachine;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnClickResearchCenter();
            }
        }

        public void OnClickConveyorToggle()
        {
            MetaBuildController.Instance.CancelBuilding();
            if (!subConveyor.activeSelf)
            {
                subConveyor.SetActive(true);

                subResource.SetActive(false);
                subMachine.SetActive(false);
            }
            else
            {
                subConveyor.SetActive(false);
            }
        }

        public void OnClickResourceToggle()
        {
            MetaBuildController.Instance.CancelBuilding();
            if (!subResource.activeSelf)
            {
                subResource.SetActive(true);

                subConveyor.SetActive(false);
                subMachine.SetActive(false);
            }
            else
            {
                subResource.SetActive(false);
            }
        }

        public void OnClickMachineToggle()
        {
            MetaBuildController.Instance.CancelBuilding();
            if (!subMachine.activeSelf)
            {
                subMachine.SetActive(true);

                subConveyor.SetActive(false);
                subResource.SetActive(false);
            }
            else
            {
                subMachine.SetActive(false);
            }
        }

        public void CloseAllSubs()
        {
            subConveyor.SetActive(false);
            subResource.SetActive(false);
            subMachine.SetActive(false);
        }

        public void OnClickResourceNodeOre()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.ResourceNodeOre);
            CloseAllSubs();
        }

        public void OnClickResourceNodeFarm()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.ResourceNodeFarm);
            CloseAllSubs();
        }

        public void OnClickMarket()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Market);
            CloseAllSubs();
        }

        public void OnClickPowerPlant()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.PowerPlant);
            CloseAllSubs();
        }

        public void OnClickResearchCenter()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.ResearchCenter);
            CloseAllSubs();
        }

        public void OnClickConveyor()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Conveyor);
            CloseAllSubs();
        }

        public void OnClickSplitter()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Splitter);
            CloseAllSubs();
        }

        public void OnClickMerger()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Merger);
            CloseAllSubs();
        }

        public void OnClicKCrossing()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Crossing);
            CloseAllSubs();
        }

        public void OnClickBlueprint()
        {
            MetaBuildController.Instance.SetBuildType(MetaBuildType.Blueprint);
            CloseAllSubs();
        }

    }
}
