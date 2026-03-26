using System;
using System.Collections.Generic;

using UnityEngine;

namespace GridFactory.Grid
{
    public static class RandomGridGenerator
    {
        public struct Settings
        {
            public float maxLockedPercent;
            public int maxAttempts;
        }

        public static List<CellLockData> GenerateUnlockableLocks(int width, int height, int seed, Settings settings)
        {
            settings.maxAttempts = Mathf.Max(1, settings.maxAttempts);
            settings.maxLockedPercent = Mathf.Clamp01(settings.maxLockedPercent);

            int total = width * height;
            int maxLocked = Mathf.FloorToInt(total * settings.maxLockedPercent);

            var rng = new System.Random(seed);

            for (int attempt = 0; attempt < settings.maxAttempts; attempt++)
            {
                bool[,] locked = new bool[width, height];
                int lockedCount = rng.Next(0, maxLocked + 1);

                var allPositions = new List<Vector2Int>(total);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        allPositions.Add(new Vector2Int(x, y));

                Shuffle(allPositions, rng);

                for (int i = 0; i < lockedCount && i < allPositions.Count; i++)
                {
                    var p = allPositions[i];
                    locked[p.x, p.y] = true;
                }

                if (HasNoFreeIslands(locked, width, height))
                {
                    var result = new List<CellLockData>(lockedCount);
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (locked[x, y])
                                result.Add(new CellLockData(x, y, LockType.Unlockable));
                        }
                    }
                    return result;
                }
            }

            return new List<CellLockData>();
        }

        private static bool HasNoFreeIslands(bool[,] locked, int width, int height)
        {
            bool[,] visited = new bool[width, height];
            var q = new Queue<Vector2Int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                if (locked[x, y]) return;
                if (visited[x, y]) return;
                visited[x, y] = true;
                q.Enqueue(new Vector2Int(x, y));
            }

            for (int x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (int y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                TryEnqueue(p.x + 1, p.y);
                TryEnqueue(p.x - 1, p.y);
                TryEnqueue(p.x, p.y + 1);
                TryEnqueue(p.x, p.y - 1);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!locked[x, y] && !visited[x, y])
                        return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
