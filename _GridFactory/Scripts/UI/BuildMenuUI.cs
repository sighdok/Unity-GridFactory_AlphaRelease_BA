using UnityEngine;
using UnityEngine.UI;

using GridFactory.Core;

namespace GridFactory.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        public static BuildMenuUI Instance { get; private set; }

        private static BuildController BC => BuildController.Instance;
        private static PortBuildingController PBC => PortBuildingController.Instance;

        [SerializeField] private Button inputPortButton;
        [SerializeField] private Button outputPortButton;
        [SerializeField] private GameObject subConveyor;
        [SerializeField] private GameObject subResource;
        [SerializeField] private GameObject subMachine;

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
            PBC.OnPortMarkerUpdated += TogglePortButtons;
        }

        public void OnClickConveyorToggle()
        {
            BC.CancelBuilding();
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
            BC.CancelBuilding();
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
            BC.CancelBuilding();
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
                inputPortButton.interactable = false;
            else if (inputDisabled == false)
                inputPortButton.interactable = true;

            if (outputDisabled == true)
                outputPortButton.interactable = false;
            else if (outputDisabled == false)
                outputPortButton.interactable = true;
        }


        public void OnClickSmelter()
        {
            BC.SetBuildType(BuildType.Smelter);
            CloseAllSubs();
        }

        public void OnClickSawmill()
        {
            BC.SetBuildType(BuildType.Sawmill);
            CloseAllSubs();
        }
        public void OnClickMason()
        {
            BC.SetBuildType(BuildType.Mason);
            CloseAllSubs();
        }
        public void OnClickOven()
        {
            BC.SetBuildType(BuildType.Oven);
            CloseAllSubs();
        }

        public void OnClickConveyor()
        {
            BC.SetBuildType(BuildType.Conveyor);
            CloseAllSubs();
        }

        public void OnClickInputPort()
        {
            BC.SetBuildType(BuildType.InputPort);
            CloseAllSubs();
        }

        public void OnClickOutputPort()
        {
            BC.SetBuildType(BuildType.OutputPort);
            CloseAllSubs();
        }

        public void OnClickSplitter()
        {
            BC.SetBuildType(BuildType.Splitter);
            CloseAllSubs();
        }

        public void OnClickMerger()
        {
            BC.SetBuildType(BuildType.Merger);
            CloseAllSubs();
        }

        public void OnClicKCrossing()
        {
            BC.SetBuildType(BuildType.Crossing);
            CloseAllSubs();
        }
    }
}
