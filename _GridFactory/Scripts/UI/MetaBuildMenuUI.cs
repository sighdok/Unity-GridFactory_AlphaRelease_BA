using UnityEngine;

using GridFactory.Meta;

namespace GridFactory.UI
{
    public class MetaBuildMenuUI : MonoBehaviour
    {
        private static MetaBuildController MBC => MBC;

        [SerializeField] private GameObject subConveyor;
        [SerializeField] private GameObject subResource;
        [SerializeField] private GameObject subMachine;

        public void OnClickConveyorToggle()
        {
            MBC.CancelBuilding();
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
            MBC.CancelBuilding();
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
            MBC.CancelBuilding();
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
            MBC.SetBuildType(MetaBuildType.ResourceNodeOre);
            CloseAllSubs();
        }

        public void OnClickResourceNodeFarm()
        {
            MBC.SetBuildType(MetaBuildType.ResourceNodeFarm);
            CloseAllSubs();
        }

        public void OnClickMarket()
        {
            MBC.SetBuildType(MetaBuildType.Market);
            CloseAllSubs();
        }

        public void OnClickPowerPlant()
        {
            MBC.SetBuildType(MetaBuildType.PowerPlant);
            CloseAllSubs();
        }

        public void OnClickResearchCenter()
        {
            MBC.SetBuildType(MetaBuildType.ResearchCenter);
            CloseAllSubs();
        }

        public void OnClickConveyor()
        {
            MBC.SetBuildType(MetaBuildType.Conveyor);
            CloseAllSubs();
        }

        public void OnClickSplitter()
        {
            MBC.SetBuildType(MetaBuildType.Splitter);
            CloseAllSubs();
        }

        public void OnClickMerger()
        {
            MBC.SetBuildType(MetaBuildType.Merger);
            CloseAllSubs();
        }

        public void OnClicKCrossing()
        {
            MBC.SetBuildType(MetaBuildType.Crossing);
            CloseAllSubs();
        }

        public void OnClickBlueprint()
        {
            MBC.SetBuildType(MetaBuildType.Blueprint);
            CloseAllSubs();
        }
    }
}
