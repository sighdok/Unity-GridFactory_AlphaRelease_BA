using System;
using System.Collections.Generic;

using UnityEngine;

using GridFactory.Directions;
using GridFactory.Grid;
using GridFactory.Machines;

namespace GridFactory.Core
{
    public class PortBuildingController : MonoBehaviour
    {
        public static PortBuildingController Instance { get; private set; }
        private static GridManager GrM => GridManager.Instance;
        private static BuildController BC => BuildController.Instance;

        [SerializeField] private GameObject rotationButtonGameObject;

        private Dictionary<PortMarker, GridEdge> _addedInputPorts = new Dictionary<PortMarker, GridEdge>();
        private Dictionary<PortMarker, GridEdge> _addedOutputPorts = new Dictionary<PortMarker, GridEdge>();
        private PortKind _currentPortKind;
        private GridEdge _lastRegisteredEdge = GridEdge.None;

        public event Action<bool, bool> OnPortMarkerUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void EnablePortBuilding(BuildType type)
        {
            rotationButtonGameObject.SetActive(false);

            if (type == BuildType.InputPort)
                _currentPortKind = PortKind.Input;
            if (type == BuildType.OutputPort)
                _currentPortKind = PortKind.Output;

            GrM.LockPortCells();
            UpdateLocks();
        }

        public void DisablePortBuilding()
        {
            rotationButtonGameObject.SetActive(true);
            _lastRegisteredEdge = GridEdge.None;

            GrM.UnlockAllSides();
            GrM.UnlockPortCells();
        }

        public void ResetPorts()
        {
            _addedInputPorts.Clear();
            _addedOutputPorts.Clear();

            _lastRegisteredEdge = GridEdge.None;

            OnPortMarkerUpdated?.Invoke(false, false);
            UpdateLocks();
        }

        public void EreasePortmarker(PortMarker marker)
        {
            if (marker.portKind == PortKind.Input)
            {
                if (_addedInputPorts.ContainsKey(marker))
                    _addedInputPorts.Remove(marker);
                OnPortMarkerUpdated?.Invoke(_addedInputPorts.Count == 3, _addedOutputPorts.Count == 1);
            }
            else if (marker.portKind == PortKind.Output)
            {
                if (_addedOutputPorts.ContainsKey(marker))
                    _addedOutputPorts.Remove(marker);
                OnPortMarkerUpdated?.Invoke(_addedInputPorts.Count == 3, _addedOutputPorts.Count == 1);
            }
        }

        public void ForcePortGhostRotation(GameObject ghostRender, Vector2Int cellPos)
        {
            if (!IsOnEdge(cellPos)) return;

            float angle = 0f;
            _lastRegisteredEdge = GetSingleEdge(cellPos);
            if (_currentPortKind == PortKind.Input)
            {
                switch (_lastRegisteredEdge)
                {
                    case GridEdge.Left: angle = 0f; break;
                    case GridEdge.Bottom: angle = 90f; break;
                    case GridEdge.Right: angle = 180f; break;
                    case GridEdge.Top: angle = 270f; break;
                }
            }
            else if (_currentPortKind == PortKind.Output)
            {
                switch (_lastRegisteredEdge)
                {
                    case GridEdge.Left: angle = 180f; break;
                    case GridEdge.Bottom: angle = 270f; break;
                    case GridEdge.Right: angle = 0f; break;
                    case GridEdge.Top: angle = 90f; break;
                }
            }
            ghostRender.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetPortOnCell(PortMarker marker, Vector2Int cellPos)
        {
            GridEdge myEdge = GetSingleEdge(cellPos);

            if (marker.portKind == PortKind.Input)
                _addedInputPorts.Add(marker, myEdge);
            else if (marker.portKind == PortKind.Output)
                _addedOutputPorts.Add(marker, myEdge);

            OnPortMarkerUpdated?.Invoke(_addedInputPorts.Count == 3, _addedOutputPorts.Count == 1);
        }

        public void RegisterPort(PortMarker marker)
        {
            if (_currentPortKind == PortKind.Input)
                _addedInputPorts.Add(marker, _lastRegisteredEdge);

            if (_currentPortKind == PortKind.Output)
                _addedInputPorts.Add(marker, _lastRegisteredEdge);

            UpdateLocks();

            if (_currentPortKind == PortKind.Input && _addedInputPorts.Count == 3)
                BC.CancelBuilding();

            if (_currentPortKind == PortKind.Output && _addedOutputPorts.Count == 1)
                BC.CancelBuilding();
            OnPortMarkerUpdated?.Invoke(_addedInputPorts.Count == 3, _addedOutputPorts.Count == 1);
        }

        public void UpdateLocks()
        {
            GrM.UnlockAllSides();
            foreach (var input in _addedInputPorts)
                GrM.LockSide(input.Value);
            foreach (var input in _addedOutputPorts)
                GrM.LockSide(input.Value);
        }

        public Direction GetForcedFacing()
        {
            if (_currentPortKind == PortKind.Input)
            {
                switch (_lastRegisteredEdge)
                {
                    case GridEdge.Left: return Direction.Right;
                    case GridEdge.Bottom: return Direction.Up;
                    case GridEdge.Right: return Direction.Left;
                    case GridEdge.Top: return Direction.Down;
                }
            }
            else if (_currentPortKind == PortKind.Output)
            {
                switch (_lastRegisteredEdge)
                {
                    case GridEdge.Left: return Direction.Left;
                    case GridEdge.Bottom: return Direction.Down;
                    case GridEdge.Right: return Direction.Right;
                    case GridEdge.Top: return Direction.Up;
                }
            }

            return Direction.Right;
        }

        public static bool IsOnEdge(Vector2Int cellPos)
        {
            if (GrM == null) return false;

            Vector2Int p = cellPos; // ggf. anpassen falls Property anders heißt

            return p.x == 0
                || p.x == GrM.Width - 1
                || p.y == 0
                || p.y == GrM.Height - 1;
        }

        public static GridEdge GetSingleEdge(Vector2Int cellPos)
        {
            Vector2Int p = cellPos;

            if (p.y == GrM.Height - 1) return GridEdge.Top;
            if (p.x == GrM.Width - 1) return GridEdge.Right;
            if (p.y == 0) return GridEdge.Bottom;
            if (p.x == 0) return GridEdge.Left;

            return GridEdge.None;
        }
    }
}
