using UnityEngine;
using UnityEngine.UI;

using GridFactory.Core;

namespace GridFactory.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        public static BuildMenuUI Instance { get; private set; }

        public Button inputPortButton;
        public Button outputPortButton;

        [SerializeField] GameObject subConveyor;
        [SerializeField] GameObject subResource;
        [SerializeField] GameObject subMachine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }


        private void Start()
        {
            PortBuildingController.Instance.OnPortMarkerUpdated += TogglePortButtons;
        }

        public void OnClickConveyorToggle()
        {
            BuildController.Instance.CancelBuilding();
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
            BuildController.Instance.CancelBuilding();
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
            BuildController.Instance.CancelBuilding();
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

        private void TogglePortButtons(bool inputDisabled, bool outputDisabled)
        {

            if (inputDisabled == true)
            {
                inputPortButton.interactable = false;
            }
            else if (inputDisabled == false)
            {
                inputPortButton.interactable = true;
            }

            if (outputDisabled == true)
            {
                outputPortButton.interactable = false;
            }
            else if (outputDisabled == false)
            {
                outputPortButton.interactable = true;
            }

        }


        public void OnClickSmelter()
        {
            BuildController.Instance.SetBuildType(BuildType.Smelter);
            CloseAllSubs();
        }

        public void OnClickSawmill()
        {
            BuildController.Instance.SetBuildType(BuildType.Sawmill);
            CloseAllSubs();
        }
        public void OnClickMason()
        {
            BuildController.Instance.SetBuildType(BuildType.Mason);
            CloseAllSubs();
        }
        public void OnClickOven()
        {
            BuildController.Instance.SetBuildType(BuildType.Oven);
            CloseAllSubs();
        }



        public void OnClickConveyor()
        {
            BuildController.Instance.SetBuildType(BuildType.Conveyor);
            CloseAllSubs();
        }

        public void OnClickInputPort()
        {
            BuildController.Instance.SetBuildType(BuildType.InputPort);
            CloseAllSubs();
        }

        public void OnClickOutputPort()
        {
            BuildController.Instance.SetBuildType(BuildType.OutputPort);
            CloseAllSubs();
        }

        public void OnClickSplitter()
        {
            BuildController.Instance.SetBuildType(BuildType.Splitter);
            CloseAllSubs();
        }

        public void OnClickMerger()
        {
            BuildController.Instance.SetBuildType(BuildType.Merger);
            CloseAllSubs();
        }

        public void OnClicKCrossing()
        {
            BuildController.Instance.SetBuildType(BuildType.Crossing);
            CloseAllSubs();
        }
    }
}
