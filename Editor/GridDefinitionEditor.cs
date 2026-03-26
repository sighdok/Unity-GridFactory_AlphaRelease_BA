using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GridFactory.Grid;
namespace GridFactory.Core.Editor
{
    [CustomEditor(typeof(GridDefinition))]
    public class GridDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty idProperty;
        private SerializedProperty displayNameProperty;
        private SerializedProperty widthProperty;
        private SerializedProperty heightProperty;
        private SerializedProperty priceProperty;
        private SerializedProperty lockedCellsProperty;

        // Track the locked state of each cell in a 2D array for easy editing
        private bool[,] cellStates;
        private bool needsRefresh = true;

        private void OnEnable()
        {
            idProperty = serializedObject.FindProperty("id");
            displayNameProperty = serializedObject.FindProperty("displayName");
            widthProperty = serializedObject.FindProperty("width");
            heightProperty = serializedObject.FindProperty("height");
            priceProperty = serializedObject.FindProperty("price");
            lockedCellsProperty = serializedObject.FindProperty("lockedCells");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(idProperty);
            EditorGUILayout.PropertyField(displayNameProperty);
            EditorGUILayout.PropertyField(priceProperty);

            string id = idProperty.stringValue;
            string displayName = displayNameProperty.stringValue;

            // Draw default properties
            EditorGUILayout.PropertyField(widthProperty);
            EditorGUILayout.PropertyField(heightProperty);

            // Validate dimensions
            int width = Mathf.Max(1, widthProperty.intValue);
            int height = Mathf.Max(1, heightProperty.intValue);

            if (widthProperty.intValue != width) widthProperty.intValue = width;
            if (heightProperty.intValue != height) heightProperty.intValue = height;

            // Initialize or refresh cell states when dimensions change
            if (needsRefresh || cellStates == null || cellStates.GetLength(0) != width || cellStates.GetLength(1) != height)
            {
                InitializeCellStates(width, height);
                needsRefresh = false;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Locked Cells", EditorStyles.boldLabel);

            // Draw the visual grid
            DrawGridVisualization(width, height);

            // Apply changes
            if (serializedObject.ApplyModifiedProperties())
            {
                needsRefresh = true;
            }
        }

        private void InitializeCellStates(int width, int height)
        {
            cellStates = new bool[width, height];

            // Initialize all cells as unlocked
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cellStates[x, y] = false;
                }
            }

            // Apply existing locked cells data
            List<Vector2Int> lockedCellsList = new List<Vector2Int>();
            for (int i = 0; i < lockedCellsProperty.arraySize; i++)
            {
                SerializedProperty cellProperty = lockedCellsProperty.GetArrayElementAtIndex(i);
                Vector2Int cell = cellProperty.vector2IntValue;

                if (cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height)
                {
                    cellStates[cell.x, cell.y] = true;
                    lockedCellsList.Add(cell);
                }
            }

            // Update the serialized property with valid locked cells
            lockedCellsProperty.arraySize = lockedCellsList.Count;
            for (int i = 0; i < lockedCellsList.Count; i++)
            {
                SerializedProperty cellProperty = lockedCellsProperty.GetArrayElementAtIndex(i);
                cellProperty.vector2IntValue = lockedCellsList[i];
            }
        }

        // Alternative DrawGridVisualization Methode für ein kompakteres Grid
        private void DrawGridVisualization(int width, int height)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"Grid: {width} × {height} (Click to toggle locked state)");

            // Calculate button size based on grid dimensions
            int buttonSize = Mathf.Clamp(300 / Mathf.Max(width, height), 15, 30);

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Y:{y}", GUILayout.Width(30));

                for (int x = 0; x < width; x++)
                {
                    // Use a button with different colors for visual feedback
                    Color originalColor = GUI.color;
                    GUI.color = cellStates[x, y] ? Color.red : Color.green;

                    if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        cellStates[x, y] = !cellStates[x, y];
                        UpdateLockedCellsList();
                    }

                    GUI.color = originalColor;

                    // Add small spacing between cells
                    GUILayout.Space(2);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Legend
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Locked:", GUILayout.Width(50));
            GUI.color = Color.red;
            GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.color = Color.white;
            GUILayout.Label("Unlocked:", GUILayout.Width(60));
            GUI.color = Color.green;
            GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void SetAllCells(bool locked)
        {
            for (int x = 0; x < cellStates.GetLength(0); x++)
            {
                for (int y = 0; y < cellStates.GetLength(1); y++)
                {
                    cellStates[x, y] = locked;
                }
            }
            UpdateLockedCellsList();
        }

        private void InvertAllCells()
        {
            for (int x = 0; x < cellStates.GetLength(0); x++)
            {
                for (int y = 0; y < cellStates.GetLength(1); y++)
                {
                    cellStates[x, y] = !cellStates[x, y];
                }
            }
            UpdateLockedCellsList();
        }

        private void UpdateLockedCellsList()
        {
            List<Vector2Int> lockedCellsList = new List<Vector2Int>();

            for (int x = 0; x < cellStates.GetLength(0); x++)
            {
                for (int y = 0; y < cellStates.GetLength(1); y++)
                {
                    if (cellStates[x, y])
                    {
                        lockedCellsList.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Update the serialized property
            lockedCellsProperty.arraySize = lockedCellsList.Count;
            for (int i = 0; i < lockedCellsList.Count; i++)
            {
                SerializedProperty cellProperty = lockedCellsProperty.GetArrayElementAtIndex(i);
                cellProperty.vector2IntValue = lockedCellsList[i];
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}