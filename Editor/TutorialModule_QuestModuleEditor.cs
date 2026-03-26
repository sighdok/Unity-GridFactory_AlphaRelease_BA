using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace NINESOFT.TUTORIAL_SYSTEM
{
    [CustomEditor(typeof(TutorialModule_Quest))]
    public class TutorialModule_QuestModuleEditor : Editor
    {
        public SerializedProperty quest;
        public SerializedProperty step;
        public SerializedProperty startDelay;



        public override void OnInspectorGUI()
        {
            GUI.backgroundColor = NSEditorData.Purple2;

            FindProperties();
            DrawProperties();
        }

        private void FindProperties()
        {
            quest = serializedObject.FindProperty("quest");
            step = serializedObject.FindProperty("step");
            startDelay = serializedObject.FindProperty("startDelay");
        }

        private void DrawProperties()
        {
            NSEditorData.DrawComponentTitleBox("QUEST", NSEditorData.EditorScriptType.ModuleUI);

            if (NSEditorData.DrawTitleBox("QUEST SETTINGS", 10))
            {

                EditorGUILayout.PropertyField(quest);
                EditorGUILayout.PropertyField(step);
                EditorGUILayout.PropertyField(startDelay);
            }
            serializedObject.ApplyModifiedProperties();
        }

    }
}
