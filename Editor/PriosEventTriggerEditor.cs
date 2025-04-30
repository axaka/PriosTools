using System.Collections.Generic;
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

		private List<string> allKeys = new List<string>();


		private void OnEnable()
		{
			eventKeyProp = serializedObject.FindProperty("EventKey");
			selectedTypeProp = serializedObject.FindProperty("SelectedType");
			boolDataProp = serializedObject.FindProperty("boolData");
			intDataProp = serializedObject.FindProperty("intData");
			floatDataProp = serializedObject.FindProperty("floatData");
			stringDataProp = serializedObject.FindProperty("stringData");
			objectDataProp = serializedObject.FindProperty("objectData");

			UpdateKeyList();
		}

		private void UpdateKeyList()
		{
			allKeys.Clear();

			// Triggers
			PriosEventTrigger[] triggers = FindObjectsOfType<PriosEventTrigger>();
			foreach (var trigger in triggers)
			{
				if (!string.IsNullOrEmpty(trigger.EventKey) && !allKeys.Contains(trigger.EventKey))
				{
					allKeys.Add(trigger.EventKey);
				}
			}

			// Recievers
			PriosEventReciever[] recievers = FindObjectsOfType<PriosEventReciever>();
			foreach (var reciever in recievers)
			{
				if (!string.IsNullOrEmpty(reciever.EventKey) && !allKeys.Contains(reciever.EventKey))
				{
					allKeys.Add(reciever.EventKey);
				}
			}
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

			// Draw the list of all used keys
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Used Keys in the Scene:", EditorStyles.boldLabel);

			if (allKeys.Count > 0)
			{
				foreach (var key in allKeys)
				{
					EditorGUILayout.LabelField($"• {key}", EditorStyles.helpBox);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("No keys found in the scene.", MessageType.Info);
			}

		}
	}
}