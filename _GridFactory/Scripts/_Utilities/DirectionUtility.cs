using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridFactory.Directions
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public struct CornerVisual
    {
        public float angle;
        public bool flipX;
        public bool flipY;

        public CornerVisual(float angle, bool flipX = false, bool flipY = false)
        {
            this.angle = angle;
            this.flipX = flipX;
            this.flipY = flipY;
        }
    }

    public static class DirectionUtils
    {
        private static readonly Direction[] _allDirectionsCW =
              {
            Direction.Up,
            Direction.Right,
            Direction.Down,
            Direction.Left
        };

        public static int RotationStepsCW(Direction from, Direction to)
        {
            int a = Array.IndexOf(_allDirectionsCW, from);
            int b = Array.IndexOf(_allDirectionsCW, to);
            return (b - a + 4) % 4;
        }

        public static class IOOrientation
        {
            public static void RotateIOByFacing(
                Direction baseFacing,
                Direction currentFacing,
                IReadOnlyList<Direction> baseInputs,
                Direction baseOutput,
                List<Direction> outInputs,
                out Direction outOutput)
            {
                outInputs.Clear();
                int steps = RotationStepsCW(baseFacing, currentFacing);
                for (int i = 0; i < baseInputs.Count; i++)
                    outInputs.Add(RotateCW(baseInputs[i], steps));

                outOutput = RotateCW(baseOutput, steps);
            }
        }

        public static Direction[] AllDirections()
        {
            return _allDirectionsCW;
        }

        public static List<Direction> AllDirectionsAsList()
        {
            List<Direction> myDirs = new List<Direction>
            {
                Direction.Up,
                Direction.Right,
                Direction.Down,
                Direction.Left
            };
            return myDirs;
        }

        public static int DirToAngle(Direction d)
        {
            switch (d)
            {
                case Direction.Right: return 0;
                case Direction.Up: return 90;
                case Direction.Left: return 180;
                case Direction.Down: return 270;
            }
            return 0;
        }

        public static Vector2Int DirectionToOffset(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Down: return new Vector2Int(0, -1);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Right: return new Vector2Int(1, 0);
            }
            return Vector2Int.zero;
        }

        public static Vector3 DirectionToOffsetVector3(Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return new Vector3(+1, 0, 0); // rechts von der Mitte
                case Direction.Left: return new Vector3(-1, 0, 0); // links  von der Mitte
                case Direction.Up: return new Vector3(0, +1, 0); // oberhalb der Mitte
                case Direction.Down: return new Vector3(0, -1, 0); // unterhalb der Mitte
            }
            return Vector3.zero;
        }

        public static Direction RotateCW(Direction dir, int steps)
        {
            steps %= 4;
            if (steps < 0) steps += 4;
            int i = Array.IndexOf(_allDirectionsCW, dir);

            return _allDirectionsCW[(i + steps) % 4];
        }

        public static Direction Opposite(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
            }
            return Direction.Right;
        }

        public static Direction GetLeft(Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return Direction.Up;
                case Direction.Up: return Direction.Left;
                case Direction.Left: return Direction.Down;
                case Direction.Down: return Direction.Right;
            }
            return Direction.Right;
        }

        public static Direction GetRight(Direction dir)
        {
            switch (dir)
            {
                case Direction.Right: return Direction.Down;
                case Direction.Down: return Direction.Left;
                case Direction.Left: return Direction.Up;
                case Direction.Up: return Direction.Right;
            }
            return Direction.Right;
        }


        public static CornerVisual GetCornerVisual(Direction outputFacing, Direction inputFacing)
        {
            Direction entry = inputFacing;
            Direction exit = outputFacing;

            if (entry == Direction.Left && exit == Direction.Down)
                return new CornerVisual(0f, false, true);
            if (entry == Direction.Left && exit == Direction.Up)
                return new CornerVisual(0f, false, false);
            if (entry == Direction.Right && exit == Direction.Down)
                return new CornerVisual(0f, true, true);
            if (entry == Direction.Right && exit == Direction.Up)
                return new CornerVisual(0f, true, false);

            if (entry == Direction.Up && exit == Direction.Right)
                return new CornerVisual(90f, true, true);
            if (entry == Direction.Up && exit == Direction.Left)
                return new CornerVisual(90f, true, false);
            if (entry == Direction.Down && exit == Direction.Left)
                return new CornerVisual(90f, false, false);
            if (entry == Direction.Down && exit == Direction.Right)
                return new CornerVisual(90f, false, true);

            return new CornerVisual(0f, false, false);
        }
    }
}
