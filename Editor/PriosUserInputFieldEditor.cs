using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PriosTools
{
	[CustomEditor(typeof(PriosUserInputField))]
	public class PriosUserInputFieldEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var field = (PriosUserInputField)target;

			// Reference to userData
			EditorGUILayout.PropertyField(serializedObject.FindProperty("userData"));

			// Label text field
			EditorGUILayout.PropertyField(serializedObject.FindProperty("labelText"));

			var keyProp = serializedObject.FindProperty("_userDataKey");

			// Show dropdown if userData is valid
			if (field.userData != null)
			{
				var keys = field.userData.GetDefinedKeys()?.ToList();
				if (keys != null && keys.Count > 0)
				{
					PriosEditor.DrawDropdownFromList("User Data Key", keyProp, keys);
				}
				else
				{
					EditorGUILayout.PropertyField(keyProp);
					EditorGUILayout.HelpBox("No keys found in userData._data.", MessageType.Warning);
				}
			}
			else
			{
				EditorGUILayout.PropertyField(keyProp);
				EditorGUILayout.HelpBox("Assign a PriosUserData asset to choose from defined keys.", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space(10);
			if (GUILayout.Button("Refresh Field from UserData"))
			{
#if UNITY_EDITOR
				field.Refresh();
				EditorUtility.SetDirty(field);
#endif
			}

		}
	}
}
