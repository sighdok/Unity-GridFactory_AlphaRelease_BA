using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    [CustomEditor(typeof(TutorialModule_Dialogue))]
    public class TutorialModule_DialogueModuleEditor : Editor
    {
        public SerializedProperty actor;
        public SerializedProperty narrativeEvent;
        public SerializedProperty startDelay;



        public override void OnInspectorGUI()
        {
            GUI.backgroundColor = NSEditorData.Purple2;

            FindProperties();
            DrawProperties();
        }

        private void FindProperties()
        {
            actor = serializedObject.FindProperty("actor");
            narrativeEvent = serializedObject.FindProperty("narrativeEvent");
            startDelay = serializedObject.FindProperty("startDelay");
        }

        private void DrawProperties()
        {
            NSEditorData.DrawComponentTitleBox("DIALOGUE", NSEditorData.EditorScriptType.ModuleUI);

            if (NSEditorData.DrawTitleBox("DIALOGUE SETTINGS", 10))
            {

                EditorGUILayout.PropertyField(actor);
                EditorGUILayout.PropertyField(narrativeEvent);
                EditorGUILayout.PropertyField(startDelay);
            }
            serializedObject.ApplyModifiedProperties();
        }

    }
}
