using UnityEngine;
using UnityEngine.InputSystem;

using GridFactory.Grid;
using GridFactory.Meta;

namespace GridFactory.Core
{
    public enum GameMode
    {
        Blueprint,
        Meta
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private static UIPanelManager UIPM => UIPanelManager.Instance;
        private static CameraController CC => CameraController.Instance;
        private static AudioManager AM => AudioManager.Instance;
        private static BuildController BC => BuildController.Instance;
        private static GridManager GrM => GridManager.Instance;
        private static MetaGridManager MGrM => MetaGridManager.Instance;
        private static MetaBuildController MBC => MetaBuildController.Instance;
        private static SaveLoadManager SLM => SaveLoadManager.Instance;

        [Header("Mode Roots")]
        [SerializeField] private GameObject blueprintRoot;
        [SerializeField] private GameObject blueprintUI;
        [SerializeField] private GameObject blueprintTiles;
        [SerializeField] private GameObject metaRoot;
        [SerializeField] private GameObject metaUI;
        [SerializeField] private GameObject metaTiles;

        [Header("Buttons")]
        [SerializeField] private GameObject clearGridButton;

        private GameMode _currentMode = GameMode.Meta;
        private string _currentControlScheme;

        public GameMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
            }
        }

        public string CurrentControlScheme
        {
            get => _currentControlScheme;
            set
            {
                _currentControlScheme = value;
            }
        }

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
            CurrentControlScheme = "desktop";
#if UNITY_ANDROID || UNITY_IOS
            CurrentControlScheme = "touch";
#endif
            ApplyMode(CurrentMode, false);
            SLM.ForceLoadGame();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                ToggleMode();
        }

        private void OnApplicationQuit()
        {
            //SaveLoadManager.Instance.ForceSaveGame();
        }

        public void HandleBuildButtonPress()
        {
            if (CurrentMode == GameMode.Blueprint)
                BC.HandleBuildInput();
            else
                MBC.HandleBuildInput();
        }

        public void HandleRotationButtonPress()
        {
            if (CurrentMode == GameMode.Blueprint)
                BC.DoRotation();
            else
                MBC.DoRotation();
        }

        public void HandleCancelButtonPress()
        {
            if (CurrentMode == GameMode.Blueprint)
                BC.CancelBuilding();
            else
                MBC.CancelBuilding();
        }

        public void EnableDeconstructionMode()
        {
            if (CurrentMode == GameMode.Blueprint)
            {
                BC.SetBuildType(BuildType.Erase);
                UIPM.CloseMicroBuildTabs();
            }
            else
            {
                MBC.SetBuildType(MetaBuildType.Erase);
                UIPM.CloseMetaBuildTabs();
            }
        }

        public void HandleToggleButtonPress()
        {
            ToggleMode();
        }

        public void ResetSimulation()
        {
            if (CurrentMode == GameMode.Blueprint)
            {
                var allCells = GrM.AllCells;
                foreach (var cell in allCells)
                {
                    if (cell.Conveyor != null)
                        cell.Conveyor.CurrentItem = null;
                    if (cell.Machine != null)
                        cell.Machine.ResetSimulation();
                }
            }
            else if (CurrentMode == GameMode.Meta)
            {
                var allCells = MGrM.AllCells;
                foreach (var cell in allCells)
                {
                    if (cell.Conveyor != null)
                        cell.Conveyor.CurrentItem = null;
                    if (cell.Machine != null)
                        cell.Machine.ResetSimulation();
                }
            }
        }

        private void ToggleMode()
        {
            CurrentMode = (CurrentMode == GameMode.Blueprint)
                ? GameMode.Meta
                : GameMode.Blueprint;

            ApplyMode(CurrentMode);
        }

        public void SetMode(GameMode mode, bool playSound = true)
        {
            CurrentMode = mode;
            ApplyMode(CurrentMode, playSound);
        }

        private void ApplyMode(GameMode mode, bool playSound = true)
        {
            UIPM.CloseAllConfigPanels();

            if (playSound)
                AM.PlayModeSwitchSFX();

            if (mode == GameMode.Blueprint)
            {
                UIPM.CloseMetaBuildTabs();
                CC.ResetCamera();
                AM.SwitchAmbient(true);

                clearGridButton.SetActive(true);

                metaUI.SetActive(false);
                metaTiles.SetActive(false);

                blueprintTiles.SetActive(true);
                blueprintUI.SetActive(true);

                blueprintRoot.transform.position = new Vector3(0, 0, 0);
                metaRoot.transform.position = new Vector3(0, -100, 0);

                var allMetaMachines = FindObjectsByType<MetaMachineBase>(FindObjectsSortMode.None);
                foreach (var c in allMetaMachines)
                    c.PauseEffects();
            }
            else
            {
                UIPM.CloseMicroBuildTabs();
                CC.ResetCamera();
                AM.SwitchAmbient(false);

                clearGridButton.SetActive(false);

                blueprintUI.SetActive(false);
                blueprintTiles.SetActive(false);

                metaTiles.SetActive(true);
                metaUI.SetActive(true);

                metaRoot.transform.position = new Vector3(0, 0, 0);
                blueprintRoot.transform.position = new Vector3(0, -100, 0);

                var allMetaMachines = FindObjectsByType<MetaMachineBase>(FindObjectsSortMode.None);
                foreach (var c in allMetaMachines)
                    c.ResumeEffects();
            }
        }
    }
}