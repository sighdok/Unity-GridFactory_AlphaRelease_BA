using System;
using System.Collections.Generic;
using UnityEngine;
using GridFactory.Blueprints;
using GridFactory.Directions;
using GridFactory.Meta;

namespace GridFactory.Core
{
    public class BuildingGhost : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer inputArrowPrefab;
        [SerializeField] private SpriteRenderer outputArrowPrefab;
        [SerializeField] private float arrowOffset = 0.5f;

        private const Direction _baseFacing = Direction.Right;

        private readonly List<Direction> _baseInputSides = new();
        private readonly List<Direction> _baseOutputSides = new();
        private readonly List<SpriteRenderer> _inputArrows = new();
        private readonly List<SpriteRenderer> _outputArrows = new();

        private Direction _currentFacing = Direction.Right;
        private bool _initialized;

        private static readonly Direction[] _CW =
        {
            Direction.Up,
            Direction.Right,
            Direction.Down,
            Direction.Left
        };

        public void InitFromBlueprint(BlueprintDefinition bp)
        {
            _initialized = false;

            Cleanup();

            if (bp == null) return;

            if (bp.inputPorts != null)
            {
                foreach (var port in bp.inputPorts)
                {
                    var side = DirectionUtils.Opposite(port.facing);
                    if (!_baseInputSides.Contains(side))
                        _baseInputSides.Add(side);
                }
            }

            if (bp.hasOutputPort)
                _baseOutputSides.Add(bp.outputPort.facing);

            if (inputArrowPrefab != null)
            {
                foreach (var side in _baseInputSides)
                {
                    var sr = Instantiate(inputArrowPrefab, transform);
                    sr.gameObject.name = $"InputArrow_{side}";
                    sr.flipX = true;
                    _inputArrows.Add(sr);
                }
            }

            if (outputArrowPrefab != null)
            {
                var sr = Instantiate(outputArrowPrefab, transform);
                sr.gameObject.name = $"OutputArrow";
                _outputArrows.Add(sr);
            }

            _currentFacing = Direction.Right;
            _initialized = true;

            UpdateVisual();
        }

        public void InitByDirections(bool hasInput, bool hasOutput, List<Direction> allInputDirections, List<Direction> allOutputDirections)
        {
            _initialized = false;

            Cleanup();

            foreach (var side in allInputDirections)
            {
                if (!_baseInputSides.Contains(side))
                    _baseInputSides.Add(side);
            }

            foreach (var side in allOutputDirections)
            {
                if (!_baseOutputSides.Contains(side))
                    _baseOutputSides.Add(side);
            }

            if (inputArrowPrefab != null && hasInput)
            {
                foreach (var side in _baseInputSides)
                {
                    var sr = Instantiate(inputArrowPrefab, transform);
                    sr.gameObject.name = $"InputArrow_{side}";
                    sr.flipX = true;
                    _inputArrows.Add(sr);
                }
            }

            if (outputArrowPrefab != null && hasOutput)
            {
                foreach (var side in _baseOutputSides)
                {
                    var sr = Instantiate(outputArrowPrefab, transform);
                    sr.gameObject.name = $"OutputArrow_{side}";
                    _outputArrows.Add(sr);
                }
            }

            _currentFacing = Direction.Right;
            _initialized = true;

            UpdateVisual();
        }

        public void SetFacing(Direction facing)
        {
            _currentFacing = facing;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (!_initialized)
                return;

            int steps = StepsCW(_baseFacing, _currentFacing);

            for (int i = 0; i < _baseInputSides.Count; i++)
            {
                if (i >= _inputArrows.Count)
                    break;

                Direction finalSide = RotateCW(_baseInputSides[i], steps);

                _inputArrows[i].transform.localPosition =
                    DirToOffset(finalSide) * arrowOffset;

                _inputArrows[i].transform.localRotation =
                    Quaternion.Euler(0, 0, DirectionUtils.DirToAngle(finalSide));
            }

            for (int i = 0; i < _baseOutputSides.Count; i++)
            {
                if (i >= _outputArrows.Count)
                    break;

                Direction finalSide = RotateCW(_baseOutputSides[i], steps);

                _outputArrows[i].transform.localPosition =
                    DirToOffset(finalSide) * arrowOffset;

                _outputArrows[i].transform.localRotation =
                     Quaternion.Euler(0, 0, DirectionUtils.DirToAngle(finalSide));
            }
        }

        public void Cleanup()
        {
            _baseInputSides.Clear();
            _baseOutputSides.Clear();

            foreach (var arrow in _inputArrows)
                if (arrow != null)
                    Destroy(arrow.gameObject);

            _inputArrows.Clear();

            foreach (var arrow in _outputArrows)
                if (arrow != null)
                    Destroy(arrow.gameObject);

            _outputArrows.Clear();
        }

        private static int StepsCW(Direction from, Direction to)
        {
            int a = Array.IndexOf(_CW, from);
            int b = Array.IndexOf(_CW, to);
            return (b - a + 4) % 4;
        }

        private static Direction RotateCW(Direction dir, int steps)
        {
            steps %= 4;
            if (steps < 0) steps += 4;

            int i = Array.IndexOf(_CW, dir);
            return _CW[(i + steps) % 4];
        }

        private static Vector3 DirToOffset(Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return new Vector3(+1, 0, 0);
                case Direction.Left: return new Vector3(-1, 0, 0);
                case Direction.Up: return new Vector3(0, +1, 0);
                case Direction.Down: return new Vector3(0, -1, 0);
            }
            return Vector3.zero;
        }
    }
}
