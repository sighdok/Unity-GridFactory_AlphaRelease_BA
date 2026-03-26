using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    [CustomEditor(typeof(TutorialModule_ButtonControl))]
    public class TutorialModule_ButtonControlEditor : Editor
    {


        public SerializedProperty targetTransform;
        public SerializedProperty disable;
        public SerializedProperty recursive;



        public override void OnInspectorGUI()
        {
            GUI.backgroundColor = NSEditorData.Purple2;

            FindProperties();
            DrawProperties();
        }

        private void FindProperties()
        {
            targetTransform = serializedObject.FindProperty("targetTransform");
            disable = serializedObject.FindProperty("disable");
            recursive = serializedObject.FindProperty("recursive");
        }

        private void DrawProperties()
        {
            NSEditorData.DrawComponentTitleBox("BUTTON", NSEditorData.EditorScriptType.ModuleUI);

            if (NSEditorData.DrawTitleBox("BUTTON SETTINGS SETTINGS", 10))
            {

                EditorGUILayout.PropertyField(targetTransform);
                EditorGUILayout.PropertyField(disable);
                EditorGUILayout.PropertyField(recursive);
            }
            serializedObject.ApplyModifiedProperties();
        }

    }
}
