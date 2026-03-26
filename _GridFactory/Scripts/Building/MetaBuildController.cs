using System;

using UnityEngine;
using UnityEngine.EventSystems;

using GridFactory.Core;
using GridFactory.Utils;
using GridFactory.Grid;
using GridFactory.Blueprints;
using GridFactory.Conveyor;
using GridFactory.Directions;
using GridFactory.Tech;
using GridFactory.UI;

namespace GridFactory.Meta
{
    public enum MetaBuildType
    {
        None,
        ResourceNodeOre,
        ResourceNodeFarm,
        Market,
        PowerPlant,
        ResearchCenter,
        Conveyor,
        Splitter,
        Merger,
        Crossing,
        Blueprint,
        Erase
    }

    public enum MetaKind
    {
        Resource,
        Market,
        PowerPlant,
        ResearchCenter,
        Blueprint,
        Splitter,
        Merger,
        Crossing,
        None
    }

    public class MetaBuildController : MonoBehaviour
    {
        public static MetaBuildController Instance { get; private set; }

        private static MetaGridManager GrM => MetaGridManager.Instance;
        private static BuildMenuUI BMUI => BuildMenuUI.Instance;

        [Header("Prefabs")]
        [SerializeField] private GameObject resourceNodeOrePrefab;
        [SerializeField] private GameObject resourceNodeFarmPrefab;
        [SerializeField] private GameObject marketPrefab;
        [SerializeField] private GameObject powerPlantPrefab;
        [SerializeField] private GameObject metaConveyorPrefab;
        [SerializeField] private GameObject metaSplitterPrefab;
        [SerializeField] private GameObject metaMergerPrefab;
        [SerializeField] private GameObject metaCrossingPrefab;
        [SerializeField] private GameObject metaBlueprintModulePrefab;
        [SerializeField] private GameObject metaResearchCenterModulePrefab;

        [Header("Sprites")]
        [SerializeField] private Sprite resourceNodeOreSprite;
        [SerializeField] private Sprite resourceNodeFarmSprite;
        [SerializeField] private Sprite marketSprite;
        [SerializeField] private Sprite powerPlantSprite;
        [SerializeField] private Sprite blueprintSprite;
        [SerializeField] private Sprite researchCenterSprite;
        [SerializeField] private Sprite conveyorStraightSprite;
        [SerializeField] private Sprite conveyorCornerSprite;
        [SerializeField] private Sprite splitterSprite;
        [SerializeField] private Sprite mergerSprite;
        [SerializeField] private Sprite crossingSprite;
        [SerializeField] private Sprite eraseSprite;

        [Header("Ghost Preview")]
        [SerializeField] private GameObject ghostRendererContainer;

        private SpriteRenderer[] _ghostRenderer;
        private SpriteRenderer _ghostMainRenderer;
        private BuildingGhost _ghost;
        private bool _ghostActive;
        private Vector2Int _ghostGridPos;
        private MetaBuildType _currentBuildType = MetaBuildType.None;
        private BlueprintDefinition _selectedBlueprint;
        private Direction _outputFacing = Direction.Right;
        private Direction _inputFacing = Direction.Right;
        private Vector3 _baseScale = Vector3.one;
        private Camera _metaCamera;

        public Action<MetaMachineBase> _onMachinePlaced;
        public Action _onMachineRotated;

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
            _metaCamera = Camera.main;
            if (ghostRendererContainer != null)
            {
                ghostRendererContainer.SetActive(false);
                _ghost = ghostRendererContainer.GetComponent<BuildingGhost>();
                _ghostRenderer = ghostRendererContainer.GetComponentsInChildren<SpriteRenderer>();

                foreach (SpriteRenderer renderer in _ghostRenderer)
                {
                    if (renderer.name == "MainSprite")
                    {
                        _ghostMainRenderer = renderer;
                        _baseScale = _ghostMainRenderer.transform.localScale;
                    }
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentMode == GameMode.Blueprint)
                return;

            if (Input.GetMouseButtonDown(1))
            {
                CancelBuilding();
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
                DoRotation();

            if (GameManager.Instance.CurrentControlScheme == "desktop")
                UpdateGhostPosition();

            if (_ghostActive && _currentBuildType == MetaBuildType.Conveyor)
                UpdateGhostConveyorVisual();

            if (Input.GetMouseButtonDown(0))
            {
                if (GameManager.Instance.CurrentControlScheme == "desktop")
                {
                    if (_currentBuildType == MetaBuildType.None)
                        HandleSelectionInput();
                    else
                        HandleBuildInput();
                }
                else if (GameManager.Instance.CurrentControlScheme == "touch")
                {
                    if (UIUtils.ClickedOnUi())
                        return;

                    if (TouchInputManager.Instance.IsDoubleTapOnSameCell())
                    {
                        HandleBuildInput();
                        return;
                    }

                    if (_currentBuildType == MetaBuildType.None)
                        HandleSelectionInput();
                    else
                        UpdateGhostPosition();
                }
            }
        }

        private void HandleSelectionInput()
        {
            if (EventSystem.current.IsPointerOverGameObject() || UIUtils.ClickedOnUi())
                return;

            Vector3 worldPos = _metaCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = GrM.WorldToGrid(worldPos);
            MetaCell cell = GrM.GetCell(gridPos);

            worldPos.z = 0f;

            if (cell == null || cell.Machine == null)
            {
                UIConfigurationManager.Instance.Close();
                ClearBuildMenu();
                return;
            }

            if (cell.Machine is MetaResearchCenter center)
            {
                UIPanelManager.Instance.ToggleTechTreeUI(center);
            }
            else if (cell.Machine is MetaResourceNode node)
            {
                UIConfigurationManager.Instance.SetResourceNode(node);
            }
            else if (cell.Machine is MetaBlueprintModule bpm)
            {
                UIConfigurationManager.Instance.SetBlueprintModule(bpm);
            }
            else
            {
                UIConfigurationManager.Instance.Close();
                ClearBuildMenu();
            }
        }

        public void HandleBuildInput()
        {
            if (!TutorialGridFactoryController.Instance.AllowBuilding())
                return;

            if (_currentBuildType == MetaBuildType.None)
                return;

            MetaCell cell = GrM.GetCell(_ghostGridPos);
            if (cell == null)
                return;

            UIConfigurationManager.Instance.Close();
            ClearBuildMenu();

            if (_currentBuildType == MetaBuildType.Erase)
                AudioManager.Instance.PlayDestroySFX();
            else
                AudioManager.Instance.PlayBuildSFX();

            switch (_currentBuildType)
            {
                case MetaBuildType.ResourceNodeOre:
                    PlaceMachine(cell, resourceNodeOrePrefab);
                    break;
                case MetaBuildType.ResourceNodeFarm:
                    PlaceMachine(cell, resourceNodeFarmPrefab);
                    break;
                case MetaBuildType.Market:
                    PlaceMachine(cell, marketPrefab);
                    break;
                case MetaBuildType.PowerPlant:
                    PlaceMachine(cell, powerPlantPrefab);
                    break;
                case MetaBuildType.Conveyor:
                    PlaceConveyor(cell, metaConveyorPrefab);
                    break;
                case MetaBuildType.Merger:
                    PlaceMachine(cell, metaMergerPrefab);
                    break;
                case MetaBuildType.Splitter:
                    PlaceMachine(cell, metaSplitterPrefab);
                    break;
                case MetaBuildType.Crossing:
                    PlaceMachine(cell, metaCrossingPrefab);
                    break;
                case MetaBuildType.Blueprint:
                    PlaceBlueprintModule(cell, metaBlueprintModulePrefab);
                    break;
                case MetaBuildType.ResearchCenter:
                    PlaceMachine(cell, metaResearchCenterModulePrefab);
                    break;
                case MetaBuildType.Erase:
                    EraseAt(cell);
                    break;
            }

            GrM.UpdateSpriteSortingByY();
        }

        public void CancelBuilding()
        {
            if (!TutorialGridFactoryController.Instance.AllowBuildingCancel())
                return;
            if (_currentBuildType == MetaBuildType.None)
                return;

            _currentBuildType = MetaBuildType.None;
            _selectedBlueprint = null;

            UIConfigurationManager.Instance.Close();
            ClearBuildMenu();

            ResetDirectionAndGhost();
            _ghostActive = false;
            if (ghostRendererContainer != null)
                ghostRendererContainer.SetActive(false);
        }

        public void SetBuildType(MetaBuildType type)
        {
            ResetDirectionAndGhost();
            _currentBuildType = type;

            if (type == MetaBuildType.Blueprint)
                UIConfigurationManager.Instance.SetBlueprintBuild();
            else
                UpdateGhostSprite();
        }

        public void SetSelectedBlueprint(BlueprintDefinition bp)
        {
            ResetDirectionAndGhost();

            _selectedBlueprint = bp;

            if (_ghost != null && _selectedBlueprint != null)
            {
                _ghost.InitFromBlueprint(_selectedBlueprint);
                _ghost.SetFacing(_outputFacing);
            }

            UpdateGhostSprite();
        }

        private void PlaceConveyor(MetaCell cell, GameObject prefab)
        {
            if (cell.Machine != null || cell.Conveyor != null || prefab == null)
                return;

            Vector3 pos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var conv = go.GetComponent<MetaConveyorBase>();
            if (conv != null)
            {
                conv.Init(cell);
                conv.SetOutputDirection(_outputFacing);
                conv.SetInputDirection(_inputFacing, true);
                cell.Conveyor = conv;
            }

            RefreshAllMetaConveyors();

            if (GameManager.Instance.CurrentControlScheme == "touch")
                ForceConveyorGhostUpdate(_outputFacing);
        }

        private void PlaceMachine(MetaCell cell, GameObject prefab)
        {
            if (cell.Machine != null || cell.Conveyor != null)
                return;

            Vector3 pos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var machine = go.GetComponent<MetaMachineBase>();
            if (machine != null)
            {
                machine.Init(cell);
                machine.SetFacing(_outputFacing);
                cell.Machine = machine;
                if (machine is MetaResearchCenter mrsc)
                {
                    UIPanelManager.Instance.OpenTechTreeUI(mrsc);
                    CancelBuilding();
                }
            }

            RefreshAllMetaConveyors();

            _onMachinePlaced?.Invoke(machine);
        }

        private void PlaceBlueprintModule(MetaCell cell, GameObject prefab)
        {
            if (cell.Machine != null || cell.Conveyor != null || prefab == null || _selectedBlueprint == null)
                return;


            Vector3 pos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var module = go.GetComponent<MetaBlueprintModule>();
            if (module != null)
            {
                module.Init(cell);
                module.SetFacing(_outputFacing, false);
                module.InitWithBlueprint(_selectedBlueprint);
                cell.Machine = module;
            }

            RefreshAllMetaConveyors();

            _onMachinePlaced?.Invoke(module);
        }

        private void EraseAt(MetaCell cell)
        {
            if (cell.Machine != null)
            {
                if (cell.Machine is MetaResearchCenter mrs)
                    TechTreeManager.Instance.CloseOnResearchCenterDeletion(mrs);

                Destroy(cell.Machine.gameObject);
                cell.Machine = null;
            }

            if (cell.Conveyor != null)
            {
                Destroy(cell.Conveyor.gameObject);
                cell.Conveyor = null;
            }
        }

        public void DoRotation()
        {
            if (_currentBuildType == MetaBuildType.None)
                return;

            RotateFacing();
            UpdateGhostRotation(_currentBuildType == MetaBuildType.Conveyor);

            AudioManager.Instance.PlayRotateSFX();

            _onMachineRotated?.Invoke();
        }

        private void RotateFacing()
        {
            switch (_outputFacing)
            {
                case Direction.Right: _outputFacing = Direction.Down; break;
                case Direction.Down: _outputFacing = Direction.Left; break;
                case Direction.Left: _outputFacing = Direction.Up; break;
                case Direction.Up: _outputFacing = Direction.Right; break;
            }
        }

        private void ResetDirectionAndGhost()
        {
            _outputFacing = Direction.Right;
            _inputFacing = Direction.Left;

            ghostRendererContainer.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            _ghostMainRenderer.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            ghostRendererContainer.transform.localScale = _baseScale;

            _ghost.Cleanup();
        }

        private void UpdateGhostPosition()
        {
            if (!_ghostActive || ghostRendererContainer == null || _metaCamera == null)
                return;

            Vector3 worldPos = _metaCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;

            _ghostGridPos = GrM.WorldToGrid(worldPos);

            MetaCell cell = GrM.GetCell(_ghostGridPos);
            if (cell == null)
                return;

            Vector3 snappedWorld = GrM.GridToWorld(_ghostGridPos);
            ghostRendererContainer.transform.position = snappedWorld;
        }

        private void UpdateGhostRotation(bool isConv = false)
        {
            if (_ghostMainRenderer == null || ghostRendererContainer == null)
                return;

            float angle = 0f;
            switch (_outputFacing)
            {
                case Direction.Right: angle = 0f; break;
                case Direction.Up: angle = 90f; break;
                case Direction.Left: angle = 180f; break;
                case Direction.Down: angle = 270f; break;
            }

            if (!isConv)
                _ghostMainRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _ghost.SetFacing(_outputFacing);
        }

        private void UpdateGhostSprite()
        {
            if (_ghostMainRenderer == null) return;

            Sprite sprite = null;
            _ghostActive = true;
            _outputFacing = Direction.Right;

            var machine = resourceNodeOrePrefab.GetComponent<MetaMachineBase>();
            switch (_currentBuildType)
            {
                case MetaBuildType.ResourceNodeOre:
                    sprite = resourceNodeOreSprite;
                    machine = resourceNodeOrePrefab.GetComponent<MetaMachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.ResourceNodeFarm:
                    sprite = resourceNodeFarmSprite;
                    machine = resourceNodeFarmPrefab.GetComponent<MetaMachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.Market:
                    sprite = marketSprite;
                    machine = marketPrefab.GetComponent<MetaMachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.PowerPlant:
                    sprite = powerPlantSprite;
                    machine = powerPlantPrefab.GetComponent<MetaMachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.ResearchCenter:
                    sprite = researchCenterSprite;
                    machine = metaResearchCenterModulePrefab.GetComponent<MetaMachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.Blueprint:
                    sprite = blueprintSprite;
                    break;
                case MetaBuildType.Conveyor:
                    sprite = conveyorStraightSprite;
                    break;
                case MetaBuildType.Splitter:
                    sprite = splitterSprite;
                    machine = metaSplitterPrefab.GetComponent<MetaSplitter>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.Merger:
                    sprite = mergerSprite;
                    machine = metaMergerPrefab.GetComponent<MetaMerger>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case MetaBuildType.Crossing:
                    sprite = crossingSprite;
                    break;
                case MetaBuildType.Erase:
                    sprite = eraseSprite;
                    break;
                case MetaBuildType.None:
                    _ghostActive = false;
                    break;
            }

            _ghostMainRenderer.sprite = sprite;
            _ghostMainRenderer.transform.localScale = _baseScale;
            ghostRendererContainer.SetActive(_ghostActive);

            UpdateGhostRotation(_currentBuildType == MetaBuildType.Conveyor);
        }

        private void UpdateGhostConveyorVisual()
        {
            if (!_ghostActive || ghostRendererContainer == null || _currentBuildType != MetaBuildType.Conveyor)
                return;

            _ghostMainRenderer.sprite = conveyorStraightSprite;

            float angle = 0f;
            switch (_outputFacing)
            {
                case Direction.Right: angle = 0f; break;
                case Direction.Up: angle = 90f; break;
                case Direction.Left: angle = 180f; break;
                case Direction.Down: angle = 270f; break;
            }
            _inputFacing = DirectionUtils.Opposite(_outputFacing);

            ghostRendererContainer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            ghostRendererContainer.transform.localScale = _baseScale;

            var lPos = _ghostGridPos + DirectionUtils.DirectionToOffset(DirectionUtils.GetLeft(_outputFacing));
            var rPos = _ghostGridPos + DirectionUtils.DirectionToOffset(DirectionUtils.GetRight(_outputFacing));
            var fPos = _ghostGridPos + DirectionUtils.DirectionToOffset(_outputFacing);
            var bPos = _ghostGridPos + DirectionUtils.DirectionToOffset(DirectionUtils.Opposite(_outputFacing));

            MetaCell fCell = GrM.GetCell(fPos);
            MetaCell bCell = GrM.GetCell(bPos);
            MetaCell lCell = GrM.GetCell(lPos);
            MetaCell rCell = GrM.GetCell(rPos);

            bool leftCorner = false;
            bool rightCorner = false;

            bool hasLeftCon = lCell != null && lCell.Conveyor != null;
            bool hasRightCon = rCell != null && rCell.Conveyor != null;

            bool hasLeftMachine = lCell != null && lCell.Machine != null;
            bool hasRightMachine = rCell != null && rCell.Machine != null;

            if (hasLeftCon)
            {
                var cellInLeftNeighborOutputDirection = lCell.Position + DirectionUtils.DirectionToOffset(lCell.Conveyor.OutputDirection);
                if (lCell.Conveyor.OutputDirection != DirectionUtils.Opposite(_outputFacing) && cellInLeftNeighborOutputDirection == _ghostGridPos)
                {
                    _inputFacing = DirectionUtils.GetLeft(_outputFacing);
                    leftCorner = true;
                }
            }

            if (hasRightCon)
            {
                var cellInRightNeighborOutputDirection = rCell.Position + DirectionUtils.DirectionToOffset(rCell.Conveyor.OutputDirection);
                if (rCell.Conveyor.OutputDirection != DirectionUtils.Opposite(_outputFacing) && cellInRightNeighborOutputDirection == _ghostGridPos)
                {
                    _inputFacing = DirectionUtils.GetRight(_outputFacing);
                    rightCorner = true;
                }
            }
            if (hasLeftMachine)
            {
                var machine = lCell.Machine;
                MetaKind machineKind = machine.GetMetaKind();
                if (machineKind != MetaKind.Splitter && machineKind != MetaKind.Crossing)
                {
                    if (lCell.Machine.OutputDirection != DirectionUtils.Opposite(_outputFacing) && (lCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetRight(_outputFacing))))
                    {
                        _inputFacing = DirectionUtils.GetLeft(_outputFacing);
                        leftCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MetaKind.Splitter)
                    {
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != lCell.Machine.InputDirection)
                            {
                                if (dir == DirectionUtils.Opposite(DirectionUtils.GetLeft(_outputFacing)) && dir != _outputFacing)
                                {
                                    _inputFacing = DirectionUtils.GetLeft(_outputFacing);
                                    leftCorner = true;
                                }
                            }
                        }
                    }
                    else if (machineKind == MetaKind.Crossing)
                    {
                        MetaCrossing cross = lCell.Machine as MetaCrossing;
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != cross.InputDirection && dir != cross.AllExtraInpuDirections[0])
                            {
                                if (dir == DirectionUtils.Opposite(DirectionUtils.GetLeft(_outputFacing)) && dir != _outputFacing)
                                {
                                    _inputFacing = DirectionUtils.GetLeft(_outputFacing);
                                    leftCorner = true;
                                }
                            }

                        }
                    }
                }
            }

            if (hasRightMachine)
            {
                var machine = rCell.Machine;
                MetaKind machineKind = machine.GetMetaKind();

                if (machineKind != MetaKind.Splitter && machineKind != MetaKind.Crossing)
                {
                    if (rCell.Machine.OutputDirection != DirectionUtils.Opposite(_outputFacing) && (rCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetLeft(_outputFacing))))
                    {
                        _inputFacing = DirectionUtils.GetRight(_outputFacing);
                        rightCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MetaKind.Splitter)
                    {
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != rCell.Machine.InputDirection)
                            {
                                if (dir == DirectionUtils.Opposite(DirectionUtils.GetRight(_outputFacing)) && dir != _outputFacing)
                                {
                                    _inputFacing = DirectionUtils.GetRight(_outputFacing);
                                    rightCorner = true;
                                }
                            }
                        }
                    }
                    else if (machineKind == MetaKind.Crossing)
                    {
                        MetaCrossing cross = rCell.Machine as MetaCrossing;
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != cross.InputDirection && dir != cross.AllExtraInpuDirections[0])
                            {
                                if (dir == DirectionUtils.Opposite(DirectionUtils.GetRight(_outputFacing)) && dir != _outputFacing)
                                {
                                    _inputFacing = DirectionUtils.GetRight(_outputFacing);
                                    rightCorner = true;
                                }
                            }
                        }
                    }
                }
            }

            bool isCorner = leftCorner || rightCorner;

            if (bCell != null && bCell.Conveyor != null
               && bCell.Conveyor.OutputDirection == DirectionUtils.Opposite(_outputFacing)
               && bCell.Conveyor.InputDirection == DirectionUtils.Opposite(bCell.Conveyor.OutputDirection))
            {
                isCorner = false;
            }

            if (bCell != null && bCell.Machine && bCell.Machine.OutputDirection == DirectionUtils.Opposite(_outputFacing))
            {
                isCorner = false;
            }

            if (fCell != null && ((fCell.Conveyor != null && fCell.Conveyor.OutputDirection == DirectionUtils.Opposite(_outputFacing)) || (fCell.Machine && fCell.Machine.OutputDirection == DirectionUtils.Opposite(_outputFacing))))
            {
                isCorner = false;
            }

            if (isCorner)
            {
                CornerVisual vis = DirectionUtils.GetCornerVisual(_outputFacing, _inputFacing);

                _ghostMainRenderer.sprite = conveyorCornerSprite;
                ghostRendererContainer.transform.rotation = Quaternion.Euler(0f, 0f, vis.angle);
                ghostRendererContainer.transform.localScale = new Vector3(
                    _baseScale.x * (vis.flipX ? -1f : 1f),
                    _baseScale.y * (vis.flipY ? -1f : 1f),
                    _baseScale.z
                );
            }
        }

        private void ForceConveyorGhostUpdate(Direction dir)
        {
            _ghostGridPos = _ghostGridPos + DirectionUtils.DirectionToOffset(dir);
            Vector3 snappedWorld = GrM.GridToWorld(_ghostGridPos);
            ghostRendererContainer.transform.position = snappedWorld;
        }

        public void ForceGhostToPosition_Tutorial(Vector2Int pos)
        {
            if (!_ghostActive || ghostRendererContainer == null || _metaCamera == null)
                return;

            _ghostGridPos = pos;
            Vector3 snappedWorld = GrM.GridToWorld(_ghostGridPos);
            ghostRendererContainer.transform.position = snappedWorld;
        }

        private void RefreshAllMetaConveyors()
        {
            /*
            var allConv = FindObjectsOfType<MetaConveyorBase>();
            foreach (var c in allConv)
            {
                c.UpdateShapeAndRotation();
            }
            */
        }

        private void ClearBuildMenu()
        {
            BMUI.CloseAllSubs();
        }
    }
}
