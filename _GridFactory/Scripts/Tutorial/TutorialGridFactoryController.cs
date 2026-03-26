using UnityEngine;

using NINESOFT.TUTORIAL_SYSTEM;
using DialogueQuests;

using GridFactory.Machines;
using GridFactory.Meta;
using GridFactory.Core;
using GridFactory.Inventory;
using GridFactory.Tech;
using GridFactory.Blueprints;

public class TutorialGridFactoryController : MonoBehaviour
{
    public static TutorialGridFactoryController Instance { get; private set; }

    private static TutorialManager TM => TutorialManager.Instance;
    private static CameraController CC => CameraController.Instance;
    private static MetaBuildController MBC => MetaBuildController.Instance;
    private static BuildController BC => BuildController.Instance;
    private static TechTreeManager TTM => TechTreeManager.Instance;
    private static GridShopManager GSM => GridShopManager.Instance;
    private static BlueprintManager BPM => BlueprintManager.Instance;
    private static InventoryManager IM => InventoryManager.Instance;
    private static UIPanelManager UIPM => UIPanelManager.Instance;
    private static UIConfigurationManager UICONFIGM => UIConfigurationManager.Instance;

    [SerializeField] private MissionPanel missionPanel;
    [SerializeField] private QuestData tutorialQuest;
    [SerializeField] private float inputNodeCounter = 0;

    public float minDistancePanPixel = 100f;

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
        CC._onPan += OnPan;
        CC._onZoom += OnZoom;

        MBC._onMachinePlaced += OnPlaceMetaMachine;
        MBC._onMachineRotated += OnRotateMetaMachine;

        BC._onMachinePlaced += OnPlaceMachine;
        BC._onMachineDestroyed += OnDestroyMachine;

        TTM._onResearchSelected += OnResearchSelected;
        TTM._onResearchCompleted += OnResearchCompleted;

        GSM._onGridBuy += OnGridBuy;

        BPM._onBlueprintSaved += OnBlueprintSaved;

        UICONFIGM._onConfigurationChanged += OnConfigurationChanged;
        UICONFIGM._onRecipeChanged += OnRecipeChanged;

        IM.OnGoldAdded += OnGoldGained;
    }

    private void OnDisable()
    {
        CC._onPan -= OnPan;
        CC._onZoom -= OnZoom;

        MBC._onMachinePlaced -= OnPlaceMetaMachine;
        MBC._onMachineRotated -= OnRotateMetaMachine;

        BC._onMachinePlaced -= OnPlaceMachine;
        BC._onMachineDestroyed -= OnDestroyMachine;

        TTM._onResearchSelected -= OnResearchSelected;
        TTM._onResearchCompleted -= OnResearchCompleted;

        GSM._onGridBuy -= OnGridBuy;

        BPM._onBlueprintSaved -= OnBlueprintSaved;

        UICONFIGM._onConfigurationChanged -= OnConfigurationChanged;
        UICONFIGM._onRecipeChanged -= OnRecipeChanged;

        IM.OnGoldAdded -= OnGoldGained;
    }

    public void StartTutorial()
    {
        UIPM.CloseAllBuildTabs();
        UIPM.CloseAll();
        TM.StageStarted(0, 0);
    }

    public void EndTutorialQuest()
    {
        NarrativeManager.Get().CompleteQuest(tutorialQuest);
        RefreshQuestPanel();
    }

    public void RefreshQuestPanel()
    {
        missionPanel.CheckForUpdate();
    }

    private void OnZoom()
    {
        if (TM.IsStageStarted(1, 1))
            TM.StageCompleted(1, 1);
    }

    private void OnPan(bool forTutorial = false)
    {
        if (forTutorial)
            if (TM.IsStageStarted(1, 2))
                TM.StageCompleted(1, 2);
    }

    private void OnPlaceMetaMachine(MetaMachineBase machine)
    {
        if (machine is MetaResourceNode)
        {
            if (TM.IsStageStarted(2, 3))
                TM.StageCompleted(2, 3);
        }
        else if (machine is MetaPowerPlant)
        {
            if (TM.IsStageStarted(3, 3))
                TM.StageCompleted(3, 3);
        }
        else if (machine is MetaResearchCenter)
        {
            if (TM.IsStageStarted(5, 1))
                TM.StageCompleted(5, 1);
        }
        else if (machine is MetaMarket)
        {
            if (TM.IsStageStarted(6, 1))
                TM.StageCompleted(6, 1);
        }
        else if (machine is MetaBlueprintModule)
        {
            if (TM.IsStageStarted(12, 2))
                TM.StageCompleted(12, 2);
        }
    }

    private void OnPlaceMachine(MachineBase machine)
    {
        if (machine is PortMarker pmi && pmi.portKind == PortKind.Input)
        {
            inputNodeCounter++;
            if (inputNodeCounter == 2)
                if (TM.IsStageStarted(8, 1))
                    TM.StageCompleted(8, 1);

        }
        else if (machine is PortMarker pmo && pmo.portKind == PortKind.Output)
        {

            if (TM.IsStageStarted(8, 3))
                TM.StageCompleted(8, 3);
        }
        else if (machine is Oven)
        {

            if (TM.IsStageStarted(9, 1))
                TM.StageCompleted(9, 1);
        }
    }

    private void OnDestroyMachine(MachineBase machine)
    {
        if (machine is PortMarker pmi && pmi.portKind == PortKind.Input)
        {
            if (TM.IsStageStarted(8, 2))
                TM.StageCompleted(8, 2);
        }
    }

    private void OnRotateMetaMachine()
    {
        if (TM.IsStageStarted(3, 2))
            TM.StageCompleted(3, 2);
    }

    private void OnGoldGained(int amount)
    {
        if (IM.Gold >= 50)
            if (TM.IsStageStarted(6, 2))
                TM.StageCompleted(6, 2);
    }

    private void OnResearchSelected(string researchId)
    {
        if (researchId == "market")
            if (TM.IsStageStarted(5, 2))
                TM.StageCompleted(5, 2);

    }

    private void OnResearchCompleted(string researchId)
    {

        if (researchId == "market")
            if (TM.IsStageStarted(5, 3))
                TM.StageCompleted(5, 3);
        if (researchId == "blueprint")
            if (TM.IsStageStarted(6, 4))
                TM.StageCompleted(6, 4);
    }

    private void OnConfigurationChanged(ItemType type)
    {
        if (TM.IsStageStarted(6, 3))
            if (type == ItemType.IronOre)
                TM.StageCompleted(6, 3);
    }

    private void OnRecipeChanged(RecipeDefinition recipe)
    {
        if (TM.IsStageStarted(9, 2))
            if (recipe.id == "coal")
                TM.StageCompleted(9, 2);
    }

    private void OnBlueprintSaved()
    {
        if (TM.IsStageStarted(11, 2))
        {
            TM.StageCompleted(11, 2);
            UIPM.ToggleBlueprintUI();
        }
    }

    public bool AllowBuildingCancel()
    {
        if (TM.IsStageStarted(2, 3) || TM.IsStageStarted(3, 2))
            return false;
        return true;
    }

    public bool AllowBuilding()
    {
        if (TM.IsStageStarted(3, 2))
            return false;
        return true;

    }

    public void OutputPortReceivedItem(ItemType type)
    {
        if (TM.IsStageStarted(10, 1))
            if (type == ItemType.Coal)
                TM.StageCompleted(10, 1);
    }

    public void FuelBurned(ItemType type)
    {
        if (TM.IsStageStarted(4, 2))
            if (type == ItemType.Wood)
                TM.StageCompleted(4, 2);
        if (TM.IsStageStarted(13, 2))
            if (type == ItemType.Coal)
                TM.StageCompleted(13, 2);
    }

    public void ItemSold(ItemType type)
    {
        if (TM.IsStageStarted(13, 2))
            if (type == ItemType.Coal)
                TM.StageCompleted(13, 2);
    }

    public void BlueprintModuleReceivedItem(ItemType type)
    {
        if (TM.IsStageStarted(13, 1))
            if (type == ItemType.Wood)
                TM.StageCompleted(13, 1);
    }

    public void OnGridBuy()
    {
        if (TM.IsStageStarted(14, 2))
            TM.StageCompleted(14, 2);
    }

    public void SetGhostToTargetCell()
    {
        if (TM.IsStageStarted(3, 2))
            MBC.ForceGhostToPosition_Tutorial(new Vector2Int(2, 0));
    }
}
