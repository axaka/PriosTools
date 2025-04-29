using UnityEditor;
using UnityEngine;

namespace PriosTools
{
    [CustomEditor(typeof(PriosEventTrigger))]
    [CanEditMultipleObjects] // Enables multi-object editing
    public class PriosEventTriggerEditor : Editor
    {
        private SerializedProperty eventKeyProp;
        private SerializedProperty selectedTypeProp;
        private SerializedProperty boolDataProp;
        private SerializedProperty intDataProp;
        private SerializedProperty floatDataProp;
        private SerializedProperty stringDataProp;
        private SerializedProperty objectDataProp;

        private void OnEnable()
        {
            eventKeyProp = serializedObject.FindProperty("EventKey");
            selectedTypeProp = serializedObject.FindProperty("SelectedType");
            boolDataProp = serializedObject.FindProperty("boolData");
            intDataProp = serializedObject.FindProperty("intData");
            floatDataProp = serializedObject.FindProperty("floatData");
            stringDataProp = serializedObject.FindProperty("stringData");
            objectDataProp = serializedObject.FindProperty("objectData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Start serialized object modification

            EditorGUILayout.PropertyField(eventKeyProp, new GUIContent("Event Key"));
            EditorGUILayout.PropertyField(selectedTypeProp, new GUIContent("Event Type"));

            // Draw fields dynamically based on selected enum type
            switch ((PriosEvent.EventType)selectedTypeProp.enumValueIndex)
            {
                case PriosEvent.EventType.Bool:
                    EditorGUILayout.PropertyField(boolDataProp, new GUIContent("Bool Value"));
                    break;
                case PriosEvent.EventType.Int:
                    EditorGUILayout.PropertyField(intDataProp, new GUIContent("Int Value"));
                    break;
                case PriosEvent.EventType.Float:
                    EditorGUILayout.PropertyField(floatDataProp, new GUIContent("Float Value"));
                    break;
                case PriosEvent.EventType.String:
                    EditorGUILayout.PropertyField(stringDataProp, new GUIContent("String Value"));
                    break;
                case PriosEvent.EventType.Object:
                    EditorGUILayout.PropertyField(objectDataProp, new GUIContent("Object Value"));
                    break;
            }

            serializedObject.ApplyModifiedProperties(); // Save changes
        }
    }
}