using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PriosTools
{
	[CustomEditor(typeof(PriosEventReciever))]
	[CanEditMultipleObjects] // Enables multi-object editing
	public class PriosEventRecieverEditor : Editor
	{
		private SerializedProperty eventKeyProp;
		private SerializedProperty selectedComponentTypesProp;
		private List<string> allKeys = new List<string>();

		private void OnEnable()
		{
			eventKeyProp = serializedObject.FindProperty("EventKey");
			selectedComponentTypesProp = serializedObject.FindProperty("SelectedComponentTypes");

			UpdateKeyList();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(eventKeyProp, new GUIContent("Event Key"));
			selectedComponentTypesProp.intValue = 
				(int)(PriosEventReciever.ComponentTypes)EditorGUILayout.EnumFlagsField(
					new GUIContent("Selected Components"), 
					(PriosEventReciever.ComponentTypes)selectedComponentTypesProp.intValue);

			serializedObject.ApplyModifiedProperties();

			// Show all used keys
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

		private void UpdateKeyList()
		{
			allKeys.Clear();
			PriosEventTrigger[] triggers = FindObjectsOfType<PriosEventTrigger>();
			foreach (var trigger in triggers)
			{
				if (!string.IsNullOrEmpty(trigger.EventKey) && !allKeys.Contains(trigger.EventKey))
				{
					allKeys.Add(trigger.EventKey);
				}
			}

			PriosEventReciever[] receivers = FindObjectsOfType<PriosEventReciever>();
			foreach (var receiver in receivers)
			{
				if (!string.IsNullOrEmpty(receiver.EventKey) && !allKeys.Contains(receiver.EventKey))
				{
					allKeys.Add(receiver.EventKey);
				}
			}
		}
	}
}