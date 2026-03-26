using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GridFactory.UI
{
    public class GridPreviewUI : MonoBehaviour
    {
        [Header("Grid Root")]
        [SerializeField] private RectTransform cellRoot;
        [SerializeField] private GridLayoutGroup gridLayout;

        [Header("Cell Prefab")]
        [SerializeField] private Image cellPrefab;

        [Header("Style")]
        [SerializeField] private Color freeColor = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color lockedColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.08f);

        [Header("Layout")]
        [Tooltip("Wenn > 0, wird die Zellgröße automatisch so berechnet, dass das Grid in den Root passt.")]
        [SerializeField] private bool autoFitCellSize = true;
        [SerializeField] private float maxCellSize = 26f;
        [SerializeField] private float minCellSize = 6f;

        private readonly List<Image> cells = new List<Image>();
        private int currentW;
        private int currentH;

        public void Render(int width, int height, System.Func<int, int, bool> isLocked)
        {
            if (width <= 0 || height <= 0) return;
            if (gridLayout == null || cellRoot == null || cellPrefab == null)
            {
                Debug.LogWarning("[GridPreviewUI] Missing references.", this);
                return;
            }

            // ✅ CRUCIAL: Ensure correct columns
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = width;

            // Optional: nice defaults (verhindert “komisch gedreht”)
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            if (autoFitCellSize)
                AutoFit(width, height);

            EnsureCells(width, height);

            // y flip so y=0 is bottom in your logical grid
            for (int y = 0; y < height; y++)
            {
                int uiY = (height - 1) - y;
                for (int x = 0; x < width; x++)
                {
                    int idx = uiY * width + x;
                    bool locked = isLocked != null && isLocked(x, y);
                    cells[idx].color = locked ? lockedColor : freeColor;
                }
            }

            // optional border
            var imgBorder = cellRoot.GetComponent<Image>();
            if (imgBorder != null) imgBorder.color = borderColor;
        }

        private void AutoFit(int width, int height)
        {
            // Cell size so the grid fits in the root rect
            Rect r = cellRoot.rect;
            float spacingX = gridLayout.spacing.x;
            float spacingY = gridLayout.spacing.y;

            float availableW = Mathf.Max(1f, r.width - (width - 1) * spacingX - gridLayout.padding.left - gridLayout.padding.right);
            float availableH = Mathf.Max(1f, r.height - (height - 1) * spacingY - gridLayout.padding.top - gridLayout.padding.bottom);

            float cellW = availableW / width;
            float cellH = availableH / height;

            float size = Mathf.Clamp(Mathf.Min(cellW, cellH), minCellSize, maxCellSize);
            gridLayout.cellSize = new Vector2(size, size);
        }

        private void EnsureCells(int width, int height)
        {
            int needed = width * height;

            if (currentW != width || currentH != height)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    if (cells[i] != null)
                        Destroy(cells[i].gameObject);
                }
                cells.Clear();
                currentW = width;
                currentH = height;
            }

            while (cells.Count < needed)
            {
                var img = Instantiate(cellPrefab, cellRoot);
                img.gameObject.name = $"Cell_{cells.Count}";
                img.raycastTarget = false;
                cells.Add(img);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null)
                    cells[i].gameObject.SetActive(i < needed);
            }

            // Force layout rebuild (hilft, wenn Root gerade enabled wurde)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cellRoot);
        }
    }
}
