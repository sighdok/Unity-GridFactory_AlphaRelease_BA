using System;

using UnityEngine;
using UnityEngine.EventSystems;

using GridFactory.Grid;
using GridFactory.Machines;
using GridFactory.Conveyor;
using GridFactory.Utils;
using GridFactory.Directions;
using GridFactory.UI;

namespace GridFactory.Core
{
    public enum BuildType
    {
        None,
        Sawmill,
        Oven,
        Mason,
        Smelter,
        Conveyor,
        InputPort,
        OutputPort,
        Splitter,
        Merger,
        Crossing,
        Erase
    }

    public enum MachineKind
    {
        Sawmill,
        Oven,
        Mason,
        Smelter,
        PortMarker,
        Splitter,
        Merger,
        Crossing,
        None
    }

    public enum PortKind
    {
        Input,
        Output
    }

    public class BuildController : MonoBehaviour
    {
        public static BuildController Instance { get; private set; }

        private static GridManager GrM => GridManager.Instance;
        private static BuildMenuUI BMUI => BuildMenuUI.Instance;

        [Header("Prefabs")]
        [SerializeField] private GameObject sawmillPrefab;
        [SerializeField] private GameObject masonPrefab;
        [SerializeField] private GameObject ovenPrefab;
        [SerializeField] private GameObject smelterPrefab;
        [SerializeField] private GameObject inputPortPrefab;
        [SerializeField] private GameObject outputPortPrefab;
        [SerializeField] private GameObject conveyorPrefab;
        [SerializeField] private GameObject splitterPrefab;
        [SerializeField] private GameObject mergerPrefab;
        [SerializeField] private GameObject crossingPrefab;

        [Header("Sprites")]
        [SerializeField] private Sprite sawmillSprite;
        [SerializeField] private Sprite masonSprite;
        [SerializeField] private Sprite ovenSprite;
        [SerializeField] private Sprite smelterSprite;
        [SerializeField] private Sprite inputPortSprite;  // für Ghost
        [SerializeField] private Sprite outputPortSprite;  // für Ghost
        [SerializeField] private Sprite splitterSprite;
        [SerializeField] private Sprite mergerSprite;
        [SerializeField] private Sprite crossingSprite;
        [SerializeField] private Sprite eraseSprite;
        [SerializeField] private Sprite conveyorStraightSprite;
        [SerializeField] private Sprite conveyorCornerSprite;

        [Header("Ghost Preview")]
        [SerializeField] private GameObject ghostRendererContainer;

        private SpriteRenderer[] _ghostRenderer;
        private SpriteRenderer _ghostMainRenderer;
        private BuildingGhost _ghost;
        private bool _ghostActive;
        private Vector2Int _ghostGridPos;
        private BuildType _currentBuildType = BuildType.None;
        private Direction _outputFacing = Direction.Right;
        private Direction _inputFacing = Direction.Left;
        private Vector3 _baseScale = Vector3.one;
        private Camera _camera;

        public Action<MachineBase> _onMachinePlaced;
        public Action<MachineBase> _onMachineDestroyed;

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
            _camera = Camera.main;
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
            if (GameManager.Instance.CurrentMode == GameMode.Meta)
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

            if (_ghostActive && _currentBuildType == BuildType.Conveyor)
                UpdateGhostConveyorVisual();

            if (Input.GetMouseButtonDown(0))
            {
                if (GameManager.Instance.CurrentControlScheme == "desktop")
                {
                    if (_currentBuildType == BuildType.None)
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

                    if (_currentBuildType == BuildType.None)
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

            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = GrM.WorldToGrid(worldPos);
            GridCell cell = GrM.GetCell(gridPos);

            worldPos.z = 0f;

            if (cell == null || cell.Machine == null)
            {
                UIConfigurationManager.Instance.Close();
                ClearBuildMenu();
                return;
            }

            var machineType = cell.Machine.GetMachineKind();

            if (machineType == MachineKind.PortMarker)
            {
                var port = cell.Machine as PortMarker;
                UIConfigurationManager.Instance.SetPortmarker(port);
            }
            else if (machineType == MachineKind.Sawmill || machineType == MachineKind.Mason || machineType == MachineKind.Oven || machineType == MachineKind.Smelter)
            {
                var machine = cell.Machine as MachineWithRecipeBase;
                UIConfigurationManager.Instance.SetMachineWithRecipeBase(machine);
            }
            else
            {
                UIConfigurationManager.Instance.Close();
                ClearBuildMenu();
            }
        }

        public void HandleBuildInput()
        {
            if (_currentBuildType == BuildType.None)
                return;

            GridCell cell = GrM.GetCell(_ghostGridPos);
            if (cell == null || cell.IsLocked)
                return;

            UIConfigurationManager.Instance.Close();
            ClearBuildMenu();

            if (_currentBuildType == BuildType.Erase)
                AudioManager.Instance.PlayDestroySFX();
            else
                AudioManager.Instance.PlayBuildSFX();

            switch (_currentBuildType)
            {
                case BuildType.Sawmill:
                    PlaceMachine(cell, sawmillPrefab);
                    break;
                case BuildType.Mason:
                    PlaceMachine(cell, masonPrefab);
                    break;
                case BuildType.Oven:
                    PlaceMachine(cell, ovenPrefab);
                    break;
                case BuildType.Smelter:
                    PlaceMachine(cell, smelterPrefab);
                    break;
                case BuildType.InputPort:
                    PlaceMachine(cell, inputPortPrefab);
                    break;
                case BuildType.OutputPort:
                    PlaceMachine(cell, outputPortPrefab);
                    break;
                case BuildType.Splitter:
                    PlaceMachine(cell, splitterPrefab);
                    break;
                case BuildType.Merger:
                    PlaceMachine(cell, mergerPrefab);
                    break;
                case BuildType.Crossing:
                    PlaceMachine(cell, crossingPrefab);
                    break;
                case BuildType.Conveyor:
                    PlaceConveyor(cell, conveyorPrefab);
                    break;
                case BuildType.Erase:
                    EraseAt(cell);
                    break;
            }

            GrM.UpdateSpriteSortingByY();
        }

        public void CancelBuilding()
        {
            if (_currentBuildType == BuildType.None)
                return;

            _currentBuildType = BuildType.None;

            PortBuildingController.Instance.DisablePortBuilding();
            UIConfigurationManager.Instance.Close();
            ClearBuildMenu();

            ResetDirectionAndGhost();
            _ghostActive = false;
            if (ghostRendererContainer != null)
                ghostRendererContainer.SetActive(false);
        }

        public void SetBuildType(BuildType type)
        {
            ResetDirectionAndGhost();
            _currentBuildType = type;

            if (_currentBuildType == BuildType.InputPort || _currentBuildType == BuildType.OutputPort)
                PortBuildingController.Instance.EnablePortBuilding(_currentBuildType);
            else
                PortBuildingController.Instance.DisablePortBuilding();

            UpdateGhostSprite();

            if (_currentBuildType == BuildType.InputPort || _currentBuildType == BuildType.OutputPort)
                PortBuildingController.Instance.ForcePortGhostRotation(ghostRendererContainer, _ghostGridPos);
        }
        private void PlaceConveyor(GridCell cell, GameObject prefab)
        {
            if (cell.Machine != null || cell.Conveyor != null)
                return;

            Vector3 pos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var conv = go.GetComponent<ConveyorBase>();
            if (conv != null)
            {
                conv.Init(cell);
                conv.SetOutputDirection(_outputFacing);
                conv.SetInputDirection(_inputFacing, true);
                cell.Conveyor = conv;
            }

            RefreshAllConveyors();

            if (GameManager.Instance.CurrentControlScheme == "touch")
                ForceConveyorGhostUpdate(_outputFacing);
        }

        private void PlaceMachine(GridCell cell, GameObject prefab)
        {
            if (cell.Machine != null || cell.Conveyor != null)
                return;

            Vector3 pos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var machine = go.GetComponent<MachineBase>();
            if (machine != null)
            {
                machine.Init(cell);

                if (machine is PortMarker)
                    machine.SetFacing(PortBuildingController.Instance.GetForcedFacing());
                else
                    machine.SetFacing(_outputFacing);
                cell.Machine = machine;

                if (machine is MachineWithRecipeBase mwrb)
                {
                    mwrb.BuildInputOutputSides();
                    mwrb.UpdateArrowPositions();
                }

                if (machine is PortMarker pm)
                {
                    PortBuildingController.Instance.RegisterPort(pm);
                }
            }

            RefreshAllConveyors();

            _onMachinePlaced?.Invoke(machine);
        }

        private void EraseAt(GridCell cell)
        {
            if (cell.Machine != null)
            {
                _onMachineDestroyed?.Invoke(cell.Machine);

                if (cell.Machine is PortMarker myMarker)
                    PortBuildingController.Instance.EreasePortmarker(myMarker);

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
            if (_currentBuildType == BuildType.None || _currentBuildType == BuildType.InputPort || _currentBuildType == BuildType.OutputPort)
                return;

            RotateFacing();
            UpdateGhostRotation(_currentBuildType == BuildType.Conveyor);

            AudioManager.Instance.PlayRotateSFX();
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
            if (!_ghostActive || ghostRendererContainer == null || _camera == null || GrM == null)
                return;

            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            _ghostGridPos = GrM.WorldToGrid(worldPos);
            GridCell cell = GrM.GetCell(_ghostGridPos);
            if (cell == null)
                return;

            Vector3 snappedWorld = GrM.GridToWorld(_ghostGridPos);
            ghostRendererContainer.transform.position = snappedWorld;

            if (_currentBuildType == BuildType.InputPort || _currentBuildType == BuildType.OutputPort)
                PortBuildingController.Instance.ForcePortGhostRotation(ghostRendererContainer, _ghostGridPos);
        }

        private void UpdateGhostRotation(bool isConv = false)
        {
            if (_ghostMainRenderer == null || ghostRendererContainer == null)
                return;
            if (_currentBuildType == BuildType.InputPort || _currentBuildType == BuildType.OutputPort)
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

            var machine = inputPortPrefab.GetComponent<MachineBase>();
            switch (_currentBuildType)
            {
                case BuildType.Sawmill:
                    sprite = sawmillSprite;
                    machine = sawmillPrefab.GetComponent<MachineWithRecipeBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Mason:
                    sprite = masonSprite;
                    machine = masonPrefab.GetComponent<MachineWithRecipeBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Oven:
                    sprite = ovenSprite;
                    machine = ovenPrefab.GetComponent<MachineWithRecipeBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Smelter:
                    sprite = smelterSprite;
                    machine = smelterPrefab.GetComponent<MachineWithRecipeBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Conveyor:
                    sprite = conveyorStraightSprite;
                    break;
                case BuildType.InputPort:
                    sprite = inputPortSprite;
                    machine = inputPortPrefab.GetComponent<MachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.OutputPort:
                    sprite = outputPortSprite;
                    machine = outputPortPrefab.GetComponent<MachineBase>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Splitter:
                    sprite = splitterSprite;
                    machine = splitterPrefab.GetComponent<Splitter>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Merger:
                    sprite = mergerSprite;
                    machine = mergerPrefab.GetComponent<Merger>();
                    _ghost.InitByDirections(machine.HasInput, machine.HasOutput, machine.AllInputDirections(), machine.AllOutputDirections());
                    _ghost.SetFacing(_outputFacing);
                    break;
                case BuildType.Crossing:
                    sprite = crossingSprite;
                    break;
                case BuildType.Erase:
                    sprite = eraseSprite;
                    break;
                case BuildType.None:
                    _ghostActive = false;
                    break;
            }

            _ghostMainRenderer.sprite = sprite;
            _ghostMainRenderer.transform.localScale = _baseScale;
            ghostRendererContainer.SetActive(_ghostActive);


            UpdateGhostRotation(_currentBuildType == BuildType.Conveyor);
        }

        private void UpdateGhostConveyorVisual()
        {
            if (!_ghostActive || ghostRendererContainer == null || _currentBuildType != BuildType.Conveyor)
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

            GridCell fCell = GrM.GetCell(fPos);
            GridCell bCell = GrM.GetCell(bPos);
            GridCell lCell = GrM.GetCell(lPos);
            GridCell rCell = GrM.GetCell(rPos);

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
                MachineKind machineKind = machine.GetMachineKind();
                if (machineKind != MachineKind.Splitter && machineKind != MachineKind.Crossing)
                {
                    if (lCell.Machine.OutputDirection != DirectionUtils.Opposite(_outputFacing) && (lCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetRight(_outputFacing))))
                    {
                        _inputFacing = DirectionUtils.GetLeft(_outputFacing);
                        leftCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MachineKind.Splitter)
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
                    else if (machineKind == MachineKind.Crossing)
                    {
                        Crossing cross = lCell.Machine as Crossing;

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
                MachineKind machineKind = machine.GetMachineKind();

                if (machineKind != MachineKind.Splitter && machineKind != MachineKind.Crossing)
                {
                    if (rCell.Machine.OutputDirection != DirectionUtils.Opposite(_outputFacing) && (rCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetLeft(_outputFacing))))
                    {
                        _inputFacing = DirectionUtils.GetRight(_outputFacing);
                        rightCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MachineKind.Splitter)
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
                    else if (machineKind == MachineKind.Crossing)
                    {
                        Crossing cross = rCell.Machine as Crossing;
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

        private void RefreshAllConveyors()
        {
            /*
                var allConv = FindObjectsOfType<ConveyorBase>();
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

